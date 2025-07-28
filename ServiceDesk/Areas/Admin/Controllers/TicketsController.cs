using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Data;
using ServiceDesk.Entities;
using ServiceDesk.ViewModels;
using System.Net.Mail;
using System.Text.Json;
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
        public async Task<IActionResult> Index(string subject, ComplainType? complaintType, TicketStatus? status, int page = 1)
        {
            int pageSize = 15;

            var query = _context.Tickets
                .Where(t => !t.IsDeleted);


            if (!string.IsNullOrEmpty(subject))
                query = query.Where(t => t.Subject.Contains(subject));


            if (complaintType.HasValue)
                query = query.Where(t => t.ComplainType == complaintType.Value);

            if (status.HasValue)
                query = query.Where(t => t.Status == status.Value);
            
            

            var complaintTypeCounts = await _context.Tickets
                .Where(t => !t.IsDeleted)
                .GroupBy(t => t.ComplainType)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
            

            var statusCounts = await _context.Tickets
                .Where(t => !t.IsDeleted)
                .GroupBy(t => t.Status)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            ViewBag.ComplaintTypeCounts = complaintTypeCounts;
            ViewBag.StatusCounts = statusCounts;


            var totalItems = await query.CountAsync();
            var tickets = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();  

            var model = new TicketFilterViewModel
            {
                Subject = subject,
                ComplaintType = complaintType,
                Status = status,
                Tickets = tickets,
                Page = page,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                PageSize = pageSize
            };

            return View(model);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(TicketModels model, List<IFormFile> attachment, List<string> contactPhones)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Generate ticket number
            var lastTicket = await _context.Tickets.OrderByDescending(t => t.Id).FirstOrDefaultAsync();
            int nextId = lastTicket?.Id + 1 ?? 1;
            model.TicketNumber = $"T-{nextId:D5}";

            if (contactPhones.Any(p => !Regex.IsMatch(p, @"^\+7 \(\d{3}\) \d{3}-\d{2}-\d{2}$")))
            {
                ModelState.AddModelError("", "Некорректный формат номера телефона.");
                return View(model);
            }

            string? attachmentPath = null;
            string? attachmentName = null;

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
                attachmentName = string.Join(";\n", savedNames);
                attachmentPath = string.Join(";\n", savedPaths);
            }
            model.CreatedAt = DateTime.Now;
            model.IsDeleted = false;
            model.RemoteUrl = model.RemoteUrl ?? "";
            model.ContactPhonesJson = JsonSerializer.Serialize(contactPhones);

            _context.Tickets.Add(model);
            await _context.SaveChangesAsync();

            // Обновим напрямую нужные поля
            if (attachmentPath != null || attachmentName != null)
            {
                model = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == model.Id);
                if (model != null)
                {
                    _context.Entry(model).Property("AttachmentName").CurrentValue = attachmentName;
                    _context.Entry(model).Property("AttachmentPath").CurrentValue = attachmentPath;
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction("Index");
        }







        public async Task<IActionResult> Details(int id)
        {
            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
            if (ticket == null) return NotFound();

            // Контактные номера
            var phones = string.IsNullOrEmpty(ticket.ContactPhonesJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(ticket.ContactPhonesJson) ?? new();
            ViewBag.ContactPhones = phones;

            // Получение файлов из папки /uploads/tickets/{id}/
            var folderPath = Path.Combine(_env.WebRootPath, "uploads", "tickets", id.ToString());
            var attachments = new List<string>();

            if (Directory.Exists(folderPath))
            {
                attachments = Directory.GetFiles(folderPath)
                    .Select(path => $"/uploads/tickets/{id}/{Path.GetFileName(path)}")
                    .ToList();
            }

            ViewBag.Attachments = attachments;

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

            // Получаем путь к файлам напрямую из базы
            var entry = _context.Entry(ticket);
            var attachmentPath = entry.Property<string>("AttachmentPath").CurrentValue;

            if (!string.IsNullOrEmpty(attachmentPath))
            {
                var paths = attachmentPath.Split(";\n", StringSplitOptions.RemoveEmptyEntries);
                foreach (var relativePath in paths)
                {
                    var filePath = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
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