using DocumentManagementSystem.Model.Abstract;

namespace DocumentManagementSystem.Model.ORM
{
	public class DocumentMetadata : BaseEntity
	{
		public Guid DocumentId { get; set; }
		// we use Key/Value separately instead of a collection since EFCore doesn't directly support collections
		public string Key { get; set; }
		public string Value { get; set; }

		// only for help with navigation
		public Document Document { get; set; }
	}
}
