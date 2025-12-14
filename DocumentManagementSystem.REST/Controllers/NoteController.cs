using DocumentManagementSystem.Application.Services.Interfaces;
using DocumentManagementSystem.Model.DTO;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManagementSystem.REST.Controllers;

[ApiController]
[Route("api/document/{documentId}/notes")]
public class NoteController : ControllerBase
{
	private readonly INoteService _noteService;

	public NoteController(INoteService noteService)
	{
		_noteService = noteService;
	}

	[HttpGet]
	public async Task<ActionResult<IReadOnlyList<NoteDTO>>> GetNotes(Guid documentId, CancellationToken ct)
	{
		var notes = await _noteService.GetNotesForDocumentAsync(documentId, ct);
		return Ok(notes);
	}

	[HttpPost]
	public async Task<ActionResult<NoteDTO>> AddNote(Guid documentId, [FromBody] CreateNoteDTO dto, CancellationToken ct)
	{
		var note = await _noteService.AddNoteAsync(documentId, dto, ct);
		return CreatedAtAction(nameof(GetNotes), new { documentId }, note);
	}
}