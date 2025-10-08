using DocumentManagementSystem.Messaging;
using DocumentManagementSystem.Messaging.Model;
using DocumentManagementSystem.Model.DTO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocumentManagementSystem.OcrWorker.Services;

public sealed class OcrWorkerService : MessageConsumerService<DocumentUploadMessageDTO>
{
	private readonly ILogger<OcrWorkerService> _logger;

	public OcrWorkerService(IOptions<RabbitMQOptions> options, ILogger<OcrWorkerService> logger)
		: base(options, logger)
	{
		_logger = logger;
	}

	protected override Task HandleMessageAsync(DocumentUploadMessageDTO msg, CancellationToken ct)
	{
		_logger.LogInformation("Processing document upload: DocumentId={DocumentId}, FileName={FileName}", msg.DocumentId, msg.FileName);
		// For Sprint 3: just log the received message
		_logger.LogDebug("Document uploaded at {UploadedAt}, StoragePath={StoragePath}", msg.UploadedAtUtc, msg.StoragePath ?? "null");
		return Task.CompletedTask;
	}
}
