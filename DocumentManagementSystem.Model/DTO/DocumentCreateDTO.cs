using DocumentManagementSystem.Model.Abstract;

namespace DocumentManagementSystem.Model.DTO
{
	public class DocumentCreateDTO : BaseFileDTO
	{
		public long FileSize { get; set; }
		// we are validating this on the server side
		public string ContentType { get; set; } = string.Empty;
		public List<string> Tags { get; set; } = [];
	}
}
