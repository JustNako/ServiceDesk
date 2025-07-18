using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Data;
using ServiceDesk.Entities;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace ServiceDesk.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class TicketsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        public TicketsController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }
        public async Task<IActionResult> Index()
        {
            var tickets = await _context.Tickets.ToListAsync();
            return View(tickets);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(TicketModels model, IFormFile? attachment)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Generate ticket number
            var lastTicket = await _context.Tickets.OrderByDescending(t => t.Id).FirstOrDefaultAsync();
            int nextId = lastTicket?.Id + 1 ?? 1;
            model.TicketNumber = $"T-{nextId:D5}";

            if (model.ContactPhones.Any(p => !Regex.IsMatch(p, @"^\+7 \(\d{3}\) \d{3}-\d{2}-\d{2}$")))
            {
                ModelState.AddModelError("", "Некорректный формат номера телефона.");
                return View(model);
            }

            if (attachment != null && attachment.Length > 0)
            {
                var allowedExtensions = new[] { ".jpeg", ".jpg", ".png", ".pdf", ".zip", ".rar", ".doc", ".docx", ".xls", ".xlsx", ".txt" };
                var fileExtension = Path.GetExtension(attachment.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("", "Недопустимый формат файла.");
                    return View(model);
                }

                if (attachment.Length > 10 * 1024 * 1024)
                {
                    ModelState.AddModelError("", "Размер файла не должен превышать 10 МБ.");
                    return View(model);
                }

                var folder = Path.Combine(_env.WebRootPath, "uploads", "tickets", nextId.ToString());
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                var filePath = Path.Combine(folder, attachment.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await attachment.CopyToAsync(stream);
                }

                model.AttachmentName = attachment.FileName;
                model.AttachmentPath = $"/uploads/tickets/{nextId}/{attachment.FileName}";

            }



            _context.Tickets.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");

        }


        public async Task<IActionResult> Details(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();
            return View(ticket);
        }



        public async Task<IActionResult> Delete(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            return View(ticket);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            if (!string.IsNullOrEmpty(ticket.AttachmentPath))
            {
                var filePath = Path.Combine(_env.WebRootPath, ticket.AttachmentPath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details));
        }

        public async Task<IActionResult> EditStatus(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();
            return View(ticket);
        }

        [HttpPost]
        public async Task<IActionResult> EditStatus(int id, TicketStatus status)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            ticket.Status = status;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new {id = ticket.Id});
        }

    }


}