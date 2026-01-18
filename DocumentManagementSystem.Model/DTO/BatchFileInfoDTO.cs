namespace DocumentManagementSystem.Model.DTO
{
	public class BatchFileInfoDTO
	{
		public string FileName { get; set; } = null!;
		public long FileSizeBytes { get; set; }
		public DateTime LastModified { get; set; }
		public string Status { get; set; } = null!;
	}
}
