using DocumentManagementSystem.BatchWorker.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Quartz;

namespace DocumentManagementSystem.BatchWorker.Jobs
{
	public class AccessLogProcessingJob : IJob
	{
		private readonly ILogger<AccessLogProcessingJob> _logger;
		private readonly IFileProcessorService _fileProcessor;

		public AccessLogProcessingJob(
			ILogger<AccessLogProcessingJob> logger,
			IFileProcessorService fileProcessor)
		{
			_logger = logger;
			_fileProcessor = fileProcessor;
		}

		public async Task Execute(IJobExecutionContext context)
		{
			_logger.LogInformation("Access log processing job triggered at {Time}", DateTime.UtcNow);

			try
			{
				await _fileProcessor.ProcessFilesAsync(context.CancellationToken);
				_logger.LogInformation("Access log processing job completed successfully");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Access log processing job failed: {Message}", ex.Message);
				throw;
			}
		}
	}
}
