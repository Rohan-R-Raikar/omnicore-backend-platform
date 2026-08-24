using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniCore.OrderService.Health;

public class RabbitMqHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public RabbitMqHealthCheck(
        IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName =
                    _configuration["RabbitMq:HostName"]
                    ?? "localhost",

                Port =
                    _configuration.GetValue<int>(
                        "RabbitMq:Port"),

                UserName =
                    _configuration["RabbitMq:UserName"]
                    ?? "guest",

                Password =
                    _configuration["RabbitMq:Password"]
                    ?? "guest"
            };

            await using var connection =
                await factory.CreateConnectionAsync(
                    cancellationToken);

            if (!connection.IsOpen)
            {
                return HealthCheckResult.Unhealthy(
                    "RabbitMQ connection is not open.");
            }

            return HealthCheckResult.Healthy(
                "RabbitMQ is reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "RabbitMQ is unavailable.",
                exception);
        }
    }
}