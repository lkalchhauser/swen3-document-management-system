namespace DocumentManagementSystem.BatchWorker.Services
{
	public class AccessLogPersistenceResult
	{
		public int ProcessedCount { get; set; }
		public int ErrorCount { get; set; }
		public bool HasErrors => ErrorCount > 0;
	}
}
