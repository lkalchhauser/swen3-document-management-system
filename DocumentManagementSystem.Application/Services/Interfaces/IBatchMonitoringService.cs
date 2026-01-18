using DocumentManagementSystem.Model.DTO;

namespace DocumentManagementSystem.Application.Services.Interfaces
{
	public interface IBatchMonitoringService
	{
		Task<BatchProcessingStatusDTO> GetBatchStatusAsync(CancellationToken cancellationToken = default);
		Task<List<DocumentAccessStatisticsDTO>> GetAccessStatisticsAsync(int topN = 10, CancellationToken cancellationToken = default);
	}
}
