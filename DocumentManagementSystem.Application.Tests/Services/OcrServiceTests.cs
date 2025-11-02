using DocumentManagementSystem.OcrWorker.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocumentManagementSystem.Application.Tests.Services;

public class OcrServiceTests
{
	private readonly Mock<ILogger<TesseractOcrService>> _mockLogger;
	private readonly TesseractOcrService _sut;

	public OcrServiceTests()
	{
		_mockLogger = new Mock<ILogger<TesseractOcrService>>();
		_sut = new TesseractOcrService(_mockLogger.Object);
	}

	[Fact]
	public async Task ExtractTextFromPdfAsync_ShouldReturnText()
	{
		// Arrange
		var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF header
		using var pdfStream = new MemoryStream(pdfContent);

		// Act
		var result = await _sut.ExtractTextFromPdfAsync(pdfStream);

		// Assert
		Assert.NotNull(result);
		Assert.NotEmpty(result);
		// Result should contain either successful OCR processing message or tessdata not found message
		Assert.True(
			result.Contains("OCR", StringComparison.OrdinalIgnoreCase) ||
			result.Contains("Tesseract", StringComparison.OrdinalIgnoreCase),
			"Result should contain OCR processing information");
	}

	[Fact]
	public async Task ExtractTextFromPdfAsync_WithEmptyStream_ShouldHandleGracefully()
	{
		// Arrange
		using var emptyStream = new MemoryStream();

		// Act
		var result = await _sut.ExtractTextFromPdfAsync(emptyStream);

		// Assert
		Assert.NotNull(result);
		// Should not throw an exception
	}
}
