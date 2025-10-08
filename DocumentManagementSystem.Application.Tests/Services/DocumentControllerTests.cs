using AutoFixture;
using DocumentManagementSystem.Application.Services.Interfaces;
using DocumentManagementSystem.Model.DTO;
using DocumentManagementSystem.REST.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocumentManagementSystem.Application.Tests.Controllers;

public sealed class DocumentControllerTests
{
	private readonly Mock<IDocumentService> _mockService;
	private readonly Mock<ILogger<DocumentController>> _mockLogger;
	private readonly DocumentController _controller;
	private readonly Fixture _fixture;

	public DocumentControllerTests()
	{
		_mockService = new Mock<IDocumentService>();
		_mockLogger = new Mock<ILogger<DocumentController>>();
		_controller = new DocumentController(_mockService.Object, _mockLogger.Object);
		_fixture = new Fixture();
	}

	[Fact]
	public async Task GetAll_ReturnsOkWithDocuments()
	{
		// Arrange
		var documents = _fixture.CreateMany<DocumentDTO>(3).ToList();
		_mockService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(documents);

		// Act
		var result = await _controller.GetAll(CancellationToken.None);

		// Assert
		var okResult = Assert.IsType<OkObjectResult>(result.Result);
		var returnedDocs = Assert.IsAssignableFrom<IReadOnlyList<DocumentDTO>>(okResult.Value);
		Assert.Equal(3, returnedDocs.Count);
	}

	[Fact]
	public async Task GetAll_ReturnsEmptyList_WhenNoDocuments()
	{
		// Arrange
		_mockService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new List<DocumentDTO>());

		// Act
		var result = await _controller.GetAll(CancellationToken.None);

