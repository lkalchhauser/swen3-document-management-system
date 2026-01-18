using DocumentManagementSystem.Model.ORM;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagementSystem.DAL
{
	public class DocumentManagementSystemContext(DbContextOptions<DocumentManagementSystemContext> options) : DbContext(options)
	{
		public DbSet<Document> Documents => Set<Document>();
		public DbSet<DocumentMetadata> Metadata => Set<DocumentMetadata>();
		public DbSet<Tag> Tags => Set<Tag>();
		public DbSet<Note> Notes => Set<Note>();
		public DbSet<DocumentAccessLog> DocumentAccessLogs => Set<DocumentAccessLog>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<DocumentAccessLog>()
				.HasIndex(dal => new { dal.DocumentId, dal.AccessDate })
				.IsUnique();

			modelBuilder.Entity<DocumentAccessLog>()
				.HasOne(dal => dal.Document)
				.WithMany()
				.HasForeignKey(dal => dal.DocumentId)
				.OnDelete(DeleteBehavior.Cascade);
		}
	}
}
