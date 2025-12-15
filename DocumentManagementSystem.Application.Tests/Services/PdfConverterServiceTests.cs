using Docnet.Core.Exceptions;
using DocumentManagementSystem.OcrWorker.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocumentManagementSystem.Application.Tests.Services;

public class PdfConverterServiceTests
{
	private readonly Mock<ILogger<PdfConverterService>> _mockLogger;
	private readonly PdfConverterService _sut;

	public PdfConverterServiceTests()
	{
		_mockLogger = new Mock<ILogger<PdfConverterService>>();
		_sut = new PdfConverterService(_mockLogger.Object);
	}

	[Fact]
	public async Task ConvertToImagesAsync_WithEmptyStream_ShouldThrowArgumentNullException()
	{
		// Arrange
		using var emptyStream = new MemoryStream();

		// Act & Assert
		await Assert.ThrowsAsync<ArgumentNullException>(async () =>
			await _sut.ConvertToImagesAsync(emptyStream));
	}

	[Fact]
	public async Task ConvertToImagesAsync_WithInvalidPdf_ShouldThrowDocnetLoadDocumentException()
	{
		// Arrange 
		var invalidData = new byte[] { 0x00, 0x01, 0x02, 0x03 };
		using var stream = new MemoryStream(invalidData);

		// Act & Assert 
		await Assert.ThrowsAsync<DocnetLoadDocumentException>(async () =>
			await _sut.ConvertToImagesAsync(stream));
	}

	[Fact]
	public async Task ConvertToImagesAsync_WithNullStream_ShouldThrowException()
	{
		// Arrange
		Stream? nullStream = null;

		// Act & Assert
		await Assert.ThrowsAsync<NullReferenceException>(async () =>
			await _sut.ConvertToImagesAsync(nullStream!));
	}

	[Fact]
	public async Task ConvertToImagesAsync_WithCancellationToken_ShouldRespectCancellation()
	{
		// Arrange
		var invalidData = new byte[] { 0x25, 0x50, 0x44, 0x46 };
		using var pdfStream = new MemoryStream(invalidData);
		var cts = new CancellationTokenSource();
		cts.Cancel();

		// Act & Assert
		await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
			await _sut.ConvertToImagesAsync(pdfStream, cts.Token));
	}

	[Fact]
	public void Constructor_ShouldFollowDependencyInversionPrinciple()
	{
		// Assert 
		Assert.NotNull(_sut);

		var constructorParams = typeof(PdfConverterService).GetConstructors()[0].GetParameters();
		Assert.Single(constructorParams);
		Assert.Equal(typeof(ILogger<PdfConverterService>), constructorParams[0].ParameterType);
	}

	[Fact]
	public void ConvertToImagesAsync_ShouldBeAsync()
	{
		// Assert 
		var method = typeof(PdfConverterService).GetMethod(nameof(PdfConverterService.ConvertToImagesAsync));
		Assert.NotNull(method);
		Assert.True(method!.ReturnType.IsGenericType);
		Assert.Equal(typeof(Task<>), method.ReturnType.GetGenericTypeDefinition());
	}
}
