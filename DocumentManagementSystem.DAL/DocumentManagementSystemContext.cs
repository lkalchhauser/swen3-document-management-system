using DocumentManagementSystem.Model.ORM;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagementSystem.DAL
{
	public class DocumentManagementSystemContext(DbContextOptions<DocumentManagementSystemContext> options) : DbContext(options)
	{
		public DbSet<Document> Documents => Set<Document>();
		public DbSet<DocumentMetadata> Metadata => Set<DocumentMetadata>();
		public DbSet<Tag> Tags => Set<Tag>();
	}
}
