using DocumentManagementSystem.Messaging;
using DocumentManagementSystem.Messaging.Model;
using DocumentManagementSystem.Model.DTO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;

namespace DocumentManagementSystem.Application.Tests.Messaging;

public sealed class MessagePublisherServiceTests
{
	private readonly Mock<IOptions<RabbitMQOptions>> _mockOptions;
	private readonly Mock<ILogger<MessagePublisherService>> _mockLogger;
	private readonly Mock<IConnection> _mockConnection;
	private readonly Mock<IChannel> _mockChannel;
	private readonly RabbitMQOptions _options;

	public MessagePublisherServiceTests()
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

		_mockLogger = new Mock<ILogger<MessagePublisherService>>();
		_mockConnection = new Mock<IConnection>();
		_mockChannel = new Mock<IChannel>();
	}

	[Fact]
	public async Task PublishAsync_ValidMessage_CallsBasicPublish()
	{
		// Arrange
		var service = new MessagePublisherService(
			_mockOptions.Object,
			_mockLogger.Object,
			_mockConnection.Object,
			_mockChannel.Object
		);

		var message = new DocumentUploadMessageDTO(
			DocumentId: Guid.NewGuid(),
			FileName: "test.pdf",
			StoragePath: "/path/to/file",
			UploadedAtUtc: DateTimeOffset.UtcNow
		);

		_mockChannel.Setup(c => c.BasicPublishAsync(
			It.IsAny<string>(),
			It.IsAny<string>(),
			It.IsAny<bool>(),
			It.IsAny<BasicProperties>(),
			It.IsAny<ReadOnlyMemory<byte>>(),
			It.IsAny<CancellationToken>()
		)).Returns(ValueTask.CompletedTask);

		// Act
		await service.PublishAsync(message);

		// Assert
		_mockChannel.Verify(c => c.BasicPublishAsync(
			"", 
			_options.QueueName, 
			false, 
			It.IsAny<BasicProperties>(),
			It.IsAny<ReadOnlyMemory<byte>>(),
			It.IsAny<CancellationToken>()
		), Times.Once);
	}

	[Fact]
	public async Task PublishAsync_SerializesMessageCorrectly()
	{
		// Arrange
		var service = new MessagePublisherService(
			_mockOptions.Object,
			_mockLogger.Object,
			_mockConnection.Object,
			_mockChannel.Object
		);

		var documentId = Guid.NewGuid();
		var message = new DocumentUploadMessageDTO(
			DocumentId: documentId,
			FileName: "test.pdf",
			StoragePath: null,
			UploadedAtUtc: DateTimeOffset.UtcNow
		);

		ReadOnlyMemory<byte> capturedBody = default;
		_mockChannel.Setup(c => c.BasicPublishAsync(
			It.IsAny<string>(),
			It.IsAny<string>(),
			It.IsAny<bool>(),
			It.IsAny<BasicProperties>(),
			It.IsAny<ReadOnlyMemory<byte>>(),
			It.IsAny<CancellationToken>()
		)).Callback<string, string, bool, BasicProperties, ReadOnlyMemory<byte>, CancellationToken>(
			(ex, rk, mand, props, body, ct) => capturedBody = body
		).Returns(ValueTask.CompletedTask);

		// Act
		await service.PublishAsync(message);

		// Assert
		var bodyString = System.Text.Encoding.UTF8.GetString(capturedBody.ToArray());
		Assert.Contains(documentId.ToString(), bodyString);
		Assert.Contains("test.pdf", bodyString);
	}

	[Fact]
	public async Task DisposeAsync_DisposesChannelAndConnection()
	{
		// Arrange
		_mockChannel.Setup(c => c.DisposeAsync())
			.Returns(ValueTask.CompletedTask);
		_mockConnection.Setup(c => c.DisposeAsync())
			.Returns(ValueTask.CompletedTask);

		var service = new MessagePublisherService(
			_mockOptions.Object,
			_mockLogger.Object,
			_mockConnection.Object,
			_mockChannel.Object
		);

		// Act
		await service.DisposeAsync();

		// Assert
		_mockChannel.Verify(c => c.DisposeAsync(), Times.Once);
		_mockConnection.Verify(c => c.DisposeAsync(), Times.Once);
	}
}
