using DocumentManagementSystem.Application.Services.Interfaces;
using DocumentManagementSystem.Messaging.Model;
using DocumentManagementSystem.Model.DTO;
using DocumentManagementSystem.OcrWorker.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DocumentManagementSystem.Application.Tests.Services;

public class OcrWorkerServiceTests
{
	private readonly Mock<IStorageService> _mockStorageService;
	private readonly Mock<IOcrService> _mockOcrService;
	private readonly Mock<IGenAiService> _mockGenAiService;
	private readonly Mock<IDocumentUpdateService> _mockDocumentUpdateService;
	private readonly Mock<ISearchService> _mockSearchService;
	private readonly Mock<IDocumentService> _mockDocumentService;
	private readonly Mock<ILogger<OcrWorkerService>> _mockLogger;
	private readonly Mock<IOptions<RabbitMQOptions>> _mockOptions;
	private readonly IServiceProvider _serviceProvider;

	public OcrWorkerServiceTests()
	{
		_mockStorageService = new Mock<IStorageService>();
		_mockOcrService = new Mock<IOcrService>();
		_mockGenAiService = new Mock<IGenAiService>();
		_mockDocumentUpdateService = new Mock<IDocumentUpdateService>();
		_mockSearchService = new Mock<ISearchService>();
		_mockDocumentService = new Mock<IDocumentService>();
		_mockLogger = new Mock<ILogger<OcrWorkerService>>();
		_mockOptions = new Mock<IOptions<RabbitMQOptions>>();

		_mockOptions.Setup(x => x.Value).Returns(new RabbitMQOptions
		{
			HostName = "localhost",
			Port = 5672,
			Username = "guest",
			Password = "guest",
			QueueName = "test_queue"
		});

		_mockGenAiService
			.Setup(x => x.GenerateSummaryAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync("AI-generated summary");

		var services = new ServiceCollection();
		services.AddScoped(_ => _mockStorageService.Object);
		services.AddScoped(_ => _mockOcrService.Object);
		services.AddScoped(_ => _mockGenAiService.Object);
		services.AddScoped(_ => _mockDocumentUpdateService.Object);
		services.AddScoped(_ => _mockSearchService.Object);     // NEW
		services.AddScoped(_ => _mockDocumentService.Object);   // NEW
		_serviceProvider = services.BuildServiceProvider();
	}

	[Fact]
	public async Task HandleMessageAsync_WithValidMessage_ShouldProcessSuccessfully()
	{
		// Arrange
		var documentId = Guid.NewGuid();
		var message = new DocumentUploadMessageDTO(
			DocumentId: documentId,
			FileName: "test.pdf",
			StoragePath: "documents/test.pdf",
			UploadedAtUtc: DateTimeOffset.UtcNow
		);

		var fileContent = new byte[] { 0x25, 0x50, 0x44, 0x46 };
		var extractedText = "This is extracted text from OCR";

		_mockStorageService
			.Setup(x => x.DownloadFileAsync(message.StoragePath, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new MemoryStream(fileContent));

		_mockOcrService
			.Setup(x => x.ExtractTextFromPdfAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(extractedText);

		_mockDocumentUpdateService
			.Setup(x => x.UpdateWithOcrAndSummaryAsync(documentId, extractedText, It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		// NEW: Mock fetching the document for indexing
		var fullDocDto = new DocumentDTO { Id = documentId, FileName = "test.pdf" };
		_mockDocumentService
			.Setup(x => x.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(fullDocDto);

		var sut = new OcrWorkerService(_mockOptions.Object, _mockLogger.Object, _serviceProvider);

		// Act
		await InvokeHandleMessageAsync(sut, message);

		// Assert
		_mockStorageService.Verify(
			x => x.DownloadFileAsync(message.StoragePath, It.IsAny<CancellationToken>()),
			Times.Once);

		_mockOcrService.Verify(
			x => x.ExtractTextFromPdfAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()),
			Times.Once);

		_mockDocumentUpdateService.Verify(
			x => x.UpdateWithOcrAndSummaryAsync(documentId, extractedText, It.IsAny<string>(), It.IsAny<CancellationToken>()),
			Times.Once);

		// NEW: Verify indexing was called
		_mockSearchService.Verify(
			x => x.IndexDocumentAsync(It.Is<DocumentDTO>(d => d.Id == documentId), It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task HandleMessageAsync_WithNullStoragePath_ShouldSkipProcessing()
	{
		// Arrange
		var message = new DocumentUploadMessageDTO(
			DocumentId: Guid.NewGuid(),
			FileName: "test.pdf",
			StoragePath: null,
			UploadedAtUtc: DateTimeOffset.UtcNow
		);

		var sut = new OcrWorkerService(_mockOptions.Object, _mockLogger.Object, _serviceProvider);

		// Act
		await InvokeHandleMessageAsync(sut, message);

		// Assert
		_mockStorageService.Verify(
			x => x.DownloadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
			Times.Never);

		_mockOcrService.Verify(
			x => x.ExtractTextFromPdfAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()),
			Times.Never);

		_mockDocumentUpdateService.Verify(
			x => x.UpdateWithOcrAndSummaryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task HandleMessageAsync_WithEmptyStoragePath_ShouldSkipProcessing()
	{
		// Arrange
		var message = new DocumentUploadMessageDTO(
			DocumentId: Guid.NewGuid(),
			FileName: "test.pdf",
			StoragePath: "",
			UploadedAtUtc: DateTimeOffset.UtcNow
		);

		var sut = new OcrWorkerService(_mockOptions.Object, _mockLogger.Object, _serviceProvider);

		// Act
		await InvokeHandleMessageAsync(sut, message);

		// Assert 
		_mockStorageService.Verify(
			x => x.DownloadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task HandleMessageAsync_WhenStorageServiceFails_ShouldNotPropagateException()
	{
		// Arrange
		var message = new DocumentUploadMessageDTO(
			DocumentId: Guid.NewGuid(),
			FileName: "test.pdf",
			StoragePath: "documents/test.pdf",
			UploadedAtUtc: DateTimeOffset.UtcNow
		);

		_mockStorageService
			.Setup(x => x.DownloadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new FileNotFoundException("File not found"));

		var sut = new OcrWorkerService(_mockOptions.Object, _mockLogger.Object, _serviceProvider);

		// Act 
		await InvokeHandleMessageAsync(sut, message);

		// Assert 
		_mockStorageService.Verify(
			x => x.DownloadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
			Times.Once);

		_mockOcrService.Verify(
			x => x.ExtractTextFromPdfAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task HandleMessageAsync_WhenOcrServiceFails_ShouldNotPropagateException()
	{
		// Arrange
		var message = new DocumentUploadMessageDTO(
			DocumentId: Guid.NewGuid(),
			FileName: "test.pdf",
			StoragePath: "documents/test.pdf",
			UploadedAtUtc: DateTimeOffset.UtcNow
		);

		_mockStorageService
			.Setup(x => x.DownloadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 }));

		_mockOcrService
			.Setup(x => x.ExtractTextFromPdfAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("OCR failed"));

		var sut = new OcrWorkerService(_mockOptions.Object, _mockLogger.Object, _serviceProvider);

		// Act 
		await InvokeHandleMessageAsync(sut, message);

		// Assert 
		_mockStorageService.Verify(
			x => x.DownloadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
			Times.Once);

		_mockOcrService.Verify(
			x => x.ExtractTextFromPdfAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()),
			Times.Once);

		_mockDocumentUpdateService.Verify(
			x => x.UpdateWithOcrAndSummaryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task HandleMessageAsync_WhenDocumentUpdateFails_ShouldNotPropagateException()
	{
		// Arrange
		var documentId = Guid.NewGuid();
		var message = new DocumentUploadMessageDTO(
			DocumentId: documentId,
			FileName: "test.pdf",
			StoragePath: "documents/test.pdf",
			UploadedAtUtc: DateTimeOffset.UtcNow
		);

		_mockStorageService
			.Setup(x => x.DownloadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 }));

		_mockOcrService
			.Setup(x => x.ExtractTextFromPdfAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync("Extracted text");

		_mockDocumentUpdateService
			.Setup(x => x.UpdateWithOcrAndSummaryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("Database update failed"));

		var sut = new OcrWorkerService(_mockOptions.Object, _mockLogger.Object, _serviceProvider);

		// Act 
		await InvokeHandleMessageAsync(sut, message);

		// Assert 
		_mockStorageService.Verify(
			x => x.DownloadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
			Times.Once);

		_mockOcrService.Verify(
			x => x.ExtractTextFromPdfAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()),
			Times.Once);

		_mockDocumentUpdateService.Verify(
			x => x.UpdateWithOcrAndSummaryAsync(documentId, "Extracted text", It.IsAny<string>(), It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task HandleMessageAsync_WhenDocumentDoesNotExist_ShouldLogWarning()
	{
		// Arrange
		var documentId = Guid.NewGuid();
		var message = new DocumentUploadMessageDTO(
			DocumentId: documentId,
			FileName: "test.pdf",
			StoragePath: "documents/test.pdf",
			UploadedAtUtc: DateTimeOffset.UtcNow
		);

		_mockStorageService
			.Setup(x => x.DownloadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 }));

		_mockOcrService
			.Setup(x => x.ExtractTextFromPdfAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync("Extracted text");

		_mockDocumentUpdateService
			.Setup(x => x.UpdateWithOcrAndSummaryAsync(documentId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		var sut = new OcrWorkerService(_mockOptions.Object, _mockLogger.Object, _serviceProvider);

		// Act
		await InvokeHandleMessageAsync(sut, message);

		// Assert 
		_mockDocumentUpdateService.Verify(
			x => x.UpdateWithOcrAndSummaryAsync(documentId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public void Constructor_ShouldFollowDependencyInversionPrinciple()
	{
		// Assert 
		var sut = new OcrWorkerService(_mockOptions.Object, _mockLogger.Object, _serviceProvider);
		Assert.NotNull(sut);

		var constructorParams = typeof(OcrWorkerService).GetConstructors()[0].GetParameters();
		Assert.Equal(3, constructorParams.Length);
		Assert.Equal(typeof(IOptions<RabbitMQOptions>), constructorParams[0].ParameterType);
		Assert.Equal(typeof(ILogger<OcrWorkerService>), constructorParams[1].ParameterType);
		Assert.Equal(typeof(IServiceProvider), constructorParams[2].ParameterType);
	}

	private async Task InvokeHandleMessageAsync(OcrWorkerService service, DocumentUploadMessageDTO message)
	{
		var method = typeof(OcrWorkerService).GetMethod(
			"HandleMessageAsync",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

		Assert.NotNull(method);

		var task = (Task)method!.Invoke(service, new object[] { message, CancellationToken.None })!;
		await task;
	}
}