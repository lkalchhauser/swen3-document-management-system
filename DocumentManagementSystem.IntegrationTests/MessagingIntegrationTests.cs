using System.Net.Http.Json;
using DocumentManagementSystem.Model.DTO;
using Moq;

namespace DocumentManagementSystem.IntegrationTests;

public class MessagingIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public MessagingIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateDocument_PublishesMessageToQueue()
    {
        // Arrange
        var createDto = new DocumentCreateDTO
        {
            FileName = "message-test.pdf",
            FileSize = 1024,
            ContentType = "application/pdf",
            Tags = new List<string> { "messaging" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/document", createDto);

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        _factory.MockMessagePublisher.Verify(
            m => m.PublishAsync(
                It.Is<DocumentUploadMessageDTO>(msg =>
                    msg.FileName == "message-test.pdf" &&
                    msg.DocumentId != Guid.Empty
                ),
                It.IsAny<CancellationToken>()
            ),
            Times.Once,
            "Message should be published to queue when document is created"
        );
    }

    [Fact]
    public async Task CreateDocument_MessageContainsCorrectData()
    {
        // Arrange
        var createDto = new DocumentCreateDTO
        {
            FileName = "data-verification.pdf",
            FileSize = 2048,
            ContentType = "application/pdf",
            Tags = new List<string> { "test", "verification" }
        };

        DocumentUploadMessageDTO? publishedMessage = null;

        _factory.MockMessagePublisher
            .Setup(m => m.PublishAsync(It.IsAny<DocumentUploadMessageDTO>(), It.IsAny<CancellationToken>()))
            .Callback<DocumentUploadMessageDTO, CancellationToken>((msg, ct) => publishedMessage = msg)
            .Returns(Task.CompletedTask);

        // Act
        var response = await _client.PostAsJsonAsync("/api/document", createDto);
        var createdDoc = await response.Content.ReadFromJsonAsync<DocumentDTO>();

        // Assert
        Assert.NotNull(publishedMessage);
        Assert.Equal("data-verification.pdf", publishedMessage.FileName);
        Assert.Equal(createdDoc!.Id, publishedMessage.DocumentId);
        Assert.NotEqual(default, publishedMessage.UploadedAtUtc);
    }

    [Fact]
    public async Task MultipleDocumentCreations_PublishMultipleMessages()
    {
        // Arrange
        var documents = new[]
        {
            new DocumentCreateDTO { FileName = "doc1.pdf", FileSize = 100, ContentType = "application/pdf", Tags = new List<string>() },
            new DocumentCreateDTO { FileName = "doc2.pdf", FileSize = 200, ContentType = "application/pdf", Tags = new List<string>() },
            new DocumentCreateDTO { FileName = "doc3.pdf", FileSize = 300, ContentType = "application/pdf", Tags = new List<string>() }
        };

        // Act
        foreach (var doc in documents)
        {
            await _client.PostAsJsonAsync("/api/document", doc);
        }

        // Assert
        _factory.MockMessagePublisher.Verify(
            m => m.PublishAsync(It.IsAny<DocumentUploadMessageDTO>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3),
            "Should publish one message per document creation"
        );
    }

    [Fact]
    public async Task FileUpload_AlsoPublishesMessage()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0x01, 0x02, 0x03, 0x04 });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "upload-queue-test.pdf");
        content.Add(new StringContent("queue,test"), "tags");

        // Act
        var response = await _client.PostAsync("/api/document/upload", content);

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        _factory.MockMessagePublisher.Verify(
            m => m.PublishAsync(
                It.Is<DocumentUploadMessageDTO>(msg => msg.FileName == "upload-queue-test.pdf"),
                It.IsAny<CancellationToken>()
            ),
            Times.Once,
            "File upload should also publish message to queue"
        );
    }

    [Fact]
    public async Task UpdateDocument_DoesNotPublishMessage()
    {
        // Arrange 
        var createDto = new DocumentCreateDTO
        {
            FileName = "original.pdf",
            FileSize = 1024,
            ContentType = "application/pdf",
            Tags = new List<string>()
        };

        var createResponse = await _client.PostAsJsonAsync("/api/document", createDto);
        var createdDoc = await createResponse.Content.ReadFromJsonAsync<DocumentDTO>();

        _factory.MockMessagePublisher.Reset();

        var updateDto = new DocumentCreateDTO
        {
            FileName = "updated.pdf",
            FileSize = 2048,
            ContentType = "application/pdf",
            Tags = new List<string>()
        };

        // Act 
        await _client.PutAsJsonAsync($"/api/document/{createdDoc!.Id}", updateDto);

        // Assert 
        _factory.MockMessagePublisher.Verify(
            m => m.PublishAsync(It.IsAny<DocumentUploadMessageDTO>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Update operation should not publish messages"
        );
    }

    [Fact]
    public async Task DeleteDocument_DoesNotPublishMessage()
    {
        // Arrange 
        var createDto = new DocumentCreateDTO
        {
            FileName = "to-delete.pdf",
            FileSize = 512,
            ContentType = "application/pdf",
            Tags = new List<string>()
        };

        var createResponse = await _client.PostAsJsonAsync("/api/document", createDto);
        var createdDoc = await createResponse.Content.ReadFromJsonAsync<DocumentDTO>();

        _factory.MockMessagePublisher.Reset();

        // Act 
        await _client.DeleteAsync($"/api/document/{createdDoc!.Id}");

        // Assert 
        _factory.MockMessagePublisher.Verify(
            m => m.PublishAsync(It.IsAny<DocumentUploadMessageDTO>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Delete operation should not publish messages"
        );
    }
}
