using DocumentManagementSystem.Messaging.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using DocumentManagementSystem.Messaging.Interfaces;

namespace DocumentManagementSystem.Messaging;

public abstract class MessageConsumerService<TMessage> : BackgroundService, IMessageConsumerService
{
	private readonly ILogger _logger;
	private readonly RabbitMQOptions _options;
	private IConnection? _connection;
	private IChannel? _channel;

	protected MessageConsumerService(IOptions<RabbitMQOptions> options, ILogger logger)
	{
		_options = options.Value;
		_logger = logger;
	}

	protected abstract Task HandleMessageAsync(TMessage message, CancellationToken ct = default);

	protected override async Task ExecuteAsync(CancellationToken ct = default)
	{
		_logger.LogInformation("Starting MessageConsumerService, connecting to RabbitMQ at {HostName}:{Port}", _options.HostName, _options.Port);

		var factory = new ConnectionFactory
		{
			HostName = _options.HostName,
			Port = _options.Port,
			UserName = _options.Username,
			Password = _options.Password
		};

		try
		{
			_connection = await factory.CreateConnectionAsync(ct);
			_logger.LogInformation("Successfully connected to RabbitMQ");

			_channel = await _connection.CreateChannelAsync(null, ct);
			_logger.LogDebug("RabbitMQ channel created");

			await _channel.QueueDeclareAsync(
				 _options.QueueName,
				 durable: true,
				 exclusive: false,
				 autoDelete: false,
				 arguments: null,
				 cancellationToken: ct);
			_logger.LogInformation("Queue '{QueueName}' declared", _options.QueueName);

			var consumer = new AsyncEventingBasicConsumer(_channel);
			consumer.ReceivedAsync += async (_, ea) =>
			{
				try
				{
					var body = Encoding.UTF8.GetString(ea.Body.ToArray());
					_logger.LogDebug("Received message from queue, DeliveryTag={DeliveryTag}", ea.DeliveryTag);

					var msg = JsonSerializer.Deserialize<TMessage>(body);
					if (msg is not null)
					{
						await HandleMessageAsync(msg, ct);
					}
					else
					{
						_logger.LogWarning("Failed to deserialize message, DeliveryTag={DeliveryTag}", ea.DeliveryTag);
					}

					await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);
					_logger.LogDebug("Message acknowledged, DeliveryTag={DeliveryTag}", ea.DeliveryTag);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error processing message, DeliveryTag={DeliveryTag}", ea.DeliveryTag);
					await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: ct);
				}
			};

			await _channel.BasicConsumeAsync(_options.QueueName, autoAck: false, consumer, cancellationToken: ct);
			_logger.LogInformation("Now consuming messages from queue '{QueueName}'", _options.QueueName);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to start MessageConsumerService");
			throw;
		}
	}

	public override async Task StopAsync(CancellationToken ct = default)
	{
		_logger.LogInformation("Stopping MessageConsumerService");
		if (_channel is not null)
		{
			await _channel.DisposeAsync();
			_logger.LogDebug("RabbitMQ channel disposed");
		}
		if (_connection is not null)
		{
			await _connection.DisposeAsync();
			_logger.LogDebug("RabbitMQ connection disposed");
		}
		await base.StopAsync(ct);
		_logger.LogInformation("MessageConsumerService stopped");
	}
}
