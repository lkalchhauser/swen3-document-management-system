using DocumentManagementSystem.Model.DTO;

namespace DocumentManagementSystem.Application.Services.Interfaces;

public interface ISearchService
{
	Task IndexDocumentAsync(DocumentDTO document, CancellationToken ct = default);
	Task<IReadOnlyList<DocumentDTO>> SearchAsync(string searchTerm, CancellationToken ct = default);
}