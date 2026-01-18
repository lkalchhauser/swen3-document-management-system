using DocumentManagementSystem.Model.Abstract;

namespace DocumentManagementSystem.Model.ORM
{
	public class BatchProcessingError : BaseEntity
	{
		public Guid DocumentId { get; set; }
		public DateOnly BatchDate { get; set; }
		public int AccessCount { get; set; }
		public string ErrorMessage { get; set; } = null!;
		public string? FileName { get; set; }
	}
}
