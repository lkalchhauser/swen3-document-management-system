using DocumentManagementSystem.Messaging.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace DocumentManagementSystem.Messaging;

public abstract class MessageConsumerService<TMessage> : BackgroundService
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

	protected abstract Task HandleMessageAsync(TMessage message, CancellationToken ct);

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var factory = new ConnectionFactory
		{
			HostName = _options.HostName,
			Port = _options.Port,
			UserName = _options.Username,
			Password = _options.Password
		};

		_connection = await factory.CreateConnectionAsync(stoppingToken);
		_channel = await _connection.CreateChannelAsync(null, stoppingToken);

		await _channel.QueueDeclareAsync(
			 _options.QueueName,
			 durable: true,
			 exclusive: false,
			 autoDelete: false,
			 arguments: null,
			 cancellationToken: stoppingToken);

		var consumer = new AsyncEventingBasicConsumer(_channel);
		consumer.ReceivedAsync += async (_, ea) =>
		{
			try
			{
				var body = Encoding.UTF8.GetString(ea.Body.ToArray());
				var msg = JsonSerializer.Deserialize<TMessage>(body);
				if (msg is not null)
				{
					await HandleMessageAsync(msg, stoppingToken);
				}

				await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing message");
				await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
			}
		};

		await _channel.BasicConsumeAsync(_options.QueueName, autoAck: false, consumer, cancellationToken: stoppingToken);
		_logger.LogInformation("Consuming from queue {Queue}", _options.QueueName);
	}

	public override async Task StopAsync(CancellationToken ct)
	{
		if (_channel is not null) await _channel.DisposeAsync();
		if (_connection is not null) await _connection.DisposeAsync();
		await base.StopAsync(ct);
	}
}
