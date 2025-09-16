using DocumentManagementSystem.Model.Abstract;

namespace DocumentManagementSystem.Model.ORM
{
	public class DocumentMetadata : BaseEntity
	{
		public Guid DocumentId { get; set; }
		public Document? Document { get; set; }

		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }

		public long FileSize { get; set; }
		public string ContentType { get; set; } = string.Empty;
		public string? StoragePath { get; set; }

		public string? OcrText { get; set; }
		public string? Summary { get; set; }
	}
}
