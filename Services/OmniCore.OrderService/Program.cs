using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using OmniCore.OrderService.Data;
using OmniCore.OrderService.Repositories.Implementations;
using OmniCore.OrderService.Repositories.Interfaces;
using OmniCore.OrderService.Services.Clients;
using OmniCore.OrderService.Services.Implementations;
using OmniCore.OrderService.Services.Interfaces;

using OmniCore.OrderService.Messaging.Producers;
using OmniCore.OrderService.Messaging.Consumers;

using OmniCore.OrderService.Middleware;

using OmniCore.OrderService.Health;

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

builder.Services.AddScoped<
    IOrderEventPublisher,
    RabbitMqOrderEventPublisher>();

builder.Services.AddHostedService<OrderCreatedConsumer>();

builder.Services.AddDbContext<OrderDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services
    .AddHttpClient<
        IProductServiceClient,
        ProductServiceClient>(
        client =>
        {
            client.BaseAddress = new Uri(productServiceUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        })
    .AddHttpMessageHandler<CorrelationIdHandler>();

builder.Services
    .AddHttpClient<
        IInventoryServiceClient,
        InventoryServiceClient>(
        client =>
        {
            client.BaseAddress = new Uri(inventoryServiceUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        })
    .AddHttpMessageHandler<CorrelationIdHandler>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<CorrelationIdHandler>();

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<OrderDbContext>(
        name: "order-database")
    .AddCheck<RabbitMqHealthCheck>(
        name: "rabbitmq");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<OrderDbContext>();

    await dbContext.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapControllers();

app.Run();