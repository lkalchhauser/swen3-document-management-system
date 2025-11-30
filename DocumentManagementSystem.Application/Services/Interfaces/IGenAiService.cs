namespace DocumentManagementSystem.Application.Services.Interfaces;

public interface IGenAiService
{
	Task<string> GenerateSummaryAsync(string text, int maxLength = 200, CancellationToken ct = default);
}
