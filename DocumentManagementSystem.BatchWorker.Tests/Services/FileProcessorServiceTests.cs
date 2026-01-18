using DocumentManagementSystem.BatchWorker.Configuration;
using DocumentManagementSystem.BatchWorker.Services;
using DocumentManagementSystem.BatchWorker.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace DocumentManagementSystem.BatchWorker.Tests.Services
{
	public class FileProcessorServiceTests
	{
		private readonly Mock<ILogger<FileProcessorService>> _loggerMock;
		private readonly Mock<IXmlParserService> _xmlParserMock;
		private readonly Mock<IAccessLogPersistenceService> _persistenceMock;
		private readonly string _tempDir;
		private readonly BatchProcessingOptions _options;
		private readonly FileProcessorService _service;

		public FileProcessorServiceTests()
		{
			_loggerMock = new Mock<ILogger<FileProcessorService>>();
			_xmlParserMock = new Mock<IXmlParserService>();
			_persistenceMock = new Mock<IAccessLogPersistenceService>();

			_tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
			_options = new BatchProcessingOptions
			{
				InputFolder = Path.Combine(_tempDir, "input"),
				ArchiveFolder = Path.Combine(_tempDir, "archive"),
				ErrorFolder = Path.Combine(_tempDir, "error"),
				FilePattern = "*.xml"
			};

			var optionsMock = new Mock<IOptions<BatchProcessingOptions>>();
			optionsMock.Setup(o => o.Value).Returns(_options);

			_service = new FileProcessorService(
				_loggerMock.Object,
				optionsMock.Object,
				_xmlParserMock.Object,
				_persistenceMock.Object);
		}

		[Fact]
		public async Task ProcessFilesAsync_NoFiles_LogsAndReturns()
		{
			Directory.CreateDirectory(_options.InputFolder);

			await _service.ProcessFilesAsync();

			_xmlParserMock.Verify(x => x.ParseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
			Directory.Delete(_tempDir, true);
		}

		[Fact]
		public async Task ProcessFilesAsync_ValidFile_MovesToArchive()
		{
			Directory.CreateDirectory(_options.InputFolder);
			var testFile = Path.Combine(_options.InputFolder, "test.xml");
			await File.WriteAllTextAsync(testFile, "<test/>");

			var batch = new AccessLogBatch(new DateOnly(2026, 1, 18), Array.Empty<AccessEntry>());
			var result = new AccessLogPersistenceResult { ProcessedCount = 0, ErrorCount = 0 };

			_xmlParserMock.Setup(x => x.ParseAsync(testFile, It.IsAny<CancellationToken>()))
				.ReturnsAsync(batch);
			_persistenceMock.Setup(p => p.SaveAccessLogsAsync(batch, "test.xml", It.IsAny<CancellationToken>()))
				.ReturnsAsync(result);

			await _service.ProcessFilesAsync();

			_persistenceMock.Verify(p => p.SaveAccessLogsAsync(batch, "test.xml", It.IsAny<CancellationToken>()), Times.Once);
			Assert.False(File.Exists(testFile));
			Assert.True(File.Exists(Path.Combine(_options.ArchiveFolder, "test.xml")));

			Directory.Delete(_tempDir, true);
		}

		[Fact]
		public async Task ProcessFilesAsync_InvalidXml_MovesToError()
		{
			Directory.CreateDirectory(_options.InputFolder);
			var testFile = Path.Combine(_options.InputFolder, "invalid.xml");
			await File.WriteAllTextAsync(testFile, "<test/>");

			_xmlParserMock.Setup(x => x.ParseAsync(testFile, It.IsAny<CancellationToken>()))
				.ThrowsAsync(new InvalidDataException("Invalid XML"));

			await _service.ProcessFilesAsync();

			_persistenceMock.Verify(p => p.SaveAccessLogsAsync(It.IsAny<AccessLogBatch>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
			Assert.False(File.Exists(testFile));
			Assert.True(File.Exists(Path.Combine(_options.ErrorFolder, "invalid.xml")));

			Directory.Delete(_tempDir, true);
		}

		[Fact]
		public async Task ProcessFilesAsync_InvalidDocumentIds_MovesToError()
		{
			Directory.CreateDirectory(_options.InputFolder);
			var testFile = Path.Combine(_options.InputFolder, "errors.xml");
			await File.WriteAllTextAsync(testFile, "<test/>");

			var batch = new AccessLogBatch(new DateOnly(2026, 1, 18), Array.Empty<AccessEntry>());
			var result = new AccessLogPersistenceResult { ProcessedCount = 0, ErrorCount = 2 };

			_xmlParserMock.Setup(x => x.ParseAsync(testFile, It.IsAny<CancellationToken>()))
				.ReturnsAsync(batch);
			_persistenceMock.Setup(p => p.SaveAccessLogsAsync(batch, "errors.xml", It.IsAny<CancellationToken>()))
				.ReturnsAsync(result);

			await _service.ProcessFilesAsync();

			_persistenceMock.Verify(p => p.SaveAccessLogsAsync(batch, "errors.xml", It.IsAny<CancellationToken>()), Times.Once);
			Assert.False(File.Exists(testFile));
			Assert.True(File.Exists(Path.Combine(_options.ErrorFolder, "errors.xml")));

			Directory.Delete(_tempDir, true);
		}
	}
}
