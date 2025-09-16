using DocumentManagementService.DAL.Repositories.Interfaces;
using DocumentManagementSystem.Model.ORM;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagementService.DAL.Repositories
{
	public class DocumentRepository : IDocumentRepository
	{
		private readonly DocumentManagementServiceContext _context;

		public DocumentRepository(DocumentManagementServiceContext context)
		{
			_context = context;
		}

		public async Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
			 await _context.Documents
				  .Include(d => d.Metadata)
				  .Include(d => d.Tags)
				  .FirstOrDefaultAsync(d => d.Id == id, ct);

		public async Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken ct = default) =>
			 await _context.Documents
				  .Include(d => d.Metadata)
				  .Include(d => d.Tags)
				  .ToListAsync(ct);

		public async Task AddAsync(Document document, CancellationToken ct = default) =>
			 await _context.Documents.AddAsync(document, ct);

		public Task UpdateAsync(Document document, CancellationToken ct = default)
		{
			_context.Documents.Update(document);
			return Task.CompletedTask;
		}

		public Task DeleteAsync(Document document, CancellationToken ct = default)
		{
			_context.Documents.Remove(document);
			return Task.CompletedTask;
		}

		public async Task SaveChangesAsync(CancellationToken ct = default) =>
			 await _context.SaveChangesAsync(ct);
	}
}
