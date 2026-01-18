namespace DocumentManagementSystem.Model.DTO
{
	public class BatchProcessingStatusDTO
	{
		public int PendingFilesCount { get; set; }
		public int ArchivedFilesCount { get; set; }
		public int ErrorFilesCount { get; set; }
		public List<BatchFileInfoDTO> PendingFiles { get; set; } = [];
		public List<BatchFileInfoDTO> ArchivedFiles { get; set; } = [];
		public List<BatchFileInfoDTO> ErrorFiles { get; set; } = [];
		public DateTime LastChecked { get; set; }
	}
}
