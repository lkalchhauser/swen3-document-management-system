namespace DocumentManagementSystem.Model.DTO
{
	public class DocumentAccessStatisticsDTO
	{
		public Guid DocumentId { get; set; }
		public string DocumentName { get; set; } = null!;
		public int TotalAccessCount { get; set; }
		public DateTime LastAccessDate { get; set; }
		public List<DailyAccessDTO> DailyAccess { get; set; } = [];
	}

	public class DailyAccessDTO
	{
		public DateOnly Date { get; set; }
		public int AccessCount { get; set; }
	}
}
