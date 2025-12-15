using DocumentManagementSystem.Model.Abstract;

namespace DocumentManagementSystem.Model.ORM;

public class Note : BaseEntity
{
	public string Text { get; set; } = string.Empty;
	public Guid DocumentId { get; set; }
	public Document? Document { get; set; }
}