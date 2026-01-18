using DocumentManagementSystem.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManagementSystem.REST.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class BatchProcessingController : ControllerBase
	{
		private readonly ILogger<BatchProcessingController> _logger;
		private readonly IBatchMonitoringService _batchMonitoringService;

		public BatchProcessingController(
			ILogger<BatchProcessingController> logger,
			IBatchMonitoringService batchMonitoringService)
		{
			_logger = logger;
			_batchMonitoringService = batchMonitoringService;
		}

		[HttpGet("status")]
		public async Task<IActionResult> GetBatchStatus(CancellationToken cancellationToken)
		{
			try
			{
				var status = await _batchMonitoringService.GetBatchStatusAsync(cancellationToken);
				return Ok(status);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting batch status");
				return StatusCode(500, new { error = "Failed to retrieve batch status" });
			}
		}

		[HttpGet("statistics")]
		public async Task<IActionResult> GetAccessStatistics([FromQuery] int top = 10, CancellationToken cancellationToken = default)
		{
			try
			{
				var statistics = await _batchMonitoringService.GetAccessStatisticsAsync(top, cancellationToken);
				return Ok(statistics);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting access statistics");
				return StatusCode(500, new { error = "Failed to retrieve access statistics" });
			}
		}
	}
}
