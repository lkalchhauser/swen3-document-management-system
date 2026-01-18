namespace DocumentManagementSystem.BatchWorker.Services.Interfaces
{
	public interface IFileProcessorService
	{
		Task ProcessFilesAsync(CancellationToken cancellationToken = default);
	}
}
