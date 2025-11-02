using Minio;
using Minio.DataModel.Args;

namespace DocumentManagementSystem.REST.Services;

public class MinioStorageService : IStorageService
{
	private readonly IMinioClient _minioClient;
	private readonly string _bucketName;
	private readonly ILogger<MinioStorageService> _logger;

	public MinioStorageService(IMinioClient minioClient, IConfiguration configuration, ILogger<MinioStorageService> logger)
	{
		_minioClient = minioClient;
		_bucketName = configuration["MinIO:BucketName"] ?? "documents";
		_logger = logger;
	}

	public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
	{
		await EnsureBucketExistsAsync(ct);

		// Generate unique object name using GUID to avoid conflicts
		var objectName = $"{Guid.NewGuid()}_{fileName}";

		_logger.LogInformation("Uploading file to MinIO: {ObjectName}, Size: {Size} bytes", objectName, fileStream.Length);

		await _minioClient.PutObjectAsync(new PutObjectArgs()
			.WithBucket(_bucketName)
			.WithObject(objectName)
			.WithStreamData(fileStream)
			.WithObjectSize(fileStream.Length)
			.WithContentType(contentType), ct);

		_logger.LogInformation("File uploaded successfully to MinIO: {ObjectName}", objectName);
		return $"{_bucketName}/{objectName}";
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
		_logger.LogInformation("File downloaded successfully from MinIO: {ObjectPath}", objectPath);
		return memoryStream;
	}

	public async Task DeleteFileAsync(string objectPath, CancellationToken ct = default)
	{
		var parts = objectPath.Split('/', 2);
		var bucket = parts[0];
		var objectName = parts[1];

		_logger.LogInformation("Deleting file from MinIO: {ObjectPath}", objectPath);

		await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
			.WithBucket(bucket)
			.WithObject(objectName), ct);

		_logger.LogInformation("File deleted successfully from MinIO: {ObjectPath}", objectPath);
	}

	private async Task EnsureBucketExistsAsync(CancellationToken ct = default)
	{
		bool found = await _minioClient.BucketExistsAsync(new BucketExistsArgs()
			.WithBucket(_bucketName), ct);

		if (!found)
		{
			_logger.LogInformation("Creating MinIO bucket: {BucketName}", _bucketName);
			await _minioClient.MakeBucketAsync(new MakeBucketArgs()
				.WithBucket(_bucketName), ct);
			_logger.LogInformation("MinIO bucket created: {BucketName}", _bucketName);
		}
	}
}
