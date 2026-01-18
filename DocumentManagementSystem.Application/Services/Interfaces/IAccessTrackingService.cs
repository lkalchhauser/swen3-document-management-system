namespace DocumentManagementSystem.Application.Services.Interfaces
{
	public interface IAccessTrackingService
	{
		Task TrackAccessAsync(Guid documentId, CancellationToken cancellationToken = default);
	}
}
