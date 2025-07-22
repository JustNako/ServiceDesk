using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Data;
using ServiceDesk.Entities;
using System.Net.Mail;
using System.Text.RegularExpressions;
using ServiceDesk.ViewModels;




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
        public IActionResult Index(string? subject, ComplainType? complaintType, TicketStatus? status)
        {
            var query = _context.Tickets.Where(t => !t.IsDeleted);

            if (!string.IsNullOrEmpty(subject))
                query = query.Where(t => t.Subject.Contains(subject));

            if (complaintType.HasValue)
                query = query.Where(t => t.ComplainType == complaintType.Value);

            if (status.HasValue)
                query = query.Where(t => t.Status == status.Value);

            var model = new TicketFilterViewModel
            {
                Subject = subject,
                ComplaintType = complaintType,
                Status = status,
                Tickets = query.OrderByDescending(t => t.CreatedAt).ToList()
            };

            return View(model);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(TicketModels model, List<IFormFile> attachment)
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

            if (attachment != null && attachment.Any())
            {
                if (attachment.Count > 5)
                {
                    ModelState.AddModelError("", "Можно прикрепить максимум 5 файлов.");
                    return View(model);
                }

                var allowedExtensions = new[] { ".jpeg", ".jpg", ".png", ".pdf", ".zip", ".rar", ".doc", ".docx", ".xls", ".xlsx", ".txt" };
                var folder = Path.Combine(_env.WebRootPath, "uploads", "tickets", nextId.ToString());

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var savedNames = new List<string>();
                var savedPaths = new List<string>();

                foreach (var file in attachment)
                {
                    var extension = Path.GetExtension(file.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("", $"Недопустимый формат файла: {file.FileName}");
                        return View(model);
                    }

                    if (file.Length > 10 * 1024 * 1024)
                    {
                        ModelState.AddModelError("", $"Размер файла '{file.FileName}' превышает 10 МБ.");
                        return View(model);
                    }

                    var filePath = Path.Combine(folder, file.FileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    savedNames.Add(file.FileName);
                    savedPaths.Add($"/uploads/tickets/{nextId}/{file.FileName}");

                }

                model.AttachmentName = string.Join(" ; " +
                    "\n", savedNames);
                model.AttachmentPath = string.Join(" ; " +
                    "\n", savedPaths);
            }
            model.CreatedAt = DateTime.Now;
            model.IsDeleted = false;

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

            ticket.IsDeleted = true; // Soft delete
            _context.Tickets.Update(ticket);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
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