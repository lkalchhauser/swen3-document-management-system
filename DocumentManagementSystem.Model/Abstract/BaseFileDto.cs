namespace DocumentManagementSystem.Model.Abstract
{
	public class BaseFileDto
	{
		public string FileName { get; set; }
		// calculated by frontend - We will verify this server side too
		public string ContentType { get; set; }
		// calculated by frontend - we will verify this server side too
		public long FileSize { get; set; }

	}
}
