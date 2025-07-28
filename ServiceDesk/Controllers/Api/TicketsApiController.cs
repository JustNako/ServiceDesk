using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Data;
using ServiceDesk.Entities;
using ServiceDesk.ViewModels;
using System;
using System.Linq;

namespace ServiceDesk.Controllers.Api
{
    [ApiController]    
    [Route("api/[controller]")]
    
    public class TicketsApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public TicketsApiController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }


        [Authorize]
        [HttpGet("{id}")]        
        public async Task<IActionResult> GetTicket(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null || ticket.IsDeleted) return NotFound();

            return Ok(ticket);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTicket([FromForm] TicketModels model, List<IFormFile>? files)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var lastTicket = _context.Tickets.OrderByDescending(t => t.Id).FirstOrDefault();
            int nextId = lastTicket?.Id + 1 ?? 1;
            model.TicketNumber = $"T-{nextId:D5}";
            model.CreatedAt = DateTime.Now;
            model.IsDeleted = false;

            List<string> savedNames = new();
            List<string> savedPaths = new();

            if (files != null && files.Any())
            {
                if (files.Count > 5)
                    return BadRequest("Максимум 5 файлов.");

                var folder = Path.Combine(_env.WebRootPath, "uploads", "tickets", $"remote_{nextId}");
                Directory.CreateDirectory(folder);

                var allowedExtensions = new[] { ".jpeg", ".jpg", ".png", ".pdf", ".zip", ".rar", ".doc", ".docx", ".xls", ".xlsx", ".txt" };

                foreach (var file in files)
                {
                    var ext = Path.GetExtension(file.FileName).ToLower();
                    if (!allowedExtensions.Contains(ext))
                        return BadRequest($"Файл запрещён: {file.FileName}");

                    if (file.Length > 10 * 1024 * 1024)
                        return BadRequest($"Файл слишком большой: {file.FileName}");

                    var path = Path.Combine(folder, file.FileName);
                    using var stream = new FileStream(path, FileMode.Create);
                    await file.CopyToAsync(stream);

                    savedNames.Add(file.FileName);
                    savedPaths.Add($"/uploads/tickets/remote_{nextId}/{file.FileName}");
                }
            }

            _context.Tickets.Add(model);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                model.Id,
                model.TicketNumber,
                model.RemoteUrl,
                Files = savedNames.Select((name, i) => new
                {
                    FileName = name,
                    Url = savedPaths[i]
                }).ToList()
            });
        }









    }



}
