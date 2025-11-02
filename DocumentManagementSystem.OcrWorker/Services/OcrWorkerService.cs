using DocumentManagementSystem.DAL;
using DocumentManagementSystem.Messaging;
using DocumentManagementSystem.Messaging.Model;
using DocumentManagementSystem.Model.DTO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocumentManagementSystem.OcrWorker.Services;

public sealed class OcrWorkerService : MessageConsumerService<DocumentUploadMessageDTO>
{
	private readonly ILogger<OcrWorkerService> _logger;
	private readonly IServiceProvider _serviceProvider;

	public OcrWorkerService(IOptions<RabbitMQOptions> options, ILogger<OcrWorkerService> logger, IServiceProvider serviceProvider)
		: base(options, logger)
	{
		_logger = logger;
		_serviceProvider = serviceProvider;
	}

	protected override async Task HandleMessageAsync(DocumentUploadMessageDTO msg, CancellationToken ct)
	{
		_logger.LogInformation("Processing document upload: DocumentId={DocumentId}, FileName={FileName}", msg.DocumentId, msg.FileName);
		_logger.LogDebug("Document uploaded at {UploadedAt}, StoragePath={StoragePath}", msg.UploadedAtUtc, msg.StoragePath ?? "null");

		if (string.IsNullOrEmpty(msg.StoragePath))
		{
			_logger.LogWarning("StoragePath is null or empty for DocumentId={DocumentId}. Skipping OCR processing.", msg.DocumentId);
			return;
		}

		// Create a scope for scoped services
		using var scope = _serviceProvider.CreateScope();
		var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
		var ocrService = scope.ServiceProvider.GetRequiredService<IOcrService>();
		var dbContext = scope.ServiceProvider.GetRequiredService<DocumentManagementSystemContext>();

		try
		{
			// Step 1: Download file from MinIO
			_logger.LogInformation("Fetching document from MinIO: {StoragePath}", msg.StoragePath);
			await using var fileStream = await storageService.DownloadFileAsync(msg.StoragePath, ct);

			// Step 2: Perform OCR processing
			_logger.LogInformation("Starting OCR processing for DocumentId={DocumentId}", msg.DocumentId);
			var extractedText = await ocrService.ExtractTextFromPdfAsync(fileStream, ct);

			// Log the OCR result (as required by assignment)
			_logger.LogInformation("OCR Result for DocumentId={DocumentId}: {OcrText}", msg.DocumentId, extractedText);

			// Step 3: Update database with OCR results
			_logger.LogInformation("Updating database with OCR results for DocumentId={DocumentId}", msg.DocumentId);
			var document = await dbContext.Documents
				.Include(d => d.Metadata)
				.FirstOrDefaultAsync(d => d.Id == msg.DocumentId, ct);

			if (document?.Metadata != null)
			{
				document.Metadata.OcrText = extractedText;
				document.Metadata.Summary = extractedText.Length > 200
					? extractedText.Substring(0, 200) + "..."
					: extractedText;
				document.Metadata.UpdatedAt = DateTimeOffset.UtcNow;

				await dbContext.SaveChangesAsync(ct);
				_logger.LogInformation("Database updated successfully for DocumentId={DocumentId}", msg.DocumentId);
			}
			else
			{
				_logger.LogWarning("Document or metadata not found in database for DocumentId={DocumentId}", msg.DocumentId);
			}

			_logger.LogInformation("OCR processing completed successfully for DocumentId={DocumentId}", msg.DocumentId);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error processing document DocumentId={DocumentId}: {Message}", msg.DocumentId, ex.Message);
			// Don't throw - we don't want to requeue the message indefinitely
		}
	}
}
