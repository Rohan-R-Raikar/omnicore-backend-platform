using OmniCore.Shared.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
			HostName = _configuration["RabbitMq:HostName"] ?? "localhost",
			Port = _configuration.GetValue<int>("RabbitMq:Port"),
			UserName = _configuration["RabbitMq:UserName"] ?? "guest",
			Password = _configuration["RabbitMq:Password"] ?? "guest"
		};

		_connection =
			await factory.CreateConnectionAsync(stoppingToken);

		_channel =
			await _connection.CreateChannelAsync(
				cancellationToken: stoppingToken);

		var queueName =
			_configuration["RabbitMq:QueueName"]
			?? "omnicore.order.created";

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

		var consumer = new AsyncEventingBasicConsumer(_channel);

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

		await Task.Delay(
			Timeout.Infinite,
			stoppingToken);
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

		try
		{
			var json =
				Encoding.UTF8.GetString(
					eventArgs.Body.ToArray());

			var orderCreatedEvent =
				JsonSerializer.Deserialize<OrderCreatedEvent>(json);

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
				"OrderId: {OrderId}, OrderNumber: {OrderNumber}, " +
				"UserId: {UserId}, TotalAmount: {TotalAmount}",
				orderCreatedEvent.OrderId,
				orderCreatedEvent.OrderNumber,
				orderCreatedEvent.UserId,
				orderCreatedEvent.TotalAmount);

			// Simulated async processing.
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

	private async Task HandleFailureAsync(
		BasicDeliverEventArgs eventArgs,
		CancellationToken cancellationToken)
	{
		if (_channel is null)
		{
			return;
		}

		var redelivered = eventArgs.Redelivered;

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
				"RabbitMQ processing failed after retry. Message will be rejected.");

			await _channel.BasicNackAsync(
				eventArgs.DeliveryTag,
				multiple: false,
				requeue: false,
				cancellationToken);
		}
	}

	public override async Task StopAsync(
		CancellationToken cancellationToken)
	{
		if (_channel is not null)
		{
			await _channel.CloseAsync(
				cancellationToken);
		}

		if (_connection is not null)
		{
			await _connection.CloseAsync(
				cancellationToken);
		}

		await base.StopAsync(cancellationToken);
	}
}