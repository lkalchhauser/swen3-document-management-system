namespace DocumentManagementSystem.OcrWorker.Services;

public interface IOcrService
{
	Task<string> ExtractTextFromPdfAsync(Stream pdfStream, CancellationToken ct = default);
}
