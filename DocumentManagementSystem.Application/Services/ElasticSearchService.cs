using DocumentManagementSystem.Application.Configuration;
using DocumentManagementSystem.Application.Services.Enums;
using DocumentManagementSystem.Application.Services.Interfaces;
using DocumentManagementSystem.Model.DTO;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocumentManagementSystem.Application.Services;

public class ElasticSearchService : ISearchService
{
	private readonly ElasticsearchClient _client;
	private readonly ILogger<ElasticSearchService> _logger;
	private readonly string _indexName;

	public ElasticSearchService(
		 IOptions<ElasticSearchOptions> options,
		 ILogger<ElasticSearchService> logger)
	{
		_logger = logger;
		_indexName = options.Value.DefaultIndex;

		var settings = new ElasticsearchClientSettings(new Uri(options.Value.Uri))
			 .DefaultIndex(_indexName);

		_client = new ElasticsearchClient(settings);
	}

	public async Task IndexDocumentAsync(DocumentDTO document, CancellationToken ct = default)
	{
		_logger.LogInformation("Indexing document {DocumentId} to Elasticsearch", document.Id);

		var response = await _client.IndexAsync(document, idx => idx.Index(_indexName), ct);

		if (!response.IsValidResponse)
		{
			_logger.LogError("Failed to index document {DocumentId}: {DebugInformation}",
				 document.Id, response.DebugInformation);
			throw new Exception($"Failed to index document: {response.DebugInformation}");
		}

		_logger.LogInformation("Document {DocumentId} successfully indexed", document.Id);
	}

	public async Task<IReadOnlyList<DocumentDTO>> SearchAsync(string searchTerm, SearchMode mode, CancellationToken ct = default)
	{
		_logger.LogInformation("Searching documents with term: '{SearchTerm}'", searchTerm);

		if (string.IsNullOrWhiteSpace(searchTerm))
		{
			return new List<DocumentDTO>();
		}

		_logger.LogInformation(mode.ToString());
		var fields = mode switch
		{
			SearchMode.Notes => new[]
			{
				Infer.Field<DocumentDTO>(p => p.Notes)
			},
			_ => new[] // Content is the default search mode
			{
				Infer.Field<DocumentDTO>(p => p.FileName),
				Infer.Field<DocumentDTO>(p => p.Tags),
				Infer.Field<DocumentDTO>(p => p.Metadata.OcrText),
				Infer.Field<DocumentDTO>(p => p.Metadata.Summary)
			}
		};

		var response = await _client.SearchAsync<DocumentDTO>(s => s
			.Indices(_indexName)
			.Query(q => q
				.MultiMatch(m => m
					.Fields(fields)
					.Query(searchTerm)
					.Fuzziness(new Fuzziness("AUTO"))
				)
			), ct);

		if (!response.IsValidResponse)
		{
			_logger.LogError("Search failed: {DebugInformation}", response.DebugInformation);
			return new List<DocumentDTO>();
		}

		var results = response.Documents.ToList();
		_logger.LogInformation("Found {Count} documents matching '{SearchTerm}'", results.Count, searchTerm);
		return results;
	}
}