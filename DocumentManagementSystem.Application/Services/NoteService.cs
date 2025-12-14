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
	private readonly ILogger<NoteService> _logger;

	public NoteService(DocumentManagementSystemContext context, IMapper mapper, ILogger<NoteService> logger)
	{
		_context = context;
		_mapper = mapper;
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