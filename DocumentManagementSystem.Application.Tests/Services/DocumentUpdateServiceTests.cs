using DocumentManagementSystem.Application.Services;
using DocumentManagementSystem.Application.Services.Interfaces;
using DocumentManagementSystem.DAL.Repositories.Interfaces;
using DocumentManagementSystem.Model.ORM;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocumentManagementSystem.Application.Tests.Services;

public class DocumentUpdateServiceTests
{
	private readonly Mock<IDocumentRepository> _mockRepository;
	private readonly Mock<ILogger<DocumentUpdateService>> _mockLogger;
	private readonly IDocumentUpdateService _sut;

	public DocumentUpdateServiceTests()
	{
		_mockRepository = new Mock<IDocumentRepository>();
		_mockLogger = new Mock<ILogger<DocumentUpdateService>>();
		_sut = new DocumentUpdateService(_mockRepository.Object, _mockLogger.Object);
	}

	[Fact]
	public async Task UpdateWithOcrAndSummaryAsync_WithValidDocument_ShouldUpdateAndReturnTrue()
	{
		// Arrange
		var documentId = Guid.NewGuid();
		var ocrText = "This is extracted OCR text from the document.";
		var aiSummary = "AI-generated summary of the document";
		var document = new Document
		{
			Id = documentId,
			Metadata = new DocumentMetadata
			{
				Id = Guid.NewGuid(),
				DocumentId = documentId
			}
		};

		_mockRepository
			.Setup(x => x.GetByIdWithMetadataAsync(documentId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(document);

		// Act
		var result = await _sut.UpdateWithOcrAndSummaryAsync(documentId, ocrText, aiSummary);

		// Assert
		Assert.True(result);
		Assert.Equal(ocrText, document.Metadata.OcrText);
		Assert.Equal(aiSummary, document.Metadata.Summary);

		_mockRepository.Verify(x => x.UpdateAsync(document, It.IsAny<CancellationToken>()), Times.Once);
		_mockRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task UpdateWithOcrAndSummaryAsync_WithNonExistentDocument_ShouldReturnFalse()
	{
		// Arrange
		var documentId = Guid.NewGuid();
		var ocrText = "Some OCR text";
		var aiSummary = "AI-generated summary";

		_mockRepository
			.Setup(x => x.GetByIdWithMetadataAsync(documentId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((Document?)null);

		// Act
		var result = await _sut.UpdateWithOcrAndSummaryAsync(documentId, ocrText, aiSummary);

		// Assert
		Assert.False(result);
		_mockRepository.Verify(x => x.UpdateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Never);
		_mockRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task UpdateWithOcrAndSummaryAsync_WithDocumentButNoMetadata_ShouldReturnFalse()
	{
		// Arrange
		var documentId = Guid.NewGuid();
		var ocrText = "Some OCR text";
		var aiSummary = "AI-generated summary";
		var document = new Document
		{
			Id = documentId,
			Metadata = null
		};

		_mockRepository
			.Setup(x => x.GetByIdWithMetadataAsync(documentId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(document);

		// Act
		var result = await _sut.UpdateWithOcrAndSummaryAsync(documentId, ocrText, aiSummary);

		// Assert
		Assert.False(result);
		_mockRepository.Verify(x => x.UpdateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public void Constructor_ShouldFollowDependencyInversionPrinciple()
	{
		// Assert 
		Assert.NotNull(_sut);

		var constructorParams = typeof(DocumentUpdateService).GetConstructors()[0].GetParameters();
		Assert.Equal(2, constructorParams.Length);
		Assert.Equal(typeof(IDocumentRepository), constructorParams[0].ParameterType);
		Assert.Equal(typeof(ILogger<DocumentUpdateService>), constructorParams[1].ParameterType);
	}
}
