using AutoMapper;
using DocumentManagementSystem.Application.Services.Interfaces;
using DocumentManagementSystem.DAL;
using DocumentManagementSystem.Model.DTO;
using DocumentManagementSystem.Model.ORM;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocumentManagementSystem.Application.Services;

public class NoteService : INoteService
{
	private readonly DocumentManagementSystemContext _context;
	private readonly IMapper _mapper;
	private readonly ISearchService _searchService;
	private readonly IDocumentService _documentService;
	private readonly ILogger<NoteService> _logger;

	public NoteService(DocumentManagementSystemContext context, IMapper mapper, ISearchService searchService, IDocumentService documentService, ILogger<NoteService> logger)
	{
		_context = context;
		_mapper = mapper;
		_searchService = searchService;
		_documentService = documentService;
		_logger = logger;
	}

	public async Task<NoteDTO> AddNoteAsync(Guid documentId, CreateNoteDTO dto, CancellationToken ct = default)
	{
		_logger.LogInformation("Adding note to document {DocumentId}", documentId);

		var note = _mapper.Map<Note>(dto);
		note.DocumentId = documentId;
		note.CreatedAt = DateTimeOffset.UtcNow;

		_context.Notes.Add(note);
		await _context.SaveChangesAsync(ct);

		try
		{
			var fullDoc = await _documentService.GetByIdAsync(documentId, ct);
			if (fullDoc != null)
			{
				await _searchService.IndexDocumentAsync(fullDoc, ct);
				_logger.LogInformation("Re-indexed document {DocumentId} to include new note", documentId);
			}
		}
		catch (Exception ex)
		{
			// Don't fail the HTTP request if search indexing fails, just log it
			_logger.LogError(ex, "Failed to re-index document {DocumentId} after adding note", documentId);
		}

		return _mapper.Map<NoteDTO>(note);
	}

	public async Task<IReadOnlyList<NoteDTO>> GetNotesForDocumentAsync(Guid documentId, CancellationToken ct = default)
	{
		var notes = await _context.Notes
			.Where(n => n.DocumentId == documentId)
			.OrderByDescending(n => n.CreatedAt)
			.ToListAsync(ct);

		return _mapper.Map<IReadOnlyList<NoteDTO>>(notes);
	}
}