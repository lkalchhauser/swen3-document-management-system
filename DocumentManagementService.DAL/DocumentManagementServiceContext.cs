using DocumentManagementSystem.Model.ORM;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagementService.DAL
{
	public class DocumentManagementServiceContext(DbContextOptions<DocumentManagementServiceContext> options) : DbContext(options)
	{
		public DbSet<Document> Documents => Set<Document>();
		public DbSet<DocumentMetadata> Metadata => Set<DocumentMetadata>();
		public DbSet<Tag> Tags => Set<Tag>();
	}
}
