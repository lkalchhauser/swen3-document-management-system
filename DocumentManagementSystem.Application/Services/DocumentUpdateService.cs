using DocumentManagementSystem.Application.Services.Interfaces;
using DocumentManagementSystem.DAL.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace DocumentManagementSystem.Application.Services;

public class DocumentUpdateService : IDocumentUpdateService
{
	private readonly IDocumentRepository _documentRepository;
	private readonly ILogger<DocumentUpdateService> _logger;

	public DocumentUpdateService(
		IDocumentRepository documentRepository,
		ILogger<DocumentUpdateService> logger)
	{
		_documentRepository = documentRepository;
		_logger = logger;
	}

	public async Task<bool> UpdateWithOcrAndSummaryAsync(Guid documentId, string ocrText, string aiSummary, CancellationToken ct = default)
	{
		_logger.LogInformation("Updating document {DocumentId} with OCR results and AI-generated summary", documentId);

		var document = await _documentRepository.GetByIdWithMetadataAsync(documentId, ct);

		if (document?.Metadata == null)
		{
			_logger.LogWarning("Document or metadata not found for DocumentId={DocumentId}", documentId);
			return false;
		}

		document.Metadata.OcrText = ocrText;
		document.Metadata.Summary = aiSummary;
		document.Metadata.UpdatedAt = DateTimeOffset.UtcNow;

		await _documentRepository.UpdateAsync(document, ct);
		await _documentRepository.SaveChangesAsync(ct);

		_logger.LogInformation("Successfully updated document {DocumentId} with OCR results and AI summary (length: {SummaryLength} chars)",
			documentId, aiSummary?.Length ?? 0);
		return true;
	}
}
