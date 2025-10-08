using AutoMapper;
using DocumentManagementSystem.Application.Services.Interfaces;
using DocumentManagementSystem.DAL.Repositories.Interfaces;
using DocumentManagementSystem.Messaging.Interfaces;
using DocumentManagementSystem.Model.DTO;
using DocumentManagementSystem.Model.ORM;
using Microsoft.Extensions.Logging;

namespace DocumentManagementSystem.Application.Services
{
	public class DocumentService : IDocumentService
	{
		private readonly IDocumentRepository _repository;
		private readonly IMapper _mapper;
		private readonly IMessagePublisherService _messagePublisherService;
		private readonly ILogger<DocumentService> _logger;

		public DocumentService(IDocumentRepository repository, IMapper mapper, IMessagePublisherService messagePublisherService, ILogger<DocumentService> logger)
		{
			_repository = repository;
			_mapper = mapper;
			_messagePublisherService = messagePublisherService;
			_logger = logger;
		}

		public async Task<DocumentDTO?> GetByIdAsync(Guid id, CancellationToken ct = default)
		{
			_logger.LogDebug("Retrieving document {DocumentId} from repository", id);
			var doc = await _repository.GetByIdAsync(id, ct);
			if (doc is null)
			{
				_logger.LogDebug("Document {DocumentId} not found in repository", id);
			}
			return doc is null ? null : _mapper.Map<DocumentDTO>(doc);
		}

		public async Task<IReadOnlyList<DocumentDTO>> GetAllAsync(CancellationToken ct = default)
		{
			_logger.LogDebug("Retrieving all documents from repository");
			var docs = await _repository.GetAllAsync(ct);
			_logger.LogDebug("Retrieved {Count} documents from repository", docs.Count);
			return _mapper.Map<IReadOnlyList<DocumentDTO>>(docs);
		}

		public async Task<DocumentDTO> CreateAsync(DocumentCreateDTO dto, CancellationToken ct = default)
		{
			_logger.LogInformation("Creating new document: {FileName}", dto.FileName);
			var doc = _mapper.Map<Document>(dto);

			doc.CreatedAt = DateTimeOffset.UtcNow;
			// TODO: Server-side metadata verification
			doc.Metadata = new DocumentMetadata
			{
				CreatedAt = DateTimeOffset.UtcNow,
				ContentType = dto.ContentType ?? "application/octet-stream",
				FileSize = 0 // Will be updated later when file is stored
			};


			await _repository.AddAsync(doc, ct);
			await _repository.SaveChangesAsync(ct);
			_logger.LogInformation("Document saved to repository with ID: {DocumentId}", doc.Id);

			var message = new DocumentUploadMessageDTO(
				DocumentId: doc.Id,
				FileName: doc.FileName,
				StoragePath: null,
				UploadedAtUtc: doc.CreatedAt
			);

			_logger.LogDebug("Publishing document upload message for {DocumentId}", doc.Id);
			await _messagePublisherService.PublishAsync(message, ct);
			_logger.LogInformation("Document upload message published for {DocumentId}", doc.Id);

			return _mapper.Map<DocumentDTO>(doc);
		}

		public async Task<DocumentDTO?> UpdateAsync(Guid id, DocumentCreateDTO dto, CancellationToken ct = default)
		{
			_logger.LogInformation("Updating document {DocumentId}", id);
			var existing = await _repository.GetByIdAsync(id, ct);
			if (existing is null)
			{
				_logger.LogWarning("Cannot update document {DocumentId}: not found", id);
				return null;
			}

			existing.FileName = dto.FileName;
			existing.Metadata!.UpdatedAt = DateTimeOffset.UtcNow;
			// TODO: don't allow metadata from client
			existing.Metadata.ContentType = dto.ContentType ?? existing.Metadata.ContentType;

			existing.Tags.Clear();
			foreach (var tagName in dto.Tags)
			{
				existing.Tags.Add(new Tag { Name = tagName });
			}

			await _repository.UpdateAsync(existing, ct);
			await _repository.SaveChangesAsync(ct);
			_logger.LogInformation("Document {DocumentId} updated successfully", id);

			return _mapper.Map<DocumentDTO>(existing);
		}

		public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
		{
			_logger.LogInformation("Deleting document {DocumentId}", id);
			var doc = await _repository.GetByIdAsync(id, ct);
			if (doc is null)
			{
				_logger.LogWarning("Cannot delete document {DocumentId}: not found", id);
				return false;
			}

			await _repository.DeleteAsync(doc, ct);
			await _repository.SaveChangesAsync(ct);
			_logger.LogInformation("Document {DocumentId} deleted successfully", id);
			return true;
		}
	}
}
