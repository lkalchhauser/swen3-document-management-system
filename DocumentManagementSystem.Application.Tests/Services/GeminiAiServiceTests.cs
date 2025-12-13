using DocumentManagementSystem.Application.Configuration;
using DocumentManagementSystem.Application.Services;
using DocumentManagementSystem.Application.Services.Gemini;
using DocumentManagementSystem.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace DocumentManagementSystem.Application.Tests.Services;

public class GeminiAiServiceTests
{
	private readonly Mock<ILogger<GeminiAiService>> _mockLogger;
	private readonly GeminiOptions _options;
	private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
	private readonly HttpClient _httpClient;

	public GeminiAiServiceTests()
	{
		_mockLogger = new Mock<ILogger<GeminiAiService>>();
		_options = new GeminiOptions
		{
			ApiKey = "test-api-key",
			Model = "gemini-1.5-flash",
			MaxRetries = 3,
			TimeoutSeconds = 30,
			MaxPromptLength = 10000
		};

		_mockHttpMessageHandler = new Mock<HttpMessageHandler>();
		_httpClient = new HttpClient(_mockHttpMessageHandler.Object);
	}

	[Fact]
	public async Task GenerateSummaryAsync_WithValidResponse_ShouldReturnSummary()
	{
		// Arrange
		var inputText = "This is a test document with some content.";
		var expectedSummary = "Test document summary";

		var responseJson = JsonSerializer.Serialize(new
		{
			candidates = new[]
			{
				new
				{
					content = new
					{
						parts = new[]
						{
							new { text = expectedSummary }
						}
					}
				}
			}
		});

		_mockHttpMessageHandler
			.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(() => new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
				Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
			});

		var service = new GeminiAiService(
			_httpClient,
			Options.Create(_options),
			_mockLogger.Object);

		// Act
		var result = await service.GenerateSummaryAsync(inputText);

