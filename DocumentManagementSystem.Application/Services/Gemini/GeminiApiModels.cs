using System.Text.Json.Serialization;

namespace DocumentManagementSystem.Application.Services.Gemini;

#region Request Models

public class GeminiRequest
{
	[JsonPropertyName("contents")]
	public Content[] Contents { get; set; } = Array.Empty<Content>();
}

public class Content
{
	[JsonPropertyName("parts")]
	public Part[] Parts { get; set; } = Array.Empty<Part>();
}

public class Part
{
	[JsonPropertyName("text")]
	public string Text { get; set; } = string.Empty;
}

#endregion

#region Response Models

public class GeminiResponse
{
	[JsonPropertyName("candidates")]
	public Candidate[] Candidates { get; set; } = Array.Empty<Candidate>();

	[JsonPropertyName("promptFeedback")]
	public PromptFeedback? PromptFeedback { get; set; }
}

public class Candidate
{
	[JsonPropertyName("content")]
	public Content Content { get; set; } = new();

	[JsonPropertyName("finishReason")]
	public string? FinishReason { get; set; }

	[JsonPropertyName("index")]
	public int Index { get; set; }

	[JsonPropertyName("safetyRatings")]
	public SafetyRating[]? SafetyRatings { get; set; }
}

public class SafetyRating
{
	[JsonPropertyName("category")]
	public string Category { get; set; } = string.Empty;

	[JsonPropertyName("probability")]
	public string Probability { get; set; } = string.Empty;
}

public class PromptFeedback
{
	[JsonPropertyName("safetyRatings")]
	public SafetyRating[]? SafetyRatings { get; set; }
}

#endregion
