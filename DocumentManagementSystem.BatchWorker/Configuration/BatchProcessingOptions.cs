using System.ComponentModel.DataAnnotations;

namespace DocumentManagementSystem.BatchWorker.Configuration
{
	public class BatchProcessingOptions
	{
		public const string SectionName = "BatchProcessing";

		[Required]
		public string InputFolder { get; set; } = "/data/batch/input";

		[Required]
		public string ArchiveFolder { get; set; } = "/data/batch/archive";

		[Required]
		public string ErrorFolder { get; set; } = "/data/batch/error";

		[Required]
		public string FilePattern { get; set; } = "access-*.xml";

		[Required]
		public string CronSchedule { get; set; } = "0 0 1 * * ?";
	}
}
