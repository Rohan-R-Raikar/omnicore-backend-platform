using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using OmniCore.OrderService.Data;
using OmniCore.OrderService.Health;
using OmniCore.OrderService.Messaging.Consumers;
using OmniCore.OrderService.Messaging.Producers;
using OmniCore.OrderService.Middleware;
using OmniCore.OrderService.Repositories.Implementations;
using OmniCore.OrderService.Repositories.Interfaces;
using OmniCore.OrderService.Services.Clients;
using OmniCore.OrderService.Services.Implementations;
using OmniCore.OrderService.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("OrderDatabase")
    ?? throw new InvalidOperationException(
        "OrderDatabase connection string is not configured.");

var productServiceUrl =
    builder.Configuration["Services:ProductService"]
    ?? throw new InvalidOperationException(
        "ProductService URL is not configured.");

var inventoryServiceUrl =
    builder.Configuration["Services:InventoryService"]
    ?? throw new InvalidOperationException(
        "InventoryService URL is not configured.");

var jwtKey =
    builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "JWT key is not configured.");

var jwtIssuer =
    builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "JWT issuer is not configured.");

var jwtAudience =
    builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "JWT audience is not configured.");

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT token."
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
});

builder.Services.AddDbContext<OrderDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

builder.Services.AddScoped<
    IOrderRepository,
    OrderRepository>();

builder.Services.AddScoped<
    IOrderService,
    OrderService>();

builder.Services.AddScoped<
    IOrderEventPublisher,
    RabbitMqOrderEventPublisher>();

builder.Services.AddHostedService<
    OrderCreatedConsumer>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<
    CorrelationIdHandler>();

//
// PRODUCT SERVICE HTTP CLIENT
//
// ProductService is called using GET requests.
// GET requests are safe to retry.
//
builder.Services
    .AddHttpClient<
        IProductServiceClient,
        ProductServiceClient>(
        client =>
        {
            client.BaseAddress =
                new Uri(productServiceUrl);
        })
    .AddHttpMessageHandler<
        CorrelationIdHandler>()
    .AddStandardResilienceHandler(options =>
    {
        // Retry transient ProductService failures.
        options.Retry.MaxRetryAttempts = 3;

        options.Retry.Delay =
            TimeSpan.FromMilliseconds(500);

        options.Retry.UseJitter = true;

        // Maximum time allowed for one HTTP attempt.
        options.AttemptTimeout.Timeout =
            TimeSpan.FromSeconds(3);

        // Maximum total time for the complete request,
        // including retries.
        options.TotalRequestTimeout.Timeout =
            TimeSpan.FromSeconds(10);

        // Circuit breaker configuration.
        options.CircuitBreaker.FailureRatio = 0.5;

        options.CircuitBreaker.MinimumThroughput = 5;

        options.CircuitBreaker.SamplingDuration =
            TimeSpan.FromSeconds(20);

        options.CircuitBreaker.BreakDuration =
            TimeSpan.FromSeconds(15);
    });

//
// INVENTORY SERVICE HTTP CLIENT
//
// Reserve and Release use POST requests.
//
// We DO NOT automatically retry POST requests because
// the InventoryService operation could already have succeeded
// even if OrderService failed to receive the response.
//
builder.Services
    .AddHttpClient<
        IInventoryServiceClient,
        InventoryServiceClient>(
        client =>
        {
            client.BaseAddress =
                new Uri(inventoryServiceUrl);
        })
    .AddHttpMessageHandler<
        CorrelationIdHandler>()
    .AddStandardResilienceHandler(options =>
    {
        // Disable automatic retries for POST/PUT/PATCH/DELETE.
        options.Retry.ShouldHandle =
            static _ => ValueTask.FromResult(false);

        // Give Inventory slightly more time because
        // reservation writes to SQL Server.
        options.AttemptTimeout.Timeout =
            TimeSpan.FromSeconds(5);

        options.TotalRequestTimeout.Timeout =
            TimeSpan.FromSeconds(8);

        // Circuit breaker configuration.
        options.CircuitBreaker.FailureRatio = 0.5;

        options.CircuitBreaker.MinimumThroughput = 5;

        options.CircuitBreaker.SamplingDuration =
            TimeSpan.FromSeconds(20);

        options.CircuitBreaker.BreakDuration =
            TimeSpan.FromSeconds(15);
    });

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),

                ClockSkew = TimeSpan.Zero
            };
    });

builder.Services.AddAuthorization();

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<OrderDbContext>(
        name: "order-database")
    .AddCheck<RabbitMqHealthCheck>(
        name: "rabbitmq");

var app = builder.Build();

//
// APPLY ORDER DATABASE MIGRATIONS
//
using (var scope = app.Services.CreateScope())
{
    var dbContext =
        scope.ServiceProvider
            .GetRequiredService<OrderDbContext>();

    await dbContext.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//
// CORRELATION ID MUST RUN BEFORE
// EXCEPTION HANDLING SO FAILURES CAN BE TRACED.
//
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapControllers();

app.Run();