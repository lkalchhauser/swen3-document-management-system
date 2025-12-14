using DocumentManagementSystem.Model.DTO;

namespace DocumentManagementSystem.Application.Services.Interfaces;

public interface INoteService
{
	Task<NoteDTO> AddNoteAsync(Guid documentId, CreateNoteDTO dto, CancellationToken ct = default);
	Task<IReadOnlyList<NoteDTO>> GetNotesForDocumentAsync(Guid documentId, CancellationToken ct = default);
}