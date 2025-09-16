using DocumentManagementSystem.Model.Abstract;

namespace DocumentManagementSystem.Model.ORM
{
	public class Tag : BaseEntity
	{
		public string? Name { get; set; }

		public ICollection<Document> Documents { get; set; } = new List<Document>();
	}
}