		// Assert
		Assert.Equal(expectedSummary, result);
		_mockHttpMessageHandler.Protected().Verify(
			"SendAsync",
			Times.Once(),
			ItExpr.IsAny<HttpRequestMessage>(),
			ItExpr.IsAny<CancellationToken>());
	}

	[Fact]
	public async Task GenerateSummaryAsync_WithEmptyInput_ShouldReturnEmptyString()
	{
		// Arrange
		var service = new GeminiAiService(
			_httpClient,
			Options.Create(_options),
			_mockLogger.Object);

		// Act
		var result = await service.GenerateSummaryAsync("");

		// Assert
		Assert.Equal(string.Empty, result);
		_mockHttpMessageHandler.Protected().Verify(
			"SendAsync",
			Times.Never(),
			ItExpr.IsAny<HttpRequestMessage>(),
			ItExpr.IsAny<CancellationToken>());
	}

	[Fact]
	public async Task GenerateSummaryAsync_WithNullInput_ShouldReturnEmptyString()
	{
		// Arrange
		var service = new GeminiAiService(
			_httpClient,
			Options.Create(_options),
			_mockLogger.Object);

		// Act
		var result = await service.GenerateSummaryAsync(null!);

		// Assert
		Assert.Equal(string.Empty, result);
	}

	[Fact]
	public async Task GenerateSummaryAsync_WithWhitespaceInput_ShouldReturnEmptyString()
	{
		// Arrange
		var service = new GeminiAiService(
			_httpClient,
			Options.Create(_options),
			_mockLogger.Object);

		// Act
		var result = await service.GenerateSummaryAsync("   ");

		// Assert
		Assert.Equal(string.Empty, result);
	}

	[Fact]
	public async Task GenerateSummaryAsync_WithLongText_ShouldTruncateToMaxPromptLength()
	{
		// Arrange
		var longText = new string('A', 15000); 
		var expectedSummary = "Summary of truncated text";

		var responseJson = JsonSerializer.Serialize(new
		{
			candidates = new[]
			{
				new
				{
					content = new
					{
						parts = new[]
						{
							new { text = expectedSummary }
						}
					}
				}
			}
		});

		_mockHttpMessageHandler
			.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.Is<HttpRequestMessage>(req =>
					req.Content != null &&
					req.Content.ReadAsStringAsync().Result.Contains("AAAA") 
				),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(() => new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
				Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
			});

		var service = new GeminiAiService(
			_httpClient,
			Options.Create(_options),
			_mockLogger.Object);

		// Act
		var result = await service.GenerateSummaryAsync(longText);

		// Assert
		Assert.Equal(expectedSummary, result);
	}

	[Fact]
	public async Task GenerateSummaryAsync_WithHttpTimeout_ShouldRetryAndThrowAfterMaxRetries()
	{
		// Arrange
		_mockHttpMessageHandler
			.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ThrowsAsync(new TaskCanceledException("Request timeout"));

		var service = new GeminiAiService(
			_httpClient,
			Options.Create(_options),
			_mockLogger.Object);

		// Act & Assert
		using var cts = new CancellationTokenSource();
		await Assert.ThrowsAsync<TimeoutException>(
			async () => await service.GenerateSummaryAsync("Test text", ct: cts.Token));

		_mockHttpMessageHandler.Protected().Verify(
			"SendAsync",
			Times.Exactly(_options.MaxRetries),
			ItExpr.IsAny<HttpRequestMessage>(),
			ItExpr.IsAny<CancellationToken>());
	}

	[Fact]
	public async Task GenerateSummaryAsync_WithHttpError_ShouldRetryAndThrowAfterMaxRetries()
	{
		// Arrange
		_mockHttpMessageHandler
			.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(() => new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.InternalServerError,
				Content = new StringContent("Internal Server Error")
			});

		var service = new GeminiAiService(
			_httpClient,
			Options.Create(_options),
			_mockLogger.Object);

		// Act & Assert
		await Assert.ThrowsAsync<HttpRequestException>(
			async () => await service.GenerateSummaryAsync("Test text"));

		_mockHttpMessageHandler.Protected().Verify(
			"SendAsync",
			Times.Exactly(_options.MaxRetries),
			ItExpr.IsAny<HttpRequestMessage>(),
			ItExpr.IsAny<CancellationToken>());
	}

	[Fact]
	public async Task GenerateSummaryAsync_With429RateLimitError_ShouldRetryWithExponentialBackoff()
	{
		// Arrange
		_mockHttpMessageHandler
			.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(() => new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.TooManyRequests,
				Content = new StringContent("Rate limit exceeded")
			});

		var service = new GeminiAiService(
			_httpClient,
			Options.Create(_options),
			_mockLogger.Object);

		// Act & Assert
		await Assert.ThrowsAsync<HttpRequestException>(
			async () => await service.GenerateSummaryAsync("Test text"));

		_mockHttpMessageHandler.Protected().Verify(
			"SendAsync",
			Times.Exactly(_options.MaxRetries),
			ItExpr.IsAny<HttpRequestMessage>(),
			ItExpr.IsAny<CancellationToken>());
	}

	[Fact]
	public async Task GenerateSummaryAsync_WithInvalidJsonResponse_ShouldThrowInvalidOperationException()
	{
		// Arrange
		_mockHttpMessageHandler
			.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(() => new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
				Content = new StringContent("Invalid JSON {{{", Encoding.UTF8, "application/json")
			});

		var service = new GeminiAiService(
			_httpClient,
			Options.Create(_options),
			_mockLogger.Object);

		// Act & Assert
		await Assert.ThrowsAsync<InvalidOperationException>(
			async () => await service.GenerateSummaryAsync("Test text"));
	}

	[Fact]
	public async Task GenerateSummaryAsync_WithMissingCandidates_ShouldThrowInvalidOperationException()
	{
		// Arrange
		var responseJson = JsonSerializer.Serialize(new { });

		_mockHttpMessageHandler
			.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(() => new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
				Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
			});

		var service = new GeminiAiService(
			_httpClient,
			Options.Create(_options),
			_mockLogger.Object);

		// Act & Assert 
		await Assert.ThrowsAsync<InvalidOperationException>(
			async () => await service.GenerateSummaryAsync("Test text"));
	}

	[Fact]
	public async Task GenerateSummaryAsync_WithSuccessOnSecondAttempt_ShouldReturnSummary()
	{
		// Arrange
		var expectedSummary = "Summary after retry";
		var responseJson = JsonSerializer.Serialize(new
		{
			candidates = new[]
			{
				new
				{
					content = new
					{
						parts = new[]
						{
							new { text = expectedSummary }
						}
					}
				}
			}
		});

		var attemptCount = 0;
		_mockHttpMessageHandler
			.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(() =>
			{
				attemptCount++;
				if (attemptCount == 1)
				{
					throw new TaskCanceledException("First attempt timeout");
				}
				return new HttpResponseMessage
				{
					StatusCode = HttpStatusCode.OK,
					Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
				};
			});

		var service = new GeminiAiService(
			_httpClient,
			Options.Create(_options),
			_mockLogger.Object);

		// Act
		using var cts = new CancellationTokenSource();
		var result = await service.GenerateSummaryAsync("Test text", ct: cts.Token);

		// Assert
		Assert.Equal(expectedSummary, result);
		Assert.Equal(2, attemptCount); 
	}

	[Fact]
	public async Task GenerateSummaryAsync_WithCancellationToken_ShouldHonorCancellation()
	{
		// Arrange
		var cts = new CancellationTokenSource();
		cts.Cancel(); 

		_mockHttpMessageHandler
			.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ThrowsAsync(new TaskCanceledException());

		var service = new GeminiAiService(
			_httpClient,
			Options.Create(_options),
			_mockLogger.Object);

		// Act & Assert
		await Assert.ThrowsAsync<OperationCanceledException>(
			async () => await service.GenerateSummaryAsync("Test text", ct: cts.Token));
	}

	[Fact]
	public async Task GenerateSummaryAsync_WithCustomMaxLength_ShouldIncludeInPrompt()
	{
		// Arrange
		var customMaxLength = 500;
		var expectedSummary = "Custom length summary";
		var responseJson = JsonSerializer.Serialize(new
		{
			candidates = new[]
			{
				new
				{
					content = new
					{
						parts = new[]
						{
							new { text = expectedSummary }
						}
					}
				}
			}
		});

		_mockHttpMessageHandler
			.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.Is<HttpRequestMessage>(req =>
					req.Content != null &&
					req.Content.ReadAsStringAsync().Result.Contains(customMaxLength.ToString())
				),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(() => new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
				Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
			});

		var service = new GeminiAiService(
			_httpClient,
			Options.Create(_options),
			_mockLogger.Object);

		// Act
		var result = await service.GenerateSummaryAsync("Test text", maxLength: customMaxLength);

		// Assert
		Assert.Equal(expectedSummary, result);
	}

	[Fact]
	public void Constructor_ShouldFollowDependencyInversionPrinciple()
	{
		// Arrange & Act
		var service = new GeminiAiService(
			_httpClient,
			Options.Create(_options),
			_mockLogger.Object);

		// Assert 
		Assert.NotNull(service);

		var constructorParams = typeof(GeminiAiService).GetConstructors()[0].GetParameters();
		Assert.Equal(3, constructorParams.Length);
		Assert.Equal(typeof(HttpClient), constructorParams[0].ParameterType);
		Assert.Equal(typeof(IOptions<GeminiOptions>), constructorParams[1].ParameterType);
		Assert.Equal(typeof(ILogger<GeminiAiService>), constructorParams[2].ParameterType);
	}

	[Fact]
	public void GeminiAiService_ShouldImplementIGenAiService()
	{
		// Assert 
		Assert.True(typeof(IGenAiService).IsAssignableFrom(typeof(GeminiAiService)));
	}
}
