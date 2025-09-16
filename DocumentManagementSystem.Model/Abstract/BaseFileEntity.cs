namespace DocumentManagementSystem.Model.Abstract
{
	public abstract class BaseFileEntity : BaseEntity
	{
		public string FileName { get; set; } = null!;
	}
}
