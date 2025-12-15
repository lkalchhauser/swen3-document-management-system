using DocumentManagementSystem.Model.Abstract;

namespace DocumentManagementSystem.Model.ORM
{
	public class Document : BaseFileEntity
	{
		public ICollection<Tag> Tags { get; set; } = [];

		public DocumentMetadata? Metadata { get; set; }
		// navigation property
		public ICollection<Note> Notes { get; set; } = [];
	}
}
