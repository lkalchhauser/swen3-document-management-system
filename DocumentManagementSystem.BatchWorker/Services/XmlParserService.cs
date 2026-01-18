using System.Xml.Linq;
using DocumentManagementSystem.BatchWorker.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace DocumentManagementSystem.BatchWorker.Services
{
	public class XmlParserService : IXmlParserService
	{
		private readonly ILogger<XmlParserService> _logger;

		public XmlParserService(ILogger<XmlParserService> logger)
		{
			_logger = logger;
		}

		public Task<AccessLogBatch> ParseAsync(string filePath, CancellationToken cancellationToken = default)
		{
			_logger.LogDebug("Parsing XML file: {FilePath}", filePath);

			var xdoc = XDocument.Load(filePath);
			var root = xdoc.Root ?? throw new InvalidDataException("Missing root element");

			if (root.Name != "accessLogBatch")
			{
				throw new InvalidDataException($"Invalid root element: expected 'accessLogBatch', found '{root.Name}'");
			}

			var batchDateAttr = root.Attribute("batchDate")?.Value
				?? throw new InvalidDataException("Missing required attribute 'batchDate'");

			if (!DateOnly.TryParse(batchDateAttr, out var batchDate))
			{
				throw new InvalidDataException($"Invalid batchDate format: {batchDateAttr}");
			}

			var entries = root.Elements("entry")
				.Select((element, index) => ParseEntry(element, index))
				.ToList();

			_logger.LogDebug("Parsed {Count} entries from batch dated {BatchDate}", entries.Count, batchDate);

			return Task.FromResult(new AccessLogBatch(batchDate, entries));
		}

		private static AccessEntry ParseEntry(XElement element, int index)
		{
			var documentIdAttr = element.Attribute("documentId")?.Value
				?? throw new InvalidDataException($"Entry {index}: Missing required attribute 'documentId'");

			var accessCountAttr = element.Attribute("accessCount")?.Value
				?? throw new InvalidDataException($"Entry {index}: Missing required attribute 'accessCount'");

			if (!Guid.TryParse(documentIdAttr, out var documentId))
			{
				throw new InvalidDataException($"Entry {index}: Invalid GUID format for documentId: {documentIdAttr}");
			}

			if (!int.TryParse(accessCountAttr, out var accessCount))
			{
				throw new InvalidDataException($"Entry {index}: Invalid integer format for accessCount: {accessCountAttr}");
			}

			if (accessCount < 0)
			{
				throw new InvalidDataException($"Entry {index}: accessCount must be non-negative, got: {accessCount}");
			}

			return new AccessEntry(documentId, accessCount);
		}
	}
}
