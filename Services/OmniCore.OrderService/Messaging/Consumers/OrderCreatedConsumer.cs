using OmniCore.Shared.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace OmniCore.OrderService.Messaging.Consumers;

public class OrderCreatedConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    private static readonly ConcurrentDictionary<Guid, byte>
        ProcessedOrders = new();

    public OrderCreatedConsumer(
        IConfiguration configuration,
        ILogger<OrderCreatedConsumer> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName =
                _configuration["RabbitMq:HostName"]
                ?? "localhost",

            Port =
                _configuration.GetValue<int>("RabbitMq:Port"),

            UserName =
                _configuration["RabbitMq:UserName"]
                ?? "guest",

            Password =
                _configuration["RabbitMq:Password"]
                ?? "guest",

            AutomaticRecoveryEnabled = true,

            NetworkRecoveryInterval =
                TimeSpan.FromSeconds(5)
        };

        var queueName =
            _configuration["RabbitMq:QueueName"]
            ?? "omnicore.order.created";

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation(
                    "Attempting RabbitMQ connection to {HostName}:{Port}.",
                    factory.HostName,
                    factory.Port);

                _connection =
                    await factory.CreateConnectionAsync(
                        stoppingToken);

                _channel =
                    await _connection.CreateChannelAsync(
                        cancellationToken: stoppingToken);

                await _channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: stoppingToken);

                await _channel.BasicQosAsync(
                    prefetchSize: 0,
                    prefetchCount: 1,
                    global: false,
                    cancellationToken: stoppingToken);

                var consumer =
                    new AsyncEventingBasicConsumer(_channel);

                consumer.ReceivedAsync += async (_, eventArgs) =>
                {
                    await HandleMessageAsync(
                        eventArgs,
                        stoppingToken);
                };

                await _channel.BasicConsumeAsync(
                    queue: queueName,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                _logger.LogInformation(
                    "OrderCreated consumer started for queue {QueueName}.",
                    queueName);

                while (!stoppingToken.IsCancellationRequested &&
                       _connection.IsOpen &&
                       _channel.IsOpen)
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(5),
                        stoppingToken);
                }

                if (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning(
                        "RabbitMQ connection was lost. Reconnecting...");
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "RabbitMQ connection failed. Retrying in 5 seconds.");

                await CleanupAsync();

                try
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(5),
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        await CleanupAsync();
    }

    private async Task HandleMessageAsync(
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellationToken)
    {
        if (_channel is null)
        {
            return;
        }

        var correlationId =
            eventArgs.BasicProperties.CorrelationId
            ?? Guid.NewGuid().ToString();

        using (_logger.BeginScope(
                   new Dictionary<string, object>
                   {
                       ["CorrelationId"] = correlationId
                   }))
        {
            try
            {
                var json =
                    Encoding.UTF8.GetString(
                        eventArgs.Body.ToArray());

                var orderCreatedEvent =
                    JsonSerializer.Deserialize<OrderCreatedEvent>(
                        json);

                if (orderCreatedEvent is null)
                {
                    throw new InvalidOperationException(
                        "OrderCreated event could not be deserialized.");
                }

                if (!ProcessedOrders.TryAdd(
                        orderCreatedEvent.OrderId,
                        0))
                {
                    _logger.LogWarning(
                        "Duplicate OrderCreated event ignored for OrderId {OrderId}.",
                        orderCreatedEvent.OrderId);

                    await _channel.BasicAckAsync(
                        eventArgs.DeliveryTag,
                        multiple: false,
                        cancellationToken);

                    return;
                }

                _logger.LogInformation(
                    "Processing OrderCreated event. " +
                    "OrderId: {OrderId}, " +
                    "OrderNumber: {OrderNumber}, " +
                    "UserId: {UserId}, " +
                    "TotalAmount: {TotalAmount}",
                    orderCreatedEvent.OrderId,
                    orderCreatedEvent.OrderNumber,
                    orderCreatedEvent.UserId,
                    orderCreatedEvent.TotalAmount);

                // Simulated asynchronous processing.
                await Task.CompletedTask;

                await _channel.BasicAckAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    cancellationToken);

                _logger.LogInformation(
                    "OrderCreated event processed successfully for OrderId {OrderId}.",
                    orderCreatedEvent.OrderId);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to process OrderCreated RabbitMQ message.");

                await HandleFailureAsync(
                    eventArgs,
                    cancellationToken);
            }
        }
    }

    private async Task HandleFailureAsync(
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellationToken)
    {
        if (_channel is null)
        {
            return;
        }

        var redelivered =
            eventArgs.Redelivered;

        if (!redelivered)
        {
            _logger.LogWarning(
                "RabbitMQ processing failed. Message will be retried once.");

            await _channel.BasicNackAsync(
                eventArgs.DeliveryTag,
                multiple: false,
                requeue: true,
                cancellationToken);
        }
        else
        {
            _logger.LogError(
                "RabbitMQ processing failed after retry. " +
                "Message will be rejected.");

            await _channel.BasicNackAsync(
                eventArgs.DeliveryTag,
                multiple: false,
                requeue: false,
                cancellationToken);
        }
    }

    private async Task CleanupAsync()
    {
        if (_channel is not null)
        {
            try
            {
                await _channel.DisposeAsync();
            }
            catch (Exception exception)
            {
                _logger.LogDebug(
                    exception,
                    "Error while disposing RabbitMQ channel.");
            }

            _channel = null;
        }

        if (_connection is not null)
        {
            try
            {
                await _connection.DisposeAsync();
            }
            catch (Exception exception)
            {
                _logger.LogDebug(
                    exception,
                    "Error while disposing RabbitMQ connection.");
            }

            _connection = null;
        }
    }

    public override async Task StopAsync(
        CancellationToken cancellationToken)
    {
        await CleanupAsync();

        await base.StopAsync(
            cancellationToken);
    }
}