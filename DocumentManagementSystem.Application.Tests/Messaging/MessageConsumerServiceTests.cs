using DocumentManagementSystem.Messaging;
using DocumentManagementSystem.Messaging.Model;
using DocumentManagementSystem.Model.DTO;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace DocumentManagementSystem.Application.Tests.Messaging;

public class TestMessageConsumer : MessageConsumerService<DocumentUploadMessageDTO>
{
	public DocumentUploadMessageDTO? LastProcessedMessage { get; private set; }
	public int MessageCount { get; private set; }

	public TestMessageConsumer(IOptions<RabbitMQOptions> options, ILogger<TestMessageConsumer> logger)
		: base(options, logger)
	{
	}

	protected override Task HandleMessageAsync(DocumentUploadMessageDTO message, CancellationToken ct = default)
	{
		LastProcessedMessage = message;
		MessageCount++;
		return Task.CompletedTask;
	}

	public Task TestHandleMessageAsync(DocumentUploadMessageDTO message, CancellationToken ct = default)
	{
		return HandleMessageAsync(message, ct);
	}
}

public sealed class MessageConsumerServiceTests
{
	private readonly Mock<IOptions<RabbitMQOptions>> _mockOptions;
	private readonly Mock<ILogger<TestMessageConsumer>> _mockLogger;
	private readonly RabbitMQOptions _options;

	public MessageConsumerServiceTests()
	{
		_options = new RabbitMQOptions
		{
			HostName = "localhost",
			Port = 5672,
			Username = "guest",
			Password = "guest",
			QueueName = "test_queue"
		};

		_mockOptions = new Mock<IOptions<RabbitMQOptions>>();
		_mockOptions.Setup(o => o.Value).Returns(_options);

		_mockLogger = new Mock<ILogger<TestMessageConsumer>>();
	}

	[Fact]
	public void Constructor_InitializesWithOptions()
	{
		// Act
		var consumer = new TestMessageConsumer(_mockOptions.Object, _mockLogger.Object);

		// Assert
		Assert.NotNull(consumer);
		Assert.IsAssignableFrom<BackgroundService>(consumer);
	}

	[Fact]
	public async Task HandleMessageAsync_ProcessesMessage_UpdatesProperties()
	{
		// Arrange
		var consumer = new TestMessageConsumer(_mockOptions.Object, _mockLogger.Object);
		var message = new DocumentUploadMessageDTO(
			DocumentId: Guid.NewGuid(),
			FileName: "test.pdf",
			StoragePath: null,
			UploadedAtUtc: DateTimeOffset.UtcNow
		);

		// Act
		await consumer.TestHandleMessageAsync(message);

		// Assert
		Assert.Equal(message, consumer.LastProcessedMessage);
		Assert.Equal(1, consumer.MessageCount);
	}

	[Fact]
	public async Task StopAsync_CompletesSuccessfully()
	{
		// Arrange
		var consumer = new TestMessageConsumer(_mockOptions.Object, _mockLogger.Object);

		// Act
		var stopTask = consumer.StopAsync(CancellationToken.None);

		// Assert
		await stopTask;
		Assert.True(stopTask.IsCompleted);
	}
}
