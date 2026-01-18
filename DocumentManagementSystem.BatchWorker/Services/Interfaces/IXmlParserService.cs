namespace DocumentManagementSystem.BatchWorker.Services.Interfaces
{
	public interface IXmlParserService
	{
		Task<AccessLogBatch> ParseAsync(string filePath, CancellationToken cancellationToken = default);
	}

	public record AccessLogBatch(DateOnly BatchDate, IReadOnlyList<AccessEntry> Entries);

	public record AccessEntry(Guid DocumentId, int AccessCount);
}
