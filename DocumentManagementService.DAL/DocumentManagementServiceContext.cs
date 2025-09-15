using DocumentManagementSystem.Model.ORM;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagementService.DAL
{
	public class DocumentManagementServiceContext(DbContextOptions<DocumentManagementServiceContext> options) : DbContext(options)
	{
		public DbSet<Document> Documents { get; set; }
		public DbSet<DocumentMetadata> Metadata { get; set; }
		public DbSet<Tag> Tags { get; set; }
	}
}
