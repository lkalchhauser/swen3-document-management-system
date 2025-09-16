using DocumentManagementSystem.Model.ORM;

namespace DocumentManagementService.DAL.Repositories.Interfaces
{
	public interface IDocumentRepository
	{
		Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default);
		Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken ct = default);
		Task AddAsync(Document document, CancellationToken ct = default);
		Task UpdateAsync(Document document, CancellationToken ct = default);
		Task DeleteAsync(Document document, CancellationToken ct = default);
		Task SaveChangesAsync(CancellationToken ct = default);
	}
}
