using OmniCore.Shared.Events;
using RabbitMQ.Client;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OmniCore.OrderService.Messaging.Producers;

public class RabbitMqOrderEventPublisher : IOrderEventPublisher
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<RabbitMqOrderEventPublisher> _logger;

    public RabbitMqOrderEventPublisher(
        IConfiguration configuration,
        ILogger<RabbitMqOrderEventPublisher> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task PublishOrderCreatedAsync(
        OrderCreatedEvent orderCreatedEvent,
        CancellationToken cancellationToken = default)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMq:HostName"] ?? "localhost",
            Port = _configuration.GetValue<int>("RabbitMq:Port"),
            UserName = _configuration["RabbitMq:UserName"] ?? "guest",
            Password = _configuration["RabbitMq:Password"] ?? "guest"
        };

        await using var connection =
            await factory.CreateConnectionAsync(cancellationToken);

        await using var channel =
            await connection.CreateChannelAsync(
                cancellationToken: cancellationToken);

        var queueName =
            _configuration["RabbitMq:QueueName"]
            ?? "omnicore.order.created";

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        var json = JsonSerializer.Serialize(orderCreatedEvent);
        var body = Encoding.UTF8.GetBytes(json);

        var correlationId =
            _httpContextAccessor.HttpContext?
                .Items["X-Correlation-ID"]?
                .ToString()
            ?? Guid.NewGuid().ToString();

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = orderCreatedEvent.OrderId.ToString(),
            CorrelationId = correlationId
        };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queueName,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Published OrderCreated event for OrderId {OrderId}.",
            orderCreatedEvent.OrderId);
    }
}