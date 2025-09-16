using DocumentManagementSystem.Model.Abstract;

namespace DocumentManagementSystem.Model.DTO
{
	public class DocumentDTO : BaseFileDTO
	{
		public Guid Id { get; set; }

		public DocumentMetadataDTO Metadata { get; set; } = null!;
		// we don't need to type this since tags are only a string
		public List<string> Tags { get; set; } = [];

	}
}
