namespace DocumentManagementSystem.Model.DTO;

public class NoteDTO
{
	public Guid Id { get; set; }
	public string Text { get; set; } = string.Empty;
	public DateTimeOffset CreatedAt { get; set; }
	public Guid DocumentId { get; set; }
}