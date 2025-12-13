namespace DocumentManagementSystem.Application.Services.Interfaces;

public interface IStorageService
{
	Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);

	Task<Stream> DownloadFileAsync(string objectPath, CancellationToken ct = default);

	Task<Stream> DownloadFileAsync(string bucketName, string objectName, CancellationToken ct = default);

	Task DeleteFileAsync(string objectPath, CancellationToken ct = default);
}