		// Assert
		var okResult = Assert.IsType<OkObjectResult>(result.Result);
		var returnedDocs = Assert.IsAssignableFrom<IReadOnlyList<DocumentDTO>>(okResult.Value);
		Assert.Empty(returnedDocs);
	}

	[Fact]
	public async Task GetById_ExistingId_ReturnsOkWithDocument()
	{
		// Arrange
		var id = Guid.NewGuid();
		var document = _fixture.Build<DocumentDTO>()
			.With(d => d.Id, id)
			.Create();
		_mockService.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(document);

		// Act
		var result = await _controller.GetById(id, CancellationToken.None);

		// Assert
		var okResult = Assert.IsType<OkObjectResult>(result.Result);
		var returnedDoc = Assert.IsType<DocumentDTO>(okResult.Value);
		Assert.Equal(id, returnedDoc.Id);
	}

	[Fact]
	public async Task GetById_NonExistingId_ReturnsNotFound()
	{
		// Arrange
		var id = Guid.NewGuid();
		_mockService.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
			.ReturnsAsync((DocumentDTO?)null);

		// Act
		var result = await _controller.GetById(id, CancellationToken.None);

		// Assert
		Assert.IsType<NotFoundResult>(result.Result);
	}

	[Fact]
	public async Task Create_ValidDto_ReturnsCreatedAtAction()
	{
		// Arrange
		var createDto = _fixture.Build<DocumentCreateDTO>()
			.With(d => d.FileName, "test.pdf")
			.Create();
		var createdDoc = _fixture.Build<DocumentDTO>()
			.With(d => d.FileName, "test.pdf")
			.Create();

		_mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
			.ReturnsAsync(createdDoc);

		// Act
		var result = await _controller.Create(createDto, CancellationToken.None);

		// Assert
		var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
		Assert.Equal(nameof(DocumentController.GetById), createdResult.ActionName);
		var returnedDoc = Assert.IsType<DocumentDTO>(createdResult.Value);
		Assert.Equal("test.pdf", returnedDoc.FileName);
	}

	[Fact]
	public async Task Create_InvalidModelState_ReturnsBadRequest()
	{
		// Arrange
		var createDto = _fixture.Create<DocumentCreateDTO>();
		_controller.ModelState.AddModelError("FileName", "Required");

		// Act
		var result = await _controller.Create(createDto, CancellationToken.None);

		// Assert
		Assert.IsType<BadRequestObjectResult>(result.Result);
	}

	[Fact]
	public async Task Update_ExistingDocument_ReturnsOkWithUpdatedDocument()
	{
		// Arrange
		var id = Guid.NewGuid();
		var updateDto = _fixture.Build<DocumentCreateDTO>()
			.With(d => d.FileName, "updated.pdf")
			.Create();
		var updatedDoc = _fixture.Build<DocumentDTO>()
			.With(d => d.Id, id)
			.With(d => d.FileName, "updated.pdf")
			.Create();

		_mockService.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
			.ReturnsAsync(updatedDoc);

		// Act
		var result = await _controller.Update(id, updateDto, CancellationToken.None);

		// Assert
		var okResult = Assert.IsType<OkObjectResult>(result.Result);
		var returnedDoc = Assert.IsType<DocumentDTO>(okResult.Value);
		Assert.Equal("updated.pdf", returnedDoc.FileName);
	}

	[Fact]
	public async Task Update_NonExistingDocument_ReturnsNotFound()
	{
		// Arrange
		var id = Guid.NewGuid();
		var updateDto = _fixture.Create<DocumentCreateDTO>();
		_mockService.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
			.ReturnsAsync((DocumentDTO?)null);

		// Act
		var result = await _controller.Update(id, updateDto, CancellationToken.None);

		// Assert
		Assert.IsType<NotFoundResult>(result.Result);
	}

	[Fact]
	public async Task Update_InvalidModelState_ReturnsBadRequest()
	{
		// Arrange
		var id = Guid.NewGuid();
		var updateDto = _fixture.Create<DocumentCreateDTO>();
		_controller.ModelState.AddModelError("FileName", "Required");

		// Act
		var result = await _controller.Update(id, updateDto, CancellationToken.None);

		// Assert
		Assert.IsType<BadRequestObjectResult>(result.Result);
	}

	[Fact]
	public async Task Delete_ExistingDocument_ReturnsNoContent()
	{
		// Arrange
		var id = Guid.NewGuid();
		_mockService.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		// Act
		var result = await _controller.Delete(id, CancellationToken.None);

		// Assert
		Assert.IsType<NoContentResult>(result);
	}

	[Fact]
	public async Task Delete_NonExistingDocument_ReturnsNotFound()
	{
		// Arrange
		var id = Guid.NewGuid();
		_mockService.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		// Act
		var result = await _controller.Delete(id, CancellationToken.None);

		// Assert
		Assert.IsType<NotFoundResult>(result);
	}

	[Fact]
	public async Task UploadFile_ValidFile_ReturnsCreatedAtAction()
	{
		// Arrange
		var fileMock = new Mock<IFormFile>();
		var content = "fake file content";
		var fileName = "test.pdf";
		var ms = new MemoryStream();
		var writer = new StreamWriter(ms);
		writer.Write(content);
		writer.Flush();
		ms.Position = 0;

		fileMock.Setup(f => f.FileName).Returns(fileName);
		fileMock.Setup(f => f.Length).Returns(ms.Length);
		fileMock.Setup(f => f.ContentType).Returns("application/pdf");

		var createdDoc = _fixture.Build<DocumentDTO>()
			.With(d => d.FileName, fileName)
			.Create();

		_mockService.Setup(s => s.CreateAsync(It.IsAny<DocumentCreateDTO>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(createdDoc);

		// Act
		var result = await _controller.UploadFile(fileMock.Object, "tag1,tag2", CancellationToken.None);

		// Assert
		var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
		var returnedDoc = Assert.IsType<DocumentDTO>(createdResult.Value);
		Assert.Equal(fileName, returnedDoc.FileName);
	}

	[Fact]
	public async Task UploadFile_NullFile_ReturnsBadRequest()
	{
		// Act
		var result = await _controller.UploadFile(null!, null, CancellationToken.None);

		// Assert
		var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Equal("No file provided", badRequestResult.Value);
	}

	[Fact]
	public async Task UploadFile_EmptyFile_ReturnsBadRequest()
	{
		// Arrange
		var fileMock = new Mock<IFormFile>();
		fileMock.Setup(f => f.Length).Returns(0);

		// Act
		var result = await _controller.UploadFile(fileMock.Object, null, CancellationToken.None);

		// Assert
		var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Equal("No file provided", badRequestResult.Value);
	}

	[Fact]
	public async Task UploadFile_WithTags_ParsesTagsCorrectly()
	{
		// Arrange
		var fileMock = new Mock<IFormFile>();
		fileMock.Setup(f => f.FileName).Returns("test.pdf");
		fileMock.Setup(f => f.Length).Returns(100);
		fileMock.Setup(f => f.ContentType).Returns("application/pdf");

		DocumentCreateDTO? capturedDto = null;
		_mockService.Setup(s => s.CreateAsync(It.IsAny<DocumentCreateDTO>(), It.IsAny<CancellationToken>()))
			.Callback<DocumentCreateDTO, CancellationToken>((dto, ct) => capturedDto = dto)
			.ReturnsAsync(_fixture.Create<DocumentDTO>());

		// Act
		await _controller.UploadFile(fileMock.Object, "tag1, tag2 , tag3", CancellationToken.None);

		// Assert
		Assert.NotNull(capturedDto);
		Assert.Equal(3, capturedDto.Tags.Count);
		Assert.Contains("tag1", capturedDto.Tags);
		Assert.Contains("tag2", capturedDto.Tags);
		Assert.Contains("tag3", capturedDto.Tags);
	}
}
