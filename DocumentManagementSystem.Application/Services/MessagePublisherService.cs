using System.Text;
using System.Text.Json;
using DocumentManagementSystem.Application.Services.Interfaces;
using DocumentManagementSystem.Model.Other;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DocumentManagementSystem.Application.Services;

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
		var factory = new ConnectionFactory
		{
			HostName = options.Value.HostName,
			Port = options.Value.Port,
			UserName = options.Value.Username,
			Password = options.Value.Password
		};

		var connection = await factory.CreateConnectionAsync();
		var channel = await connection.CreateChannelAsync();

		await channel.QueueDeclareAsync(
			queue: options.Value.QueueName,
			durable: true,
			exclusive: false,
			autoDelete: false,
			arguments: null
		);

		return new MessagePublisherService(options, logger, connection, channel);
	}

	public async Task PublishAsync<T>(T message, CancellationToken ct = default)
	{
		try
		{
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

			_logger.LogInformation("Published message to queue {QueueName}", _options.QueueName);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error publishing message");
		}
	}

	public async ValueTask DisposeAsync()
	{
		try
		{
			await _channel.CloseAsync();
			await _channel.DisposeAsync();
			await _connection.CloseAsync();
			await _channel.DisposeAsync();
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Error disposing RabbitMQ resources");
		}
	}
}