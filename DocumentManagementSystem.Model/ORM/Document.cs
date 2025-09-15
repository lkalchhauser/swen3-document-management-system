using DocumentManagementSystem.Model.Abstract;

namespace DocumentManagementSystem.Model.ORM
{
	public class Document : BaseFileEntity
	{
		public string FilePath { get; set; } // for MiniIO in the future
		public string? OcrText { get; set; }
		public string? Summary { get; set; }

		public DateTime UploadedAt { get; set; }

		public ICollection<Tag> Tags { get; set; } = new List<Tag>();


	}
}
