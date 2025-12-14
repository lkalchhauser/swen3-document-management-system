using DocumentManagementSystem.Model.DTO;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace DocumentManagementSystem.IntegrationTests;

public class DocumentApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
	private readonly HttpClient _client;
	private readonly CustomWebApplicationFactory _factory;

	public DocumentApiIntegrationTests(CustomWebApplicationFactory factory)
	{
		_factory = factory;
		_client = factory.CreateClient();
	}

	[Fact]
	public async Task EndToEnd_CreateDocument_ReturnsCreated_AndCanBeRetrieved()
	{
		// Arrange
		var createDto = new DocumentCreateDTO
		{
			FileName = "integration-test.pdf",
			FileSize = 2048,
			ContentType = "application/pdf",
			Tags = new List<string> { "integration", "test" }
		};

		// Act 
		var createResponse = await _client.PostAsJsonAsync("/api/document", createDto);

		// Assert 
		Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
		var createdDoc = await createResponse.Content.ReadFromJsonAsync<DocumentDTO>();
		Assert.NotNull(createdDoc);
		Assert.Equal("integration-test.pdf", createdDoc.FileName);
		Assert.Equal(2, createdDoc.Tags.Count);
		Assert.Contains("integration", createdDoc.Tags);

		// Act 
		var getResponse = await _client.GetAsync($"/api/document/{createdDoc.Id}");

		// Assert 
		Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
		var retrievedDoc = await getResponse.Content.ReadFromJsonAsync<DocumentDTO>();
		Assert.NotNull(retrievedDoc);
		Assert.Equal(createdDoc.Id, retrievedDoc.Id);
		Assert.Equal("integration-test.pdf", retrievedDoc.FileName);

		_factory.MockMessagePublisher.Verify(
			 m => m.PublishAsync(It.IsAny<DocumentUploadMessageDTO>(), It.IsAny<CancellationToken>()),
			 Times.Once
		);
	}

	[Fact]
	public async Task EndToEnd_UpdateDocument_PersistsChanges()
	{
		// Arrange 
		var createDto = new DocumentCreateDTO
		{
			FileName = "original.pdf",
			FileSize = 1024,
			ContentType = "application/pdf",
			Tags = new List<string> { "original" }
		};

		var createResponse = await _client.PostAsJsonAsync("/api/document", createDto);
		var createdDoc = await createResponse.Content.ReadFromJsonAsync<DocumentDTO>();
		Assert.NotNull(createdDoc);

		var updateDto = new DocumentCreateDTO
		{
			FileName = "updated.pdf",
			FileSize = 3072,
			ContentType = "application/pdf",
			Tags = new List<string> { "updated", "modified" }
		};

		// Act 
		var updateResponse = await _client.PutAsJsonAsync($"/api/document/{createdDoc.Id}", updateDto);

		// Assert 
		Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
		var updatedDoc = await updateResponse.Content.ReadFromJsonAsync<DocumentDTO>();
		Assert.NotNull(updatedDoc);
		Assert.Equal("updated.pdf", updatedDoc.FileName);
		Assert.Equal(2, updatedDoc.Tags.Count);
		Assert.Contains("updated", updatedDoc.Tags);
		Assert.Contains("modified", updatedDoc.Tags);

		var getResponse = await _client.GetAsync($"/api/document/{createdDoc.Id}");
		var retrievedDoc = await getResponse.Content.ReadFromJsonAsync<DocumentDTO>();
		Assert.NotNull(retrievedDoc);
		Assert.Equal("updated.pdf", retrievedDoc.FileName);
	}

	[Fact]
	public async Task EndToEnd_DeleteDocument_RemovesFromDatabase()
	{
		// Arrange 
		var createDto = new DocumentCreateDTO
		{
			FileName = "to-delete.pdf",
			FileSize = 512,
			ContentType = "application/pdf",
			Tags = new List<string> { "delete-me" }
		};

		var createResponse = await _client.PostAsJsonAsync("/api/document", createDto);
		var createdDoc = await createResponse.Content.ReadFromJsonAsync<DocumentDTO>();
		Assert.NotNull(createdDoc);

		// Act 
		var deleteResponse = await _client.DeleteAsync($"/api/document/{createdDoc.Id}");

		// Assert 
		Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

		var getResponse = await _client.GetAsync($"/api/document/{createdDoc.Id}");
		Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
	}

	[Fact]
	public async Task EndToEnd_GetAllDocuments_ReturnsAllCreatedDocuments()
	{
		// Arrange 
		var docs = new[]
		{
				new DocumentCreateDTO { FileName = "doc1.pdf", FileSize = 100, ContentType = "application/pdf", Tags = new List<string> { "tag1" } },
				new DocumentCreateDTO { FileName = "doc2.pdf", FileSize = 200, ContentType = "application/pdf", Tags = new List<string> { "tag2" } },
				new DocumentCreateDTO { FileName = "doc3.pdf", FileSize = 300, ContentType = "application/pdf", Tags = new List<string> { "tag3" } }
		  };

		foreach (var doc in docs)
		{
			await _client.PostAsJsonAsync("/api/document", doc);
		}

		// Act
		var response = await _client.GetAsync("/api/document");

		// Assert
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var allDocs = await response.Content.ReadFromJsonAsync<List<DocumentDTO>>();
		Assert.NotNull(allDocs);
		Assert.True(allDocs.Count >= 3);
	}

	[Fact]
	public async Task GetById_NonExistingId_ReturnsNotFound()
	{
		// Arrange
		var nonExistingId = Guid.NewGuid();

		// Act
		var response = await _client.GetAsync($"/api/document/{nonExistingId}");

		// Assert
		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task Create_InvalidData_ReturnsBadRequest()
	{
		// Arrange 
		var invalidDto = new { };

		var content = new StringContent(
			 System.Text.Json.JsonSerializer.Serialize(invalidDto),
			 Encoding.UTF8,
			 "application/json"
		);

		// Act
		var response = await _client.PostAsync("/api/document", content);

		// Assert
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task Update_NonExistingDocument_ReturnsNotFound()
	{
		// Arrange
		var nonExistingId = Guid.NewGuid();
		var updateDto = new DocumentCreateDTO
		{
			FileName = "ghost.pdf",
			FileSize = 1024,
			ContentType = "application/pdf",
			Tags = new List<string>()
		};

		// Act
		var response = await _client.PutAsJsonAsync($"/api/document/{nonExistingId}", updateDto);

		// Assert
		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task Delete_NonExistingDocument_ReturnsNotFound()
	{
		// Arrange
		var nonExistingId = Guid.NewGuid();

		// Act
		var response = await _client.DeleteAsync($"/api/document/{nonExistingId}");

		// Assert
		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}
}
