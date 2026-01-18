using DocumentManagementSystem.DAL.Repositories.Interfaces;
using DocumentManagementSystem.Model.ORM;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagementSystem.DAL.Repositories
{
	public class DocumentAccessLogRepository : IDocumentAccessLogRepository
	{
		private readonly DocumentManagementSystemContext _context;

		public DocumentAccessLogRepository(DocumentManagementSystemContext context)
		{
			_context = context;
		}

		public async Task<DocumentAccessLog?> GetByDocumentAndDateAsync(Guid documentId, DateOnly accessDate, CancellationToken cancellationToken = default)
		{
			return await _context.DocumentAccessLogs
				.FirstOrDefaultAsync(dal => dal.DocumentId == documentId && dal.AccessDate == accessDate, cancellationToken);
		}

		public async Task<bool> DocumentExistsAsync(Guid documentId, CancellationToken cancellationToken = default)
		{
			return await _context.Documents.AnyAsync(d => d.Id == documentId, cancellationToken);
		}

		public async Task AddAsync(DocumentAccessLog accessLog, CancellationToken cancellationToken = default)
		{
			await _context.DocumentAccessLogs.AddAsync(accessLog, cancellationToken);
		}

		public Task UpdateAsync(DocumentAccessLog accessLog, CancellationToken cancellationToken = default)
		{
			_context.DocumentAccessLogs.Update(accessLog);
			return Task.CompletedTask;
		}

		public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
		{
			await _context.SaveChangesAsync(cancellationToken);
		}
	}
}
