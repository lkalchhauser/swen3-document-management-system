using DocumentManagementSystem.BatchWorker.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocumentManagementSystem.BatchWorker.Tests.Services
{
	public class XmlParserServiceTests
	{
		private readonly Mock<ILogger<XmlParserService>> _loggerMock;
		private readonly XmlParserService _service;

		public XmlParserServiceTests()
		{
			_loggerMock = new Mock<ILogger<XmlParserService>>();
			_service = new XmlParserService(_loggerMock.Object);
		}

		[Fact]
		public async Task ParseAsync_ValidXml_ReturnsCorrectBatch()
		{
			var tempFile = Path.GetTempFileName();
			var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<accessLogBatch batchDate=""2026-01-18"">
  <entry documentId=""a1b2c3d4-e5f6-7890-abcd-ef1234567890"" accessCount=""15"" />
  <entry documentId=""b2c3d4e5-f6a7-8901-bcde-f12345678901"" accessCount=""8"" />
</accessLogBatch>";
			await File.WriteAllTextAsync(tempFile, xml);

			try
			{
				var result = await _service.ParseAsync(tempFile);

				Assert.Equal(new DateOnly(2026, 1, 18), result.BatchDate);
				Assert.Equal(2, result.Entries.Count);
				Assert.Equal(Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), result.Entries[0].DocumentId);
				Assert.Equal(15, result.Entries[0].AccessCount);
				Assert.Equal(Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901"), result.Entries[1].DocumentId);
				Assert.Equal(8, result.Entries[1].AccessCount);
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[Fact]
		public async Task ParseAsync_MissingBatchDate_ThrowsInvalidDataException()
		{
			var tempFile = Path.GetTempFileName();
			var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<accessLogBatch>
  <entry documentId=""a1b2c3d4-e5f6-7890-abcd-ef1234567890"" accessCount=""15"" />
</accessLogBatch>";
			await File.WriteAllTextAsync(tempFile, xml);

			try
			{
				await Assert.ThrowsAsync<InvalidDataException>(() => _service.ParseAsync(tempFile));
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[Fact]
		public async Task ParseAsync_InvalidBatchDate_ThrowsInvalidDataException()
		{
			var tempFile = Path.GetTempFileName();
			var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<accessLogBatch batchDate=""INVALID"">
  <entry documentId=""a1b2c3d4-e5f6-7890-abcd-ef1234567890"" accessCount=""15"" />
</accessLogBatch>";
			await File.WriteAllTextAsync(tempFile, xml);

			try
			{
				await Assert.ThrowsAsync<InvalidDataException>(() => _service.ParseAsync(tempFile));
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[Fact]
		public async Task ParseAsync_InvalidDocumentId_ThrowsInvalidDataException()
		{
			var tempFile = Path.GetTempFileName();
			var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<accessLogBatch batchDate=""2026-01-18"">
  <entry documentId=""NOT-A-GUID"" accessCount=""15"" />
</accessLogBatch>";
			await File.WriteAllTextAsync(tempFile, xml);

			try
			{
				await Assert.ThrowsAsync<InvalidDataException>(() => _service.ParseAsync(tempFile));
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[Fact]
		public async Task ParseAsync_NegativeAccessCount_ThrowsInvalidDataException()
		{
			var tempFile = Path.GetTempFileName();
			var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<accessLogBatch batchDate=""2026-01-18"">
  <entry documentId=""a1b2c3d4-e5f6-7890-abcd-ef1234567890"" accessCount=""-5"" />
</accessLogBatch>";
			await File.WriteAllTextAsync(tempFile, xml);

			try
			{
				await Assert.ThrowsAsync<InvalidDataException>(() => _service.ParseAsync(tempFile));
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[Fact]
		public async Task ParseAsync_MissingDocumentId_ThrowsInvalidDataException()
		{
			var tempFile = Path.GetTempFileName();
			var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<accessLogBatch batchDate=""2026-01-18"">
  <entry accessCount=""15"" />
</accessLogBatch>";
			await File.WriteAllTextAsync(tempFile, xml);

			try
			{
				await Assert.ThrowsAsync<InvalidDataException>(() => _service.ParseAsync(tempFile));
			}
			finally
			{
				File.Delete(tempFile);
			}
		}
	}
}
