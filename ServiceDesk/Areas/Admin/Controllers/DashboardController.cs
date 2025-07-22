using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ServiceDesk.Entities;
using ServiceDesk.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ServiceDesk.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public DashboardController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var ticketCount = await _context.Tickets.CountAsync(t => !t.IsDeleted);
            var userCount = await _userManager.Users.CountAsync(u => !u.IsDeleted);

            ViewBag.TicketCount = ticketCount;
            ViewBag.UserCount = userCount;

            return View();
        }
    }
}
