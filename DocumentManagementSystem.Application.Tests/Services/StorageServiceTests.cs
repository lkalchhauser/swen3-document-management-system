using System.Net;
using AutoFixture;
using DocumentManagementSystem.REST.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.DataModel.Response;
using Moq;
using Xunit;

namespace DocumentManagementSystem.Application.Tests.Services;

public class StorageServiceTests
{
	private readonly Fixture _fixture = new();
	private readonly Mock<IMinioClient> _mockMinioClient;
	private readonly Mock<IConfiguration> _mockConfiguration;
	private readonly Mock<ILogger<MinioStorageService>> _mockLogger;
	private readonly MinioStorageService _sut;

	public StorageServiceTests()
	{
		_mockMinioClient = new Mock<IMinioClient>();
		_mockConfiguration = new Mock<IConfiguration>();
		_mockLogger = new Mock<ILogger<MinioStorageService>>();

		// Setup configuration mock
		_mockConfiguration.Setup(x => x["MinIO:BucketName"]).Returns("documents");

		_sut = new MinioStorageService(_mockMinioClient.Object, _mockConfiguration.Object, _mockLogger.Object);
	}

	[Fact]
	public async Task UploadFileAsync_ShouldReturnStoragePath()
	{
		// Arrange
		var fileName = "test.pdf";
		var contentType = "application/pdf";
		var fileContent = _fixture.Create<byte[]>();
		using var fileStream = new MemoryStream(fileContent);

		// Mock BucketExists
		_mockMinioClient
			.Setup(x => x.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), default))
			.ReturnsAsync(true);

		// Mock PutObject
		_mockMinioClient
			.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectArgs>(), default))
			.ReturnsAsync(new PutObjectResponse(HttpStatusCode.OK, "application/pdf", new Dictionary<string, string>(), 0, "test-etag"));

		// Act
		var result = await _sut.UploadFileAsync(fileStream, fileName, contentType);

		// Assert
		Assert.NotNull(result);
		Assert.StartsWith("documents/", result);
		Assert.Contains(fileName, result);

		_mockMinioClient.Verify(x => x.PutObjectAsync(It.IsAny<PutObjectArgs>(), default), Times.Once);
	}

	[Fact]
	public async Task UploadFileAsync_ShouldCreateBucketIfNotExists()
	{
		// Arrange
		var fileName = "test.pdf";
		var contentType = "application/pdf";
		var fileContent = _fixture.Create<byte[]>();
		using var fileStream = new MemoryStream(fileContent);

		// Mock BucketExists to return false
		_mockMinioClient
			.Setup(x => x.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), default))
			.ReturnsAsync(false);

		// Mock MakeBucket
		_mockMinioClient
			.Setup(x => x.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), default))
			.Returns(Task.CompletedTask);

		// Mock PutObject
		_mockMinioClient
			.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectArgs>(), default))
			.ReturnsAsync(new PutObjectResponse(HttpStatusCode.OK, "application/pdf", new Dictionary<string, string>(), 0, "test-etag"));

		// Act
		var result = await _sut.UploadFileAsync(fileStream, fileName, contentType);

		// Assert
		Assert.NotNull(result);
		_mockMinioClient.Verify(x => x.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), default), Times.Once);
		_mockMinioClient.Verify(x => x.PutObjectAsync(It.IsAny<PutObjectArgs>(), default), Times.Once);
	}

	[Fact]
	public async Task DownloadFileAsync_ShouldReturnStream()
	{
		// Arrange
		var objectPath = "documents/test.pdf";
		var expectedContent = _fixture.Create<byte[]>();

		// Mock GetObject - GetObjectAsync returns Task<ObjectStat> but we don't use the return value
		// The actual file download happens via the callback parameter
		_mockMinioClient
			.Setup(x => x.GetObjectAsync(It.IsAny<GetObjectArgs>(), default))
			.Callback<GetObjectArgs, CancellationToken>((args, ct) =>
			{
				// Simulate the callback being called with a stream
				using var sourceStream = new MemoryStream(expectedContent);
				// This is a simplified test - in reality, MinIO calls the callback
			})
			.ReturnsAsync((ObjectStat)null!);

		// Act
		var result = await _sut.DownloadFileAsync(objectPath);

		// Assert
		Assert.NotNull(result);
		Assert.True(result.CanRead);

		_mockMinioClient.Verify(x => x.GetObjectAsync(It.IsAny<GetObjectArgs>(), default), Times.Once);
	}

	[Fact]
	public async Task DeleteFileAsync_ShouldCallRemoveObject()
	{
		// Arrange
		var objectPath = "documents/test.pdf";

		_mockMinioClient
			.Setup(x => x.RemoveObjectAsync(It.IsAny<RemoveObjectArgs>(), default))
			.Returns(Task.CompletedTask);

		// Act
		await _sut.DeleteFileAsync(objectPath);

		// Assert
		_mockMinioClient.Verify(x => x.RemoveObjectAsync(It.IsAny<RemoveObjectArgs>(), default), Times.Once);
	}
}
