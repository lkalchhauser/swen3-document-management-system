namespace DocumentManagementSystem.OcrWorker.Services;

public interface IPdfConverterService
{
	Task<IReadOnlyList<byte[]>> ConvertToImagesAsync(Stream pdfStream, CancellationToken ct = default);
}
