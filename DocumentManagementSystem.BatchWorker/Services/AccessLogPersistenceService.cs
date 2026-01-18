using DocumentManagementSystem.BatchWorker.Services.Interfaces;
using DocumentManagementSystem.DAL;
using DocumentManagementSystem.DAL.Repositories.Interfaces;
using DocumentManagementSystem.Model.ORM;
using Microsoft.Extensions.Logging;

namespace DocumentManagementSystem.BatchWorker.Services
{
	public class AccessLogPersistenceService : IAccessLogPersistenceService
	{
		private readonly ILogger<AccessLogPersistenceService> _logger;
		private readonly IDocumentAccessLogRepository _repository;
		private readonly IDocumentRepository _documentRepository;
		private readonly DocumentManagementSystemContext _context;

		public AccessLogPersistenceService(
			ILogger<AccessLogPersistenceService> logger,
			IDocumentAccessLogRepository repository,
			IDocumentRepository documentRepository,
			DocumentManagementSystemContext context)
		{
			_logger = logger;
			_repository = repository;
			_documentRepository = documentRepository;
			_context = context;
		}

		public async Task<AccessLogPersistenceResult> SaveAccessLogsAsync(AccessLogBatch batch, string? fileName = null, CancellationToken cancellationToken = default)
		{
			_logger.LogInformation("Saving {Count} access log entries for date {BatchDate}",
				batch.Entries.Count, batch.BatchDate);

			var processedCount = 0;
			var errorCount = 0;

			foreach (var entry in batch.Entries)
			{
				// Check if document exists
				if (!await _documentRepository.ExistsAsync(entry.DocumentId, cancellationToken))
				{
					// Document doesn't exist - log as error in BatchProcessingErrors table
					_logger.LogWarning("Document {DocumentId} not found, logging error", entry.DocumentId);

					var error = new BatchProcessingError
					{
						Id = Guid.NewGuid(),
						DocumentId = entry.DocumentId,
						BatchDate = batch.BatchDate,
						AccessCount = entry.AccessCount,
						ErrorMessage = "Document ID does not exist in database",
						FileName = fileName,
						CreatedAt = DateTimeOffset.UtcNow
					};

					await _context.BatchProcessingErrors.AddAsync(error, cancellationToken);
					errorCount++;
					continue;
				}

				// Update DocumentAccessLog (for historical tracking per date)
				var existingLog = await _repository.GetByDocumentAndDateAsync(
					entry.DocumentId, batch.BatchDate, cancellationToken);

				if (existingLog != null)
				{
					existingLog.AccessCount += entry.AccessCount;
					await _repository.UpdateAsync(existingLog, cancellationToken);
					_logger.LogDebug("Updated access log for document {DocumentId}, new count: {Count}",
						entry.DocumentId, existingLog.AccessCount);
				}
				else
				{
					var newLog = new DocumentAccessLog
					{
						Id = Guid.NewGuid(),
						DocumentId = entry.DocumentId,
						AccessDate = batch.BatchDate,
						AccessCount = entry.AccessCount,
						CreatedAt = DateTimeOffset.UtcNow
					};

					await _repository.AddAsync(newLog, cancellationToken);
					_logger.LogDebug("Created new access log for document {DocumentId}, count: {Count}",
						entry.DocumentId, newLog.AccessCount);
				}

				// Update Documents.AccessCount (total access count in Documents table)
				await _documentRepository.IncrementAccessCountAsync(entry.DocumentId, entry.AccessCount, cancellationToken);
				_logger.LogDebug("Incremented access count for document {DocumentId} by {Count}",
					entry.DocumentId, entry.AccessCount);

				processedCount++;
			}

			// Save all changes
			await _context.SaveChangesAsync(cancellationToken);

			_logger.LogInformation("Processed {ProcessedCount} access logs, logged {ErrorCount} errors",
				processedCount, errorCount);

			return new AccessLogPersistenceResult
			{
				ProcessedCount = processedCount,
				ErrorCount = errorCount
			};
		}
	}
}
