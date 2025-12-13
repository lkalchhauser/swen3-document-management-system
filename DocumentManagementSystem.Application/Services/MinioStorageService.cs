using DocumentManagementSystem.Application.Configuration;
using DocumentManagementSystem.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace DocumentManagementSystem.Application.Services;

public class MinioStorageService : IStorageService
{
	private readonly IMinioClient _minioClient;
	private readonly ILogger<MinioStorageService> _logger;
	private readonly MinioOptions _options;

	public MinioStorageService(
		IMinioClient minioClient,
		IOptions<MinioOptions> options,
		ILogger<MinioStorageService> logger)
	{
		_minioClient = minioClient;
		_options = options.Value;
		_logger = logger;
	}

	public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
	{
		await EnsureBucketExistsAsync(ct);

		var objectName = $"{Guid.NewGuid()}_{fileName}";

		_logger.LogInformation("Uploading file to MinIO: Bucket={Bucket}, Object={Object}, Size={Size} bytes",
			_options.BucketName, objectName, fileStream.Length);

		try
		{
			await _minioClient.PutObjectAsync(new PutObjectArgs()
				.WithBucket(_options.BucketName)
				.WithObject(objectName)
				.WithStreamData(fileStream)
				.WithObjectSize(fileStream.Length)
				.WithContentType(contentType), ct);

			var storagePath = $"{_options.BucketName}/{objectName}";
			_logger.LogInformation("File uploaded successfully to MinIO: {StoragePath}", storagePath);

			return storagePath;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error uploading file to MinIO: Bucket={Bucket}, Object={Object}",
				_options.BucketName, objectName);
			throw;
		}
	}

	public async Task<Stream> DownloadFileAsync(string objectPath, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(objectPath))
		{
			throw new ArgumentException("Object path cannot be null or empty", nameof(objectPath));
		}

		var (bucket, objectName) = ParseObjectPath(objectPath);
		return await DownloadFileAsync(bucket, objectName, ct);
	}

	public async Task<Stream> DownloadFileAsync(string bucketName, string objectName, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(bucketName))
		{
			throw new ArgumentException("Bucket name cannot be null or empty", nameof(bucketName));
		}

		if (string.IsNullOrWhiteSpace(objectName))
		{
			throw new ArgumentException("Object name cannot be null or empty", nameof(objectName));
		}

		_logger.LogInformation("Downloading file from MinIO: Bucket={Bucket}, Object={Object}", bucketName, objectName);

		try
		{
			var memoryStream = new MemoryStream();

			await _minioClient.GetObjectAsync(new GetObjectArgs()
				.WithBucket(bucketName)
				.WithObject(objectName)
				.WithCallbackStream(stream =>
				{
					stream.CopyTo(memoryStream);
				}), ct);

			memoryStream.Position = 0;
			_logger.LogInformation("File downloaded successfully from MinIO: Bucket={Bucket}, Object={Object}, Size={Size} bytes",
				bucketName, objectName, memoryStream.Length);

			return memoryStream;
		}
		catch (ObjectNotFoundException ex)
		{
			_logger.LogError(ex, "Object not found in MinIO: Bucket={Bucket}, Object={Object}", bucketName, objectName);
			throw new FileNotFoundException($"Object '{objectName}' not found in bucket '{bucketName}'", ex);
		}
		catch (BucketNotFoundException ex)
		{
			_logger.LogError(ex, "Bucket not found in MinIO: Bucket={Bucket}", bucketName);
			throw new InvalidOperationException($"Bucket '{bucketName}' not found", ex);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error downloading file from MinIO: Bucket={Bucket}, Object={Object}", bucketName, objectName);
			throw;
		}
	}

	public async Task DeleteFileAsync(string objectPath, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(objectPath))
		{
			throw new ArgumentException("Object path cannot be null or empty", nameof(objectPath));
		}

		var (bucket, objectName) = ParseObjectPath(objectPath);

		_logger.LogInformation("Deleting file from MinIO: Bucket={Bucket}, Object={Object}", bucket, objectName);

		try
		{
			await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
				.WithBucket(bucket)
				.WithObject(objectName), ct);

			_logger.LogInformation("File deleted successfully from MinIO: Bucket={Bucket}, Object={Object}", bucket, objectName);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error deleting file from MinIO: Bucket={Bucket}, Object={Object}", bucket, objectName);
			throw;
		}
	}

	private async Task EnsureBucketExistsAsync(CancellationToken ct = default)
	{
		try
		{
			bool found = await _minioClient.BucketExistsAsync(new BucketExistsArgs()
				.WithBucket(_options.BucketName), ct);

			if (!found)
			{
				_logger.LogInformation("Creating MinIO bucket: {BucketName}", _options.BucketName);
				await _minioClient.MakeBucketAsync(new MakeBucketArgs()
					.WithBucket(_options.BucketName), ct);
				_logger.LogInformation("MinIO bucket created: {BucketName}", _options.BucketName);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error ensuring bucket exists: {BucketName}", _options.BucketName);
			throw;
		}
	}

	private (string bucket, string objectName) ParseObjectPath(string objectPath)
	{
		var parts = objectPath.Split('/', 2);

		if (parts.Length < 2)
		{
			throw new ArgumentException(
				$"Invalid object path format: '{objectPath}'. Expected format: 'bucket/objectname'",
				nameof(objectPath));
		}

		var bucket = parts[0];
		var objectName = parts[1];

		if (string.IsNullOrWhiteSpace(bucket) || string.IsNullOrWhiteSpace(objectName))
		{
			throw new ArgumentException(
				$"Invalid object path format: '{objectPath}'. Both bucket and object name must be non-empty",
				nameof(objectPath));
		}

		return (bucket, objectName);
	}
}
