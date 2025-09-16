using AutoFixture;
using DocumentManagementSystem.DAL.Repositories;
using DocumentManagementSystem.Model.ORM;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagementSystem.DAL.Tests.Repositories
{
	public sealed class DocumentRepositoryTests
	{
		private static DocumentManagementSystemContext CreateInMemoryContext()
		{
			var options = new DbContextOptionsBuilder<DocumentManagementSystemContext>()
				 .UseInMemoryDatabase(Guid.NewGuid().ToString())
				 .Options;

			return new DocumentManagementSystemContext(options);
		}

		[Fact]
		public async Task AddAsync_AssignsId()
		{
			// Arrange
			var fixture = new Fixture();
			await using var context = CreateInMemoryContext();
			var repo = new DocumentRepository(context);

			var doc = fixture.Build<Document>()
				 .With(x => x.FileName, "test-add.pdf")
				 .Without(x => x.Metadata)
				 .Without(x => x.Tags)
				 .Create();

			// Act
			await repo.AddAsync(doc);
			await repo.SaveChangesAsync();

			// Assert
			Assert.NotEqual(Guid.Empty, doc.Id);
		}

		[Fact]
		public async Task GetByIdAsync_ExistingDocument_ReturnsEntity()
		{
			// Arrange
			var fixture = new Fixture();
			await using var context = CreateInMemoryContext();
			var seeded = fixture.Build<Document>()
				 .With(x => x.FileName, "test-get.pdf")
				 .Without(x => x.Metadata)
				 .Without(x => x.Tags)
				 .Create();

			await context.Documents.AddAsync(seeded);
			await context.SaveChangesAsync();

			var repo = new DocumentRepository(context);

			// Act
			var loaded = await repo.GetByIdAsync(seeded.Id);

			// Assert
			Assert.NotNull(loaded);
			Assert.Equal("test-get.pdf", loaded!.FileName);
		}

		[Fact]
		public async Task GetByIdAsync_MissingDocument_ReturnsNull()
		{
			// Arrange
			await using var context = CreateInMemoryContext();
			var repo = new DocumentRepository(context);

			// Act
			var loaded = await repo.GetByIdAsync(Guid.NewGuid());

			// Assert
			Assert.Null(loaded);
		}

		[Fact]
		public async Task UpdateAsync_ChangesArePersisted()
		{
			// Arrange
			var fixture = new Fixture();
			await using var context = CreateInMemoryContext();
			var repo = new DocumentRepository(context);

			var doc = fixture.Build<Document>()
				 .With(x => x.FileName, "original.pdf")
				 .Without(x => x.Metadata)
				 .Without(x => x.Tags)
				 .Create();

			await context.Documents.AddAsync(doc);
			await context.SaveChangesAsync();

			// Act
			doc.FileName = "updated.pdf";
			await repo.UpdateAsync(doc);
			await repo.SaveChangesAsync();

			// Assert
			var reloaded = await context.Documents.AsNoTracking().FirstAsync(x => x.Id == doc.Id);
			Assert.Equal("updated.pdf", reloaded.FileName);
		}

		[Fact]
		public async Task DeleteAsync_RemovesDocument()
		{
			// Arrange
			var fixture = new Fixture();
			await using var context = CreateInMemoryContext();
			var repo = new DocumentRepository(context);

			var doc = fixture.Build<Document>()
				 .With(x => x.FileName, "delete-me.pdf")
				 .Without(x => x.Metadata)
				 .Without(x => x.Tags)
				 .Create();

			await context.Documents.AddAsync(doc);
			await context.SaveChangesAsync();

			// Act
			await repo.DeleteAsync(doc);
			await repo.SaveChangesAsync();

			// Assert
			var exists = await context.Documents.AnyAsync(x => x.Id == doc.Id);
			Assert.False(exists);
		}
	}
}