namespace DocumentManagementSystem.BatchWorker.Services.Interfaces
{
	public interface IAccessLogPersistenceService
	{
		Task SaveAccessLogsAsync(AccessLogBatch batch, CancellationToken cancellationToken = default);
	}
}
