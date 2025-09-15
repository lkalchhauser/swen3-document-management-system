namespace DocumentManagementSystem.Model.Abstract
{
	public abstract class BaseFileEntity : BaseEntity
	{
		public string FileName { get; set; }
		public string ContentType { get; set; }
		public long FileSize { get; set; }
	}
}
