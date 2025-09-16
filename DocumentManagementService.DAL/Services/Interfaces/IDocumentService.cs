using DocumentManagementSystem.Model.DTO;

namespace DocumentManagementService.DAL.Services.Interfaces
{
	public interface IDocumentService
	{
		Task<DocumentDTO?> GetByIdAsync(Guid id, CancellationToken ct = default);
		Task<IReadOnlyList<DocumentDTO>> GetAllAsync(CancellationToken ct = default);
		Task<DocumentDTO> CreateAsync(DocumentCreateDTO dto, CancellationToken ct = default);
		Task<DocumentDTO?> UpdateAsync(Guid id, DocumentCreateDTO dto, CancellationToken ct = default);
		Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
	}
}
