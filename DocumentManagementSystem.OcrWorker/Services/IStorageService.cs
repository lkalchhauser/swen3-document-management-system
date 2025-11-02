namespace DocumentManagementSystem.OcrWorker.Services;

public interface IStorageService
{
	Task<Stream> DownloadFileAsync(string objectPath, CancellationToken ct = default);
}
