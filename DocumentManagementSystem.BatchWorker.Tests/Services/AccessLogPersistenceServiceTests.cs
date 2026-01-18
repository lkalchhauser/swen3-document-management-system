using DocumentManagementSystem.BatchWorker.Services;
using DocumentManagementSystem.BatchWorker.Services.Interfaces;
using DocumentManagementSystem.DAL.Repositories.Interfaces;
using DocumentManagementSystem.Model.ORM;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocumentManagementSystem.BatchWorker.Tests.Services
{
	public class AccessLogPersistenceServiceTests
	{
		private readonly Mock<ILogger<AccessLogPersistenceService>> _loggerMock;
		private readonly Mock<IDocumentAccessLogRepository> _repositoryMock;
		private readonly AccessLogPersistenceService _service;

		public AccessLogPersistenceServiceTests()
		{
			_loggerMock = new Mock<ILogger<AccessLogPersistenceService>>();
			_repositoryMock = new Mock<IDocumentAccessLogRepository>();
			_service = new AccessLogPersistenceService(_loggerMock.Object, _repositoryMock.Object);
		}

		[Fact]
		public async Task SaveAccessLogsAsync_NewEntry_CreatesNewLog()
		{
			var documentId = Guid.NewGuid();
			var batchDate = new DateOnly(2026, 1, 18);
			var entry = new AccessEntry(documentId, 10);
			var batch = new AccessLogBatch(batchDate, new[] { entry });

			_repositoryMock.Setup(r => r.DocumentExistsAsync(documentId, It.IsAny<CancellationToken>()))
				.ReturnsAsync(true);
			_repositoryMock.Setup(r => r.GetByDocumentAndDateAsync(documentId, batchDate, It.IsAny<CancellationToken>()))
				.ReturnsAsync((DocumentAccessLog?)null);

			await _service.SaveAccessLogsAsync(batch);

			_repositoryMock.Verify(r => r.AddAsync(It.Is<DocumentAccessLog>(log =>
				log.DocumentId == documentId &&
				log.AccessDate == batchDate &&
				log.AccessCount == 10
			), It.IsAny<CancellationToken>()), Times.Once);
			_repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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

			_repositoryMock.Setup(r => r.DocumentExistsAsync(documentId, It.IsAny<CancellationToken>()))
				.ReturnsAsync(true);
			_repositoryMock.Setup(r => r.GetByDocumentAndDateAsync(documentId, batchDate, It.IsAny<CancellationToken>()))
				.ReturnsAsync(existingLog);

			await _service.SaveAccessLogsAsync(batch);

			Assert.Equal(15, existingLog.AccessCount);
			_repositoryMock.Verify(r => r.UpdateAsync(existingLog, It.IsAny<CancellationToken>()), Times.Once);
			_repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task SaveAccessLogsAsync_NonExistentDocument_SkipsEntry()
		{
			var documentId = Guid.NewGuid();
			var batchDate = new DateOnly(2026, 1, 18);
			var entry = new AccessEntry(documentId, 10);
			var batch = new AccessLogBatch(batchDate, new[] { entry });

			_repositoryMock.Setup(r => r.DocumentExistsAsync(documentId, It.IsAny<CancellationToken>()))
				.ReturnsAsync(false);

			await _service.SaveAccessLogsAsync(batch);

			_repositoryMock.Verify(r => r.AddAsync(It.IsAny<DocumentAccessLog>(), It.IsAny<CancellationToken>()), Times.Never);
			_repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<DocumentAccessLog>(), It.IsAny<CancellationToken>()), Times.Never);
			_repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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

			_repositoryMock.Setup(r => r.DocumentExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(true);
			_repositoryMock.Setup(r => r.GetByDocumentAndDateAsync(It.IsAny<Guid>(), batchDate, It.IsAny<CancellationToken>()))
				.ReturnsAsync((DocumentAccessLog?)null);

			await _service.SaveAccessLogsAsync(batch);

			_repositoryMock.Verify(r => r.AddAsync(It.IsAny<DocumentAccessLog>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
			_repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
		}
	}
}
