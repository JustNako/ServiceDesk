using Microsoft.AspNetCore.Mvc;

namespace ServiceDesk.Controllers
{
    public class RedirectController : Controller
    {
        public IActionResult ToAdmin()
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }
    }
}
