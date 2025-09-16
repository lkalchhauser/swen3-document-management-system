using AutoFixture;
using AutoMapper;
using DocumentManagementSystem.DAL.Mapper;
using DocumentManagementSystem.DAL.Repositories;
using DocumentManagementSystem.DAL.Services;
using DocumentManagementSystem.Model.DTO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocumentManagementSystem.DAL.Tests.Services;

public sealed class DocumentServiceTests
{
	private readonly IMapper _mapper;

	public DocumentServiceTests()
	{
		var loggerFactory = LoggerFactory.Create(_ => { });
		var config = new MapperConfiguration(
			cfg =>
			{
				cfg.AddProfile<MappingProfile>();
			},
			loggerFactory
		);
		_mapper = config.CreateMapper();
	}


	private static DocumentManagementSystemContext CreateInMemoryContext()
	{
		var options = new DbContextOptionsBuilder<DocumentManagementSystemContext>()
			 .UseInMemoryDatabase(Guid.NewGuid().ToString())
			 .Options;

		return new DocumentManagementSystemContext(options);
	}

	[Fact]
	public async Task CreateAsync_ValidDto_CreatesDocument()
	{
		// Arrange
		var fixture = new Fixture();
		await using var context = CreateInMemoryContext();
		var repo = new DocumentRepository(context);
		var service = new DocumentService(repo, _mapper);

		var dto = fixture.Build<DocumentCreateDTO>()
			 .With(x => x.FileName, "create-test.pdf")
			 .With(x => x.ContentType, "application/pdf")
			 .With(x => x.Tags, new List<string> { "tag1" })
			 .Create();

		// Act
		var result = await service.CreateAsync(dto);

		// Assert
		Assert.Equal("create-test.pdf", result.FileName);
		Assert.NotNull(result.Metadata);
		Assert.Contains("tag1", result.Tags);
	}

	[Fact]
	public async Task GetByIdAsync_ExistingDocument_ReturnsDto()
	{
		// Arrange
		var fixture = new Fixture();
		await using var context = CreateInMemoryContext();
		var repo = new DocumentRepository(context);
		var service = new DocumentService(repo, _mapper);

		var dto = fixture.Build<DocumentCreateDTO>()
			 .With(x => x.FileName, "get-test.pdf")
			 .With(x => x.ContentType, "application/pdf")
			 .With(x => x.Tags, new List<string> { "tagX" })
			 .Create();

		var created = await service.CreateAsync(dto);

		// Act
		var result = await service.GetByIdAsync(created.Id);

		// Assert
		Assert.NotNull(result);
		Assert.Equal("get-test.pdf", result!.FileName);
	}

	[Fact]
	public async Task GetByIdAsync_MissingDocument_ReturnsNull()
	{
		// Arrange
		await using var context = CreateInMemoryContext();
		var repo = new DocumentRepository(context);
		var service = new DocumentService(repo, _mapper);

		// Act
		var result = await service.GetByIdAsync(Guid.NewGuid());

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task UpdateAsync_ExistingDocument_UpdatesFields()
	{
		// Arrange
		var fixture = new Fixture();
		await using var context = CreateInMemoryContext();
		var repo = new DocumentRepository(context);
		var service = new DocumentService(repo, _mapper);

		var dto = fixture.Build<DocumentCreateDTO>()
			 .With(x => x.FileName, "before-update.pdf")
			 .With(x => x.ContentType, "application/pdf")
			 .With(x => x.Tags, new List<string> { "oldTag" })
			 .Create();

		var created = await service.CreateAsync(dto);

		var updateDto = fixture.Build<DocumentCreateDTO>()
			 .With(x => x.FileName, "after-update.pdf")
			 .With(x => x.ContentType, "application/pdf")
			 .With(x => x.Tags, new List<string> { "newTag" })
			 .Create();

		// Act
		var updated = await service.UpdateAsync(created.Id, updateDto);

		// Assert
		Assert.NotNull(updated);
		Assert.Equal("after-update.pdf", updated!.FileName);
		Assert.Contains("newTag", updated.Tags);
	}

	[Fact]
	public async Task UpdateAsync_MissingDocument_ReturnsNull()
	{
		// Arrange
		var fixture = new Fixture();
		await using var context = CreateInMemoryContext();
		var repo = new DocumentRepository(context);
		var service = new DocumentService(repo, _mapper);

		var updateDto = fixture.Build<DocumentCreateDTO>()
			 .With(x => x.FileName, "missing.pdf")
			 .Create();

		// Act
		var updated = await service.UpdateAsync(Guid.NewGuid(), updateDto);

		// Assert
		Assert.Null(updated);
	}

	[Fact]
	public async Task DeleteAsync_ExistingDocument_ReturnsTrue()
	{
		// Arrange
		var fixture = new Fixture();
		await using var context = CreateInMemoryContext();
		var repo = new DocumentRepository(context);
		var service = new DocumentService(repo, _mapper);

		var dto = fixture.Build<DocumentCreateDTO>()
			 .With(x => x.FileName, "delete-test.pdf")
			 .With(x => x.ContentType, "application/pdf")
			 .Create();

		var created = await service.CreateAsync(dto);

		// Act
		var result = await service.DeleteAsync(created.Id);

		// Assert
		Assert.True(result);
	}

	[Fact]
	public async Task DeleteAsync_MissingDocument_ReturnsFalse()
	{
		// Arrange
		await using var context = CreateInMemoryContext();
		var repo = new DocumentRepository(context);
		var service = new DocumentService(repo, _mapper);

		// Act
		var result = await service.DeleteAsync(Guid.NewGuid());

		// Assert
		Assert.False(result);
	}
}
