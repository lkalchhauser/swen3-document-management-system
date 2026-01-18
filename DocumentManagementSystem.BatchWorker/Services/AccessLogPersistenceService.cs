using DocumentManagementSystem.BatchWorker.Services.Interfaces;
using DocumentManagementSystem.DAL.Repositories.Interfaces;
using DocumentManagementSystem.Model.ORM;
using Microsoft.Extensions.Logging;

namespace DocumentManagementSystem.BatchWorker.Services
{
	public class AccessLogPersistenceService : IAccessLogPersistenceService
	{
		private readonly ILogger<AccessLogPersistenceService> _logger;
		private readonly IDocumentAccessLogRepository _repository;

		public AccessLogPersistenceService(
			ILogger<AccessLogPersistenceService> logger,
			IDocumentAccessLogRepository repository)
		{
			_logger = logger;
			_repository = repository;
		}

		public async Task SaveAccessLogsAsync(AccessLogBatch batch, CancellationToken cancellationToken = default)
		{
			_logger.LogInformation("Saving {Count} access log entries for date {BatchDate}",
				batch.Entries.Count, batch.BatchDate);

			var processedCount = 0;
			var skippedCount = 0;

			foreach (var entry in batch.Entries)
			{
				if (!await _repository.DocumentExistsAsync(entry.DocumentId, cancellationToken))
				{
					_logger.LogWarning("Document {DocumentId} not found, skipping entry", entry.DocumentId);
					skippedCount++;
					continue;
				}

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

				processedCount++;
			}

			await _repository.SaveChangesAsync(cancellationToken);

			_logger.LogInformation("Saved {ProcessedCount} access logs, skipped {SkippedCount}",
				processedCount, skippedCount);
		}
	}
}
