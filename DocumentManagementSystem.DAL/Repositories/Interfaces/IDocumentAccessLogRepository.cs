using DocumentManagementSystem.Model.ORM;

namespace DocumentManagementSystem.DAL.Repositories.Interfaces
{
	public interface IDocumentAccessLogRepository
	{
		Task<DocumentAccessLog?> GetByDocumentAndDateAsync(Guid documentId, DateOnly accessDate, CancellationToken cancellationToken = default);
		Task<bool> DocumentExistsAsync(Guid documentId, CancellationToken cancellationToken = default);
		Task AddAsync(DocumentAccessLog accessLog, CancellationToken cancellationToken = default);
		Task UpdateAsync(DocumentAccessLog accessLog, CancellationToken cancellationToken = default);
		Task SaveChangesAsync(CancellationToken cancellationToken = default);
	}
}
