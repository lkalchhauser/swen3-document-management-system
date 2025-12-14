using DocumentManagementSystem.Application.Services.Interfaces;
using DocumentManagementSystem.Messaging;
using DocumentManagementSystem.Messaging.Model;
using DocumentManagementSystem.Model.DTO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocumentManagementSystem.OcrWorker.Services;

public sealed class OcrWorkerService : MessageConsumerService<DocumentUploadMessageDTO>
{
	private readonly ILogger<OcrWorkerService> _logger;
	private readonly IServiceProvider _serviceProvider;

	public OcrWorkerService(
		IOptions<RabbitMQOptions> options,
		ILogger<OcrWorkerService> logger,
		IServiceProvider serviceProvider)
		: base(options, logger)
	{
		_logger = logger;
		_serviceProvider = serviceProvider;
	}

	protected override async Task HandleMessageAsync(DocumentUploadMessageDTO msg, CancellationToken ct)
	{
		_logger.LogInformation("Processing document upload: DocumentId={DocumentId}, FileName={FileName}",
			msg.DocumentId, msg.FileName);
		_logger.LogDebug("Document uploaded at {UploadedAt}, StoragePath={StoragePath}",
			msg.UploadedAtUtc, msg.StoragePath ?? "null");

		if (string.IsNullOrEmpty(msg.StoragePath))
		{
			_logger.LogWarning("StoragePath is null or empty for DocumentId={DocumentId}. Skipping OCR processing.",
				msg.DocumentId);
			return;
		}

		using var scope = _serviceProvider.CreateScope();
		var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
		var ocrService = scope.ServiceProvider.GetRequiredService<IOcrService>();
		var genAiService = scope.ServiceProvider.GetRequiredService<IGenAiService>();
		var documentUpdateService = scope.ServiceProvider.GetRequiredService<IDocumentUpdateService>();
		var searchService = scope.ServiceProvider.GetRequiredService<ISearchService>();
		var documentService = scope.ServiceProvider.GetRequiredService<IDocumentService>();

		try
		{
			_logger.LogInformation("Fetching document from storage: {StoragePath}", msg.StoragePath);
			await using var fileStream = await storageService.DownloadFileAsync(msg.StoragePath, ct);

			_logger.LogInformation("Starting OCR processing for DocumentId={DocumentId}", msg.DocumentId);
			var extractedText = await ocrService.ExtractTextFromPdfAsync(fileStream, ct);

			_logger.LogInformation("OCR Result for DocumentId={DocumentId}: {OcrText}", msg.DocumentId, extractedText);

			_logger.LogInformation("Starting AI summary generation for DocumentId={DocumentId}", msg.DocumentId);
			string aiSummary;

			try
			{
				aiSummary = await genAiService.GenerateSummaryAsync(extractedText, maxLength: 200, ct: ct);
				_logger.LogInformation("AI Summary for DocumentId={DocumentId}: {Summary}", msg.DocumentId, aiSummary);
			}
			catch (Exception genAiEx)
			{
				_logger.LogError(genAiEx, "Failed to generate AI summary for DocumentId={DocumentId}. Using truncated text as fallback.",
					msg.DocumentId);

				aiSummary = extractedText.Length > 200
					? extractedText.Substring(0, 200) + "..."
					: extractedText;

				_logger.LogWarning("Using fallback summary (truncation) for DocumentId={DocumentId}", msg.DocumentId);
			}

			_logger.LogInformation("Updating document with OCR results and AI summary for DocumentId={DocumentId}", msg.DocumentId);
			var updateSuccess = await documentUpdateService.UpdateWithOcrAndSummaryAsync(msg.DocumentId, extractedText, aiSummary, ct);

			if (updateSuccess)
			{
				_logger.LogInformation("Document processing completed successfully for DocumentId={DocumentId} (OCR + AI Summary)", msg.DocumentId);

				var fullDoc = await documentService.GetByIdAsync(msg.DocumentId, ct);
				if (fullDoc != null)
				{
					await searchService.IndexDocumentAsync(fullDoc, ct);
					_logger.LogInformation("Document {DocumentId} successfully indexed in Elasticsearch", msg.DocumentId);
				}
			}
			else
			{
				_logger.LogWarning("Failed to update document for DocumentId={DocumentId}. Document may not exist.", msg.DocumentId);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error processing document DocumentId={DocumentId}: {Message}", msg.DocumentId, ex.Message);
		}
	}
}
