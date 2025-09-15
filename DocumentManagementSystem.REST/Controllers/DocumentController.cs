using Microsoft.AspNetCore.Mvc;

namespace DocumentManagementSystem.REST.Controllers
{
	public class DocumentController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
