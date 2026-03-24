using Asp.Versioning.ApiExplorer;
using Hangfire;
using OmniCore.API.Extensions;
using OmniCore.API.Middlewares;
using OmniCore.Infrastructure;
using OmniCore.Infrastructure.BackgroundJobs;
using OmniCore.Persistence;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();

builder.Services.AddResponseCaching();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddApplicationServices();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure();

builder.Services.AddApiVersioningConfig();
builder.Services.AddSwaggerConfig();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddRateLimiterConfig();
builder.Services.AddHangfireConfig(builder.Configuration);

var app = builder.Build();

app.UseSwagger();

app.UseSwaggerUI(options =>
{
    var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

    app.UseSwaggerUI(options =>
    {
        foreach (var desc in provider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint(
                $"/swagger/{desc.GroupName}/swagger.json",
                desc.GroupName.ToUpperInvariant());
        }
    });
});

app.UseMiddleware<ExceptionMiddleware>();
app.UseSerilogRequestLogging();
app.UseRateLimiter();
app.UseResponseCaching();

// app.UseHttpsRedirection(); // optional for local dev

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire");

RecurringJob.AddOrUpdate<OrderCleanupJob>(
    "cancel-expired-orders",
    job => job.CancelExpiredOrders(),
    "*/1 * * * *"
);

app.MapControllers();
app.Run();