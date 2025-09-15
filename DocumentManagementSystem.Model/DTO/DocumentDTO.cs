using DocumentManagementSystem.Model.Abstract;

namespace DocumentManagementSystem.Model.DTO
{
	public class DocumentDTO : BaseFileDto
	{
		public Guid Id { get; set; }
		public DateTime UploadedAt { get; set; }
		public string? OcrText { get; set; }
		public string? Summary { get; set; }

		public List<string> Tags { get; set; } = [];
		public Dictionary<string, string> Metadata { get; set; } = [];

	}
}
