using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;

namespace DocumentManagementSystem.OcrWorker.Services;

public class MinioStorageService : IStorageService
{
	private readonly IMinioClient _minioClient;
	private readonly ILogger<MinioStorageService> _logger;

	public MinioStorageService(IMinioClient minioClient, ILogger<MinioStorageService> logger)
	{
		_minioClient = minioClient;
		_logger = logger;
	}

	public async Task<Stream> DownloadFileAsync(string objectPath, CancellationToken ct = default)
	{
		// Extract bucket and object name from path (format: "bucket/objectname")
		var parts = objectPath.Split('/', 2);
		var bucket = parts[0];
		var objectName = parts[1];

		_logger.LogInformation("Downloading file from MinIO: {ObjectPath}", objectPath);

		var memoryStream = new MemoryStream();

		await _minioClient.GetObjectAsync(new GetObjectArgs()
			.WithBucket(bucket)
			.WithObject(objectName)
			.WithCallbackStream(stream =>
			{
				stream.CopyTo(memoryStream);
			}), ct);

		memoryStream.Position = 0;
		_logger.LogInformation("File downloaded successfully from MinIO: {ObjectPath}, Size: {Size} bytes", objectPath, memoryStream.Length);
		return memoryStream;
	}
}
