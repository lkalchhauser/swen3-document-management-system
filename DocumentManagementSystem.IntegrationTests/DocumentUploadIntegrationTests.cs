using DocumentManagementSystem.Model.DTO;
using System.Net;
using System.Net.Http.Json;

namespace DocumentManagementSystem.IntegrationTests;

public class DocumentUploadIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
	private readonly HttpClient _client;
	private readonly CustomWebApplicationFactory _factory;

	public DocumentUploadIntegrationTests(CustomWebApplicationFactory factory)
	{
		_factory = factory;
		_client = factory.CreateClient();
	}

	[Fact]
	public async Task Upload_ValidFile_CreatesDocumentAndReturnsSuccess()
	{
		// Arrange
		var content = new MultipartFormDataContent();
		var fileContent = new ByteArrayContent(new byte[] { 0x25, 0x50, 0x44, 0x46 });
		fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
		content.Add(fileContent, "file", "upload-test.pdf");
		content.Add(new StringContent("important,upload,test"), "tags");

		// Act
		var response = await _client.PostAsync("/api/document/upload", content);

		// Assert
		Assert.Equal(HttpStatusCode.Created, response.StatusCode);

		var createdDoc = await response.Content.ReadFromJsonAsync<DocumentDTO>();
		Assert.NotNull(createdDoc);
		Assert.Equal("upload-test.pdf", createdDoc.FileName);
		Assert.Equal(4, createdDoc.Metadata?.FileSize);
		Assert.Equal("application/pdf", createdDoc.Metadata?.ContentType);
		Assert.Equal(3, createdDoc.Tags.Count);
		Assert.Contains("important", createdDoc.Tags);
		Assert.Contains("upload", createdDoc.Tags);
		Assert.Contains("test", createdDoc.Tags);
	}

	[Fact]
	public async Task Upload_FileWithoutTags_CreatesDocumentSuccessfully()
	{
		// Arrange
		var content = new MultipartFormDataContent();
		var fileContent = new ByteArrayContent(new byte[] { 0x01, 0x02, 0x03 });
		fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
		content.Add(fileContent, "file", "no-tags.txt");

		// Act
		var response = await _client.PostAsync("/api/document/upload", content);

		// Assert
		Assert.Equal(HttpStatusCode.Created, response.StatusCode);

		var createdDoc = await response.Content.ReadFromJsonAsync<DocumentDTO>();
		Assert.NotNull(createdDoc);
		Assert.Equal("no-tags.txt", createdDoc.FileName);
		Assert.Empty(createdDoc.Tags);
	}

	[Fact]
	public async Task Upload_EmptyFile_ReturnsBadRequest()
	{
		// Arrange
		var content = new MultipartFormDataContent();
		var fileContent = new ByteArrayContent(Array.Empty<byte>());
		fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
		content.Add(fileContent, "file", "empty.pdf");

		// Act
		var response = await _client.PostAsync("/api/document/upload", content);

		// Assert
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task Upload_NoFile_ReturnsBadRequest()
	{
		// Arrange
		var content = new MultipartFormDataContent();

		// Act
		var response = await _client.PostAsync("/api/document/upload", content);

		// Assert
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task Upload_LargeFile_CreatesDocumentWithCorrectSize()
	{
		// Arrange
		var largeContent = new byte[1024 * 100];
		new Random().NextBytes(largeContent);

		var content = new MultipartFormDataContent();
		var fileContent = new ByteArrayContent(largeContent);
		fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
		content.Add(fileContent, "file", "large-file.bin");

		// Act
		var response = await _client.PostAsync("/api/document/upload", content);

		// Assert
		Assert.Equal(HttpStatusCode.Created, response.StatusCode);

		var createdDoc = await response.Content.ReadFromJsonAsync<DocumentDTO>();
		Assert.NotNull(createdDoc);
		Assert.Equal("large-file.bin", createdDoc.FileName);
		Assert.Equal(1024 * 100, createdDoc.Metadata?.FileSize);
	}

	[Fact]
	public async Task Upload_TagsWithSpaces_ParsesCorrectly()
	{
		// Arrange
		var content = new MultipartFormDataContent();
		var fileContent = new ByteArrayContent(new byte[] { 0x01 });
		fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
		content.Add(fileContent, "file", "tagged.txt");
		content.Add(new StringContent("  tag1  ,  tag2  , tag3  "), "tags");

		// Act
		var response = await _client.PostAsync("/api/document/upload", content);

		// Assert
		Assert.Equal(HttpStatusCode.Created, response.StatusCode);

		var createdDoc = await response.Content.ReadFromJsonAsync<DocumentDTO>();
		Assert.NotNull(createdDoc);
		Assert.Equal(3, createdDoc.Tags.Count);
		Assert.Contains("tag1", createdDoc.Tags);
		Assert.Contains("tag2", createdDoc.Tags);
		Assert.Contains("tag3", createdDoc.Tags);
	}
}
