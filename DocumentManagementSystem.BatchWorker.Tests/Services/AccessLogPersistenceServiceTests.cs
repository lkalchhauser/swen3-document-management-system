using DocumentManagementSystem.BatchWorker.Services;
using DocumentManagementSystem.BatchWorker.Services.Interfaces;
using DocumentManagementSystem.DAL;
using DocumentManagementSystem.DAL.Repositories.Interfaces;
using DocumentManagementSystem.Model.ORM;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocumentManagementSystem.BatchWorker.Tests.Services
{
	public class AccessLogPersistenceServiceTests
	{
		private readonly Mock<ILogger<AccessLogPersistenceService>> _loggerMock;
		private readonly Mock<IDocumentAccessLogRepository> _accessLogRepositoryMock;
		private readonly Mock<IDocumentRepository> _documentRepositoryMock;
		private readonly DocumentManagementSystemContext _context;
		private readonly AccessLogPersistenceService _service;

		public AccessLogPersistenceServiceTests()
		{
			_loggerMock = new Mock<ILogger<AccessLogPersistenceService>>();
			_accessLogRepositoryMock = new Mock<IDocumentAccessLogRepository>();
			_documentRepositoryMock = new Mock<IDocumentRepository>();

			// Use in-memory database for context
			var options = new DbContextOptionsBuilder<DocumentManagementSystemContext>()
				.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
				.Options;
			_context = new DocumentManagementSystemContext(options);

			_service = new AccessLogPersistenceService(
				_loggerMock.Object,
				_accessLogRepositoryMock.Object,
				_documentRepositoryMock.Object,
				_context);
		}

		[Fact]
		public async Task SaveAccessLogsAsync_NewEntry_CreatesNewLog()
		{
			var documentId = Guid.NewGuid();
			var batchDate = new DateOnly(2026, 1, 18);
			var entry = new AccessEntry(documentId, 10);
			var batch = new AccessLogBatch(batchDate, new[] { entry });

			_documentRepositoryMock.Setup(r => r.ExistsAsync(documentId, It.IsAny<CancellationToken>()))
				.ReturnsAsync(true);
			_accessLogRepositoryMock.Setup(r => r.GetByDocumentAndDateAsync(documentId, batchDate, It.IsAny<CancellationToken>()))
				.ReturnsAsync((DocumentAccessLog?)null);

			var result = await _service.SaveAccessLogsAsync(batch);

			Assert.Equal(1, result.ProcessedCount);
			Assert.Equal(0, result.ErrorCount);
			_accessLogRepositoryMock.Verify(r => r.AddAsync(It.Is<DocumentAccessLog>(log =>
				log.DocumentId == documentId &&
				log.AccessDate == batchDate &&
				log.AccessCount == 10
			), It.IsAny<CancellationToken>()), Times.Once);
			_documentRepositoryMock.Verify(r => r.IncrementAccessCountAsync(documentId, 10, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task SaveAccessLogsAsync_ExistingEntry_UpdatesAccessCount()
		{
			var documentId = Guid.NewGuid();
			var batchDate = new DateOnly(2026, 1, 18);
			var entry = new AccessEntry(documentId, 5);
			var batch = new AccessLogBatch(batchDate, new[] { entry });

			var existingLog = new DocumentAccessLog
			{
				Id = Guid.NewGuid(),
				DocumentId = documentId,
				AccessDate = batchDate,
				AccessCount = 10,
				CreatedAt = DateTimeOffset.UtcNow
			};

			_documentRepositoryMock.Setup(r => r.ExistsAsync(documentId, It.IsAny<CancellationToken>()))
				.ReturnsAsync(true);
			_accessLogRepositoryMock.Setup(r => r.GetByDocumentAndDateAsync(documentId, batchDate, It.IsAny<CancellationToken>()))
				.ReturnsAsync(existingLog);

			var result = await _service.SaveAccessLogsAsync(batch);

			Assert.Equal(1, result.ProcessedCount);
			Assert.Equal(0, result.ErrorCount);
			Assert.Equal(15, existingLog.AccessCount);
			_accessLogRepositoryMock.Verify(r => r.UpdateAsync(existingLog, It.IsAny<CancellationToken>()), Times.Once);
			_documentRepositoryMock.Verify(r => r.IncrementAccessCountAsync(documentId, 5, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task SaveAccessLogsAsync_NonExistentDocument_LogsError()
		{
			var documentId = Guid.NewGuid();
			var batchDate = new DateOnly(2026, 1, 18);
			var entry = new AccessEntry(documentId, 10);
			var batch = new AccessLogBatch(batchDate, new[] { entry });

			_documentRepositoryMock.Setup(r => r.ExistsAsync(documentId, It.IsAny<CancellationToken>()))
				.ReturnsAsync(false);

			var result = await _service.SaveAccessLogsAsync(batch, "test-file.xml");

			Assert.Equal(0, result.ProcessedCount);
			Assert.Equal(1, result.ErrorCount);
			Assert.True(result.HasErrors);

			// Should not process the entry
			_accessLogRepositoryMock.Verify(r => r.AddAsync(It.IsAny<DocumentAccessLog>(), It.IsAny<CancellationToken>()), Times.Never);
			_accessLogRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<DocumentAccessLog>(), It.IsAny<CancellationToken>()), Times.Never);
			_documentRepositoryMock.Verify(r => r.IncrementAccessCountAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);

			// Should have logged error to database
			Assert.Single(_context.BatchProcessingErrors);
			var error = _context.BatchProcessingErrors.First();
			Assert.Equal(documentId, error.DocumentId);
			Assert.Equal("test-file.xml", error.FileName);
			Assert.Equal("Document ID does not exist in database", error.ErrorMessage);
		}

		[Fact]
		public async Task SaveAccessLogsAsync_MultipleEntries_ProcessesAll()
		{
			var doc1Id = Guid.NewGuid();
			var doc2Id = Guid.NewGuid();
			var batchDate = new DateOnly(2026, 1, 18);
			var entries = new[]
			{
				new AccessEntry(doc1Id, 10),
				new AccessEntry(doc2Id, 15)
			};
			var batch = new AccessLogBatch(batchDate, entries);

			_documentRepositoryMock.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(true);
			_accessLogRepositoryMock.Setup(r => r.GetByDocumentAndDateAsync(It.IsAny<Guid>(), batchDate, It.IsAny<CancellationToken>()))
				.ReturnsAsync((DocumentAccessLog?)null);

			var result = await _service.SaveAccessLogsAsync(batch);

			Assert.Equal(2, result.ProcessedCount);
			Assert.Equal(0, result.ErrorCount);
			Assert.False(result.HasErrors);

			_accessLogRepositoryMock.Verify(r => r.AddAsync(It.IsAny<DocumentAccessLog>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
			_documentRepositoryMock.Verify(r => r.IncrementAccessCountAsync(doc1Id, 10, It.IsAny<CancellationToken>()), Times.Once);
			_documentRepositoryMock.Verify(r => r.IncrementAccessCountAsync(doc2Id, 15, It.IsAny<CancellationToken>()), Times.Once);
		}
	}
}
