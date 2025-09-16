using DocumentManagementSystem.Model.Abstract;

namespace DocumentManagementSystem.Model.ORM
{
	public class Document : BaseFileEntity
	{
		public ICollection<Tag> Tags { get; set; } = new List<Tag>();

		public DocumentMetadata? Metadata { get; set; }
	}
}
