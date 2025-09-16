namespace DocumentManagementSystem.Model.Abstract
{
	public abstract class BaseEntity
	{
		public Guid Id { get; set; }
		public DateTimeOffset CreatedAt { get; set; }
	}
}
