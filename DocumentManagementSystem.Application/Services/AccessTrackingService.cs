using DocumentManagementSystem.Application.Services.Interfaces;
using DocumentManagementSystem.DAL.Repositories.Interfaces;
using DocumentManagementSystem.Model.ORM;
using Microsoft.Extensions.Logging;

namespace DocumentManagementSystem.Application.Services
{
	public class AccessTrackingService : IAccessTrackingService
	{
		private readonly ILogger<AccessTrackingService> _logger;
		private readonly IDocumentAccessLogRepository _repository;
		private readonly IDocumentRepository _documentRepository;

		public AccessTrackingService(
			ILogger<AccessTrackingService> logger,
			IDocumentAccessLogRepository repository,
			IDocumentRepository documentRepository)
		{
			_logger = logger;
			_repository = repository;
			_documentRepository = documentRepository;
		}

		public async Task TrackAccessAsync(Guid documentId, CancellationToken cancellationToken = default)
		{
			try
			{
				var today = DateOnly.FromDateTime(DateTime.UtcNow);

				// Update DocumentAccessLog (daily tracking)
				var existingLog = await _repository.GetByDocumentAndDateAsync(documentId, today, cancellationToken);

				if (existingLog != null)
				{
					existingLog.AccessCount++;
					await _repository.UpdateAsync(existingLog, cancellationToken);
				}
				else
				{
					var newLog = new DocumentAccessLog
					{
						Id = Guid.NewGuid(),
						DocumentId = documentId,
						AccessDate = today,
						AccessCount = 1,
						CreatedAt = DateTimeOffset.UtcNow
					};
					await _repository.AddAsync(newLog, cancellationToken);
				}

				// Update Documents.AccessCount (total count)
				await _documentRepository.IncrementAccessCountAsync(documentId, 1, cancellationToken);

				await _repository.SaveChangesAsync(cancellationToken);
				_logger.LogDebug("Tracked access for document {DocumentId}", documentId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to track access for document {DocumentId}", documentId);
			}
		}
	}
}
