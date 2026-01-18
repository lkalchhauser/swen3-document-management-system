using DocumentManagementSystem.Model.Abstract;

namespace DocumentManagementSystem.Model.ORM
{
	public class DocumentAccessLog : BaseEntity
	{
		public Guid DocumentId { get; set; }
		public Document Document { get; set; } = null!;
		public DateOnly AccessDate { get; set; }
		public int AccessCount { get; set; }
	}
}
