namespace DocumentManagementSystem.BatchWorker.Services.Interfaces
{
	public interface IAccessLogPersistenceService
	{
		Task<AccessLogPersistenceResult> SaveAccessLogsAsync(AccessLogBatch batch, string? fileName = null, CancellationToken cancellationToken = default);
	}
}
