using AutoMapper;
using DocumentManagementService.DAL.Repositories.Interfaces;
using DocumentManagementService.DAL.Services.Interfaces;
using DocumentManagementSystem.Model.DTO;
using DocumentManagementSystem.Model.ORM;

namespace DocumentManagementService.DAL.Services
{
	public class DocumentService : IDocumentService
	{
		private readonly IDocumentRepository _repository;
		private readonly IMapper _mapper;

		public DocumentService(IDocumentRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public async Task<DocumentDTO?> GetByIdAsync(Guid id, CancellationToken ct = default)
		{
			var doc = await _repository.GetByIdAsync(id, ct);
			return doc is null ? null : _mapper.Map<DocumentDTO>(doc);
		}

		public async Task<IReadOnlyList<DocumentDTO>> GetAllAsync(CancellationToken ct = default)
		{
			var docs = await _repository.GetAllAsync(ct);
			return _mapper.Map<IReadOnlyList<DocumentDTO>>(docs);
		}

		public async Task<DocumentDTO> CreateAsync(DocumentCreateDTO dto, CancellationToken ct = default)
		{
			var doc = _mapper.Map<Document>(dto);

			doc.CreatedAt = DateTimeOffset.UtcNow;
			// Server-side metadata
			doc.Metadata = new DocumentMetadata
			{
				CreatedAt = DateTimeOffset.UtcNow,
				ContentType = dto.ContentType ?? "application/octet-stream",
				FileSize = 0 // Will be updated later when file is stored
			};

			await _repository.AddAsync(doc, ct);
			await _repository.SaveChangesAsync(ct);

			return _mapper.Map<DocumentDTO>(doc);
		}

		public async Task<DocumentDTO?> UpdateAsync(Guid id, DocumentCreateDTO dto, CancellationToken ct = default)
		{
			var existing = await _repository.GetByIdAsync(id, ct);
			if (existing is null) return null;

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

			return _mapper.Map<DocumentDTO>(existing);
		}

		public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
		{
			var doc = await _repository.GetByIdAsync(id, ct);
			if (doc is null) return false;

			await _repository.DeleteAsync(doc, ct);
			await _repository.SaveChangesAsync(ct);
			return true;
		}
	}
}
