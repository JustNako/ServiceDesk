using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Data;
using ServiceDesk.Entities;
using ServiceDesk.ViewModels;
using System;

namespace ServiceDesk.Controllers.Api
{
    [ApiController]    
    [Route("api/[controller]")]
    [Authorize]
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
        public async Task<IActionResult> CreateTicket([FromBody] TicketModels model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Генерация номера заявки
            var lastTicket = _context.Tickets.OrderByDescending(t => t.Id).FirstOrDefault();
            int nextId = lastTicket?.Id + 1 ?? 1;
            model.TicketNumber = $"T-{nextId:D5}";
            model.CreatedAt = DateTime.Now;
            model.IsDeleted = false;

            _context.Tickets.Add(model);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, ticketId = model.Id });
        }

        [Authorize]
        [HttpPost]
        [Route("api/tickets/upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateFromApi([FromForm] TicketApiDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RemoteUrl))
                return BadRequest("RemoteUrl обязателен.");

            var ticket = new TicketModels
            {
                RemoteUrl = dto.RemoteUrl,
                CreatedAt = DateTime.Now
            };

            // Путь для сохранения
            var folder = Path.Combine(_env.WebRootPath, "uploads", "api", Guid.NewGuid().ToString());
            Directory.CreateDirectory(folder);

            var savedPaths = new List<string>();
            

            if (dto.Files != null && dto.Files.Any())
            {
                foreach (var file in dto.Files)
                {
                    if (file.Length > 0)
                    {
                        var path = Path.Combine(folder, file.FileName);
                        using var stream = new FileStream(path, FileMode.Create);
                        await file.CopyToAsync(stream);

                        savedPaths.Add($"/uploads/api/{Path.GetFileName(folder)}/{file.FileName}");
                    }

                    if (file.Length > 10 * 1024 * 1024)
                    {
                        return BadRequest($"Файл {file.FileName} превышает 10 МБ.");
                    }
                    if (dto.Files?.Count > 5)
                    {
                        return BadRequest("Можно загрузить не более 5 файлов.");
                    }

                }

                ticket.AttachmentPath = string.Join(";", savedPaths);
                ticket.AttachmentName = string.Join(";", dto.Files.Select(f => f.FileName));
            }

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            return Ok(ticket);
        }





    }



}
