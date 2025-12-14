namespace DocumentManagementSystem.Application.Configuration;

public class ElasticSearchOptions
{
	public const string SectionName = "ElasticSearch";
	public string Uri { get; set; } = "http://localhost:9200";
	public string DefaultIndex { get; set; } = "documents";
}