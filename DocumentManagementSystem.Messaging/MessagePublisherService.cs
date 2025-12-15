using DocumentManagementSystem.Messaging.Interfaces;
using DocumentManagementSystem.Messaging.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace DocumentManagementSystem.Messaging;

public class MessagePublisherService : IMessagePublisherService, IAsyncDisposable
{
	private readonly RabbitMQOptions _options;
	private readonly ILogger<MessagePublisherService> _logger;
	private readonly IConnection _connection;
	private readonly IChannel _channel;

	public MessagePublisherService(IOptions<RabbitMQOptions> options, ILogger<MessagePublisherService> logger, IConnection connection, IChannel channel)
	{
		_options = options.Value;
		_logger = logger;
		_connection = connection;
		_channel = channel;
	}

	public static async Task<MessagePublisherService> CreateAsync(
		IOptions<RabbitMQOptions> options, ILogger<MessagePublisherService> logger)
	{
		logger.LogInformation("Initializing MessagePublisherService, connecting to RabbitMQ at {HostName}:{Port}", options.Value.HostName, options.Value.Port);

		var factory = new ConnectionFactory
		{
			HostName = options.Value.HostName,
			Port = options.Value.Port,
			UserName = options.Value.Username,
			Password = options.Value.Password
		};

		try
		{
			var connection = await factory.CreateConnectionAsync();
			logger.LogInformation("Successfully connected to RabbitMQ");

			var channel = await connection.CreateChannelAsync();
			logger.LogDebug("RabbitMQ channel created");

			await channel.QueueDeclareAsync(
				queue: options.Value.QueueName,
				durable: true,
				exclusive: false,
				autoDelete: false,
				arguments: null
			);
			logger.LogInformation("Queue '{QueueName}' declared", options.Value.QueueName);

			return new MessagePublisherService(options, logger, connection, channel);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to initialize MessagePublisherService");
			throw;
		}
	}

	public async Task PublishAsync<T>(T message, CancellationToken ct = default)
	{
		try
		{
			_logger.LogDebug("Serializing message of type {MessageType}", typeof(T).Name);
			var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

			var props = new BasicProperties { Persistent = true };

			await _channel.BasicPublishAsync(
				exchange: "",
				routingKey: _options.QueueName,
				mandatory: false,
				basicProperties: props,
				body: body,
				cancellationToken: ct
			);

			_logger.LogInformation("Published message to queue '{QueueName}', Type={MessageType}", _options.QueueName, typeof(T).Name);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error publishing message of type {MessageType} to queue '{QueueName}'", typeof(T).Name, _options.QueueName);
			throw;
		}
	}

	public async ValueTask DisposeAsync()
	{
		_logger.LogInformation("Disposing MessagePublisherService");
		try
		{
			await _channel.CloseAsync();
			await _channel.DisposeAsync();
			_logger.LogDebug("RabbitMQ channel disposed");

			await _connection.CloseAsync();
			await _connection.DisposeAsync();
			_logger.LogDebug("RabbitMQ connection disposed");
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Error disposing RabbitMQ resources");
		}
	}
}
