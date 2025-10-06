using DocumentManagementSystem.Messaging;
using DocumentManagementSystem.Messaging.Model;
using DocumentManagementSystem.Model.DTO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocumentManagementSystem.OcrWorker.Services;

public sealed class OcrWorkerService : MessageConsumerService<DocumentUploadMessageDTO>
{
	public OcrWorkerService(IOptions<RabbitMQOptions> options, ILogger<OcrWorkerService> logger)
		: base(options, logger) { }

	protected override Task HandleMessageAsync(DocumentUploadMessageDTO msg, CancellationToken ct)
	{
		// For Sprint 3: just log the received message
		Console.WriteLine($"[OCR Worker] Received document {msg.DocumentId} - {msg.FileName}");
		return Task.CompletedTask;
	}
}