using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Entities;

namespace ServiceDesk.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<AppUser> _userManager;

        public UsersController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }


        
        // Показать список всех пользователей
        public IActionResult Index()
        {
            var users = _userManager.Users
                .Where(u=> !u.IsDeleted)
                .ToList();
            return View(users);
        }

        // GET: Create user form
        public IActionResult Create()
        {
            return View(new AppUser());
        }

        // POST: Create user
        [HttpPost]
        public async Task<IActionResult> Create(AppUser model, string password)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new AppUser
            {
                UserName = model.UserName,
                FullName = model.FullName,
                ContactPhones = model.ContactPhones
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
                return RedirectToAction("Index");

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // GET: Delete confirm
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: Delete
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (user.IsSystemAdmin)
            {
                TempData["Error"] = "Системного администратора удалить нельзя.";
                return RedirectToAction("Index");
            }

            user.IsDeleted = true;
            await _userManager.UpdateAsync(user);
            return RedirectToAction("Index");
        }


        // GET: Details
        public async Task<IActionResult> Details(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }


    }
}
