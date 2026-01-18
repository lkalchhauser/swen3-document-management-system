using DocumentManagementSystem.Application.Services.Interfaces;
using DocumentManagementSystem.DAL;
using DocumentManagementSystem.Model.DTO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DocumentManagementSystem.Application.Services
{
	public class BatchMonitoringService : IBatchMonitoringService
	{
		private readonly ILogger<BatchMonitoringService> _logger;
		private readonly DocumentManagementSystemContext _context;
		private readonly string _inputFolder;
		private readonly string _archiveFolder;
		private readonly string _errorFolder;

		public BatchMonitoringService(
			ILogger<BatchMonitoringService> logger,
			DocumentManagementSystemContext context,
			IConfiguration configuration)
		{
			_logger = logger;
			_context = context;
			_inputFolder = configuration["BatchProcessing:InputFolder"] ?? "/data/batch/input";
			_archiveFolder = configuration["BatchProcessing:ArchiveFolder"] ?? "/data/batch/archive";
			_errorFolder = configuration["BatchProcessing:ErrorFolder"] ?? "/data/batch/error";
		}

		public Task<BatchProcessingStatusDTO> GetBatchStatusAsync(CancellationToken cancellationToken = default)
		{
			var pendingFiles = GetFilesFromDirectory(_inputFolder, "Pending");
			var archivedFiles = GetFilesFromDirectory(_archiveFolder, "Archived");
			var errorFiles = GetFilesFromDirectory(_errorFolder, "Error");

			var status = new BatchProcessingStatusDTO
			{
				PendingFilesCount = pendingFiles.Count,
				ArchivedFilesCount = archivedFiles.Count,
				ErrorFilesCount = errorFiles.Count,
				PendingFiles = pendingFiles.OrderByDescending(f => f.LastModified).Take(10).ToList(),
				ArchivedFiles = archivedFiles.OrderByDescending(f => f.LastModified).Take(10).ToList(),
				ErrorFiles = errorFiles.OrderByDescending(f => f.LastModified).Take(10).ToList(),
				LastChecked = DateTime.UtcNow
			};

			return Task.FromResult(status);
		}

		public async Task<List<DocumentAccessStatisticsDTO>> GetAccessStatisticsAsync(int topN = 10, CancellationToken cancellationToken = default)
		{
			var stats = await _context.DocumentAccessLogs
				.Include(dal => dal.Document)
				.GroupBy(dal => new { dal.DocumentId, dal.Document.FileName })
				.Select(g => new
				{
					g.Key.DocumentId,
					g.Key.FileName,
					TotalAccessCount = g.Sum(dal => dal.AccessCount),
					LastAccessDate = g.Max(dal => dal.AccessDate),
					DailyAccess = g.Select(dal => new DailyAccessDTO
					{
						Date = dal.AccessDate,
						AccessCount = dal.AccessCount
					}).ToList()
				})
				.OrderByDescending(s => s.TotalAccessCount)
				.Take(topN)
				.ToListAsync(cancellationToken);

			return stats.Select(s => new DocumentAccessStatisticsDTO
			{
				DocumentId = s.DocumentId,
				DocumentName = s.FileName,
				TotalAccessCount = s.TotalAccessCount,
				LastAccessDate = s.LastAccessDate.ToDateTime(TimeOnly.MinValue),
				DailyAccess = s.DailyAccess.OrderByDescending(d => d.Date).Take(30).ToList()
			}).ToList();
		}

		private List<BatchFileInfoDTO> GetFilesFromDirectory(string directoryPath, string status)
		{
			try
			{
				if (!Directory.Exists(directoryPath))
				{
					_logger.LogWarning("Directory does not exist: {DirectoryPath}", directoryPath);
					return [];
				}

				var files = Directory.GetFiles(directoryPath, "*.xml")
					.Select(filePath =>
					{
						var fileInfo = new FileInfo(filePath);
						return new BatchFileInfoDTO
						{
							FileName = fileInfo.Name,
							FileSizeBytes = fileInfo.Length,
							LastModified = fileInfo.LastWriteTimeUtc,
							Status = status
						};
					})
					.ToList();

				return files;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error reading files from directory: {DirectoryPath}", directoryPath);
				return [];
			}
		}
	}
}
