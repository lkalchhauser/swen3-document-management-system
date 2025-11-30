namespace DocumentManagementSystem.Application.Services.Interfaces;

public interface IDocumentUpdateService
{
	Task<bool> UpdateWithOcrAndSummaryAsync(Guid documentId, string ocrText, string aiSummary, CancellationToken ct = default);
}
