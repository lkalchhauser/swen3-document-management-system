using DocumentManagementSystem.DAL.Repositories.Interfaces;
using DocumentManagementSystem.Model.ORM;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocumentManagementSystem.DAL.Repositories
{
	public class DocumentRepository : IDocumentRepository
	{
		private readonly DocumentManagementSystemContext _context;
		private readonly ILogger<DocumentRepository> _logger;

		public DocumentRepository(DocumentManagementSystemContext context, ILogger<DocumentRepository> logger)
		{
			_context = context;
			_logger = logger;
		}

		public async Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default)
		{
			_logger.LogDebug("Querying database for document {DocumentId}", id);
			var doc = await _context.Documents
				.Include(d => d.Metadata)
				.Include(d => d.Tags)
				.Include(d => d.Notes)
				.FirstOrDefaultAsync(d => d.Id == id, ct);

			if (doc is null)
			{
				_logger.LogDebug("Document {DocumentId} not found in database", id);
			}
			return doc;
		}

		public async Task<Document?> GetByIdWithMetadataAsync(Guid id, CancellationToken ct = default)
		{
			_logger.LogDebug("Querying database for document {DocumentId} with metadata", id);
			var doc = await _context.Documents
				.Include(d => d.Metadata)
				.FirstOrDefaultAsync(d => d.Id == id, ct);

			if (doc is null)
			{
				_logger.LogDebug("Document {DocumentId} not found in database", id);
			}
			return doc;
		}

		public async Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken ct = default)
		{
			_logger.LogDebug("Querying database for all documents");
			var docs = await _context.Documents
				.Include(d => d.Metadata)
				.Include(d => d.Tags)
				.ToListAsync(ct);
			_logger.LogDebug("Query returned {Count} documents from database", docs.Count);
			return docs;
		}

		public async Task AddAsync(Document document, CancellationToken ct = default)
		{
			_logger.LogDebug("Adding document {DocumentId} to database context", document.Id);
			await _context.Documents.AddAsync(document, ct);
		}

		public Task UpdateAsync(Document document, CancellationToken ct = default)
		{
			_logger.LogDebug("Updating document {DocumentId} in database context", document.Id);
			_context.Documents.Update(document);
			return Task.CompletedTask;
		}

		public Task DeleteAsync(Document document, CancellationToken ct = default)
		{
			_logger.LogDebug("Deleting document {DocumentId} from database context", document.Id);
			_context.Documents.Remove(document);
			return Task.CompletedTask;
		}

		public async Task SaveChangesAsync(CancellationToken ct = default)
		{
			_logger.LogDebug("Saving changes to database");
			var changeCount = await _context.SaveChangesAsync(ct);
			_logger.LogDebug("Saved {ChangeCount} changes to database", changeCount);
		}
	}
}
