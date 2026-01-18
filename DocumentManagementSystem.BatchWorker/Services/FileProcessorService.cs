using DocumentManagementSystem.BatchWorker.Configuration;
using DocumentManagementSystem.BatchWorker.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocumentManagementSystem.BatchWorker.Services
{
	public class FileProcessorService : IFileProcessorService
	{
		private readonly ILogger<FileProcessorService> _logger;
		private readonly BatchProcessingOptions _options;
		private readonly IXmlParserService _xmlParser;
		private readonly IAccessLogPersistenceService _persistenceService;

		public FileProcessorService(
			ILogger<FileProcessorService> logger,
			IOptions<BatchProcessingOptions> options,
			IXmlParserService xmlParser,
			IAccessLogPersistenceService persistenceService)
		{
			_logger = logger;
			_options = options.Value;
			_xmlParser = xmlParser;
			_persistenceService = persistenceService;
		}

		public async Task ProcessFilesAsync(CancellationToken cancellationToken = default)
		{
			EnsureDirectoriesExist();

			var files = Directory.GetFiles(_options.InputFolder, _options.FilePattern)
				.OrderBy(f => f)
				.ToList();

			if (files.Count == 0)
			{
				_logger.LogInformation("No files found matching pattern '{Pattern}' in '{InputFolder}'",
					_options.FilePattern, _options.InputFolder);
				return;
			}

			_logger.LogInformation("Processing {Count} file(s)", files.Count);

			foreach (var filePath in files)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					_logger.LogInformation("Cancellation requested, stopping file processing");
					break;
				}

				await ProcessSingleFileAsync(filePath, cancellationToken);
			}
		}

		private async Task ProcessSingleFileAsync(string filePath, CancellationToken cancellationToken)
		{
			var fileName = Path.GetFileName(filePath);
			_logger.LogInformation("Processing file: {FileName}", fileName);

			try
			{
				var batch = await _xmlParser.ParseAsync(filePath, cancellationToken);
				var result = await _persistenceService.SaveAccessLogsAsync(batch, fileName, cancellationToken);

				if (result.HasErrors)
				{
					MoveToError(filePath, fileName);
					_logger.LogWarning("File contains {ErrorCount} invalid document IDs, moved to error folder: {FileName}",
						result.ErrorCount, fileName);
				}
				else
				{
					MoveToArchive(filePath, fileName);
					_logger.LogInformation("Successfully processed and archived file: {FileName}", fileName);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing file {FileName}: {Message}", fileName, ex.Message);
				MoveToError(filePath, fileName);
				_logger.LogInformation("Moved file to error folder: {FileName}", fileName);
			}
		}

		private void EnsureDirectoriesExist()
		{
			Directory.CreateDirectory(_options.InputFolder);
			Directory.CreateDirectory(_options.ArchiveFolder);
			Directory.CreateDirectory(_options.ErrorFolder);
		}

		private void MoveToArchive(string sourcePath, string fileName)
		{
			var destinationPath = Path.Combine(_options.ArchiveFolder, fileName);
			MoveFileSafely(sourcePath, destinationPath);
		}

		private void MoveToError(string sourcePath, string fileName)
		{
			var destinationPath = Path.Combine(_options.ErrorFolder, fileName);
			MoveFileSafely(sourcePath, destinationPath);
		}

		private static void MoveFileSafely(string sourcePath, string destinationPath)
		{
			if (File.Exists(destinationPath))
			{
				File.Delete(destinationPath);
			}

			File.Move(sourcePath, destinationPath);
		}
	}
}
