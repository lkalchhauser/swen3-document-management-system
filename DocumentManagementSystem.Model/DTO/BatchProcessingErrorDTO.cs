namespace DocumentManagementSystem.Model.DTO
{
	public class BatchProcessingErrorDTO
	{
		public Guid Id { get; set; }
		public Guid DocumentId { get; set; }
		public DateOnly BatchDate { get; set; }
		public int AccessCount { get; set; }
		public string ErrorMessage { get; set; } = null!;
		public string? FileName { get; set; }
		public DateTimeOffset CreatedAt { get; set; }
	}
}
