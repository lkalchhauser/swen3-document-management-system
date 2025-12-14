using System.ComponentModel.DataAnnotations;

namespace DocumentManagementSystem.Application.Configuration;

public class GeminiOptions
{
	public const string SectionName = "Gemini";

	[Required(ErrorMessage = "Gemini API key is required")]
	public string ApiKey { get; set; } = null!;

	public string Model { get; set; } = "gemini-2.5-flash";

	public string ApiEndpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models";

	[Range(1, 10, ErrorMessage = "MaxRetries must be between 1 and 10")]
	public int MaxRetries { get; set; } = 3;

	[Range(5, 120, ErrorMessage = "TimeoutSeconds must be between 5 and 120")]
	public int TimeoutSeconds { get; set; } = 30;

	public int MaxPromptLength { get; set; } = 10000;
}
