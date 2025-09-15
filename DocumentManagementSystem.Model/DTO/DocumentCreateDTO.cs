using DocumentManagementSystem.Model.Abstract;

namespace DocumentManagementSystem.Model.DTO
{
	public class DocumentCreateDTO : BaseFileDto
	{
		public List<string>? Tags { get; set; }
		public Dictionary<string, string>? Metadata { get; set; }
	}
}
