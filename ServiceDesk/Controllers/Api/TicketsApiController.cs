using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Data;
using ServiceDesk.Entities;
using ServiceDesk.Helpers;
using ServiceDesk.ViewModels;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;

namespace ServiceDesk.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]

    public class TicketsApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<TicketsApiController> _logger;
        private static readonly ActivitySource activitySource = new("ServiceDesk");

        public TicketsApiController(AppDbContext context, IWebHostEnvironment env, ILogger<TicketsApiController> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
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
        public async Task<IActionResult> CreateTicket([FromForm] TicketApiDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var lastTicket = await _context.Tickets.OrderByDescending(t => t.Id).FirstOrDefaultAsync();
            int nextId = lastTicket?.Id + 1 ?? 1;

            // Создание модели заявки
            var model = new TicketModels
            {
                Subject = dto.Subject,
                Description = dto.Description,
                ContactPhonesJson = JsonSerializer.Serialize(dto.ContactPhones),
                RemoteUrl = dto.RemoteUrl,
                ComplainType = dto.ComplainType,
                TicketNumber = $"T-{nextId:D5}",
                CreatedAt = DateTime.Now,
                IsDeleted = false,
                Status = TicketStatus.Created
            };

            _context.Tickets.Add(model);
            await _context.SaveChangesAsync(); // Нужно, чтобы получить model.Id

            string? attachmentName = null;
            string? attachmentPath = null;

            if (dto.Files != null && dto.Files.Any())
            {
                if (dto.Files.Count > 5)
                    return BadRequest("Максимум 5 файлов.");

                var folder = Path.Combine(_env.WebRootPath, "uploads", "tickets", $"remote_{model.Id}");
                Directory.CreateDirectory(folder);

                var allowedExtensions = new[] { ".jpeg", ".jpg", ".png", ".pdf", ".zip", ".rar", ".doc", ".docx", ".xls", ".xlsx", ".txt" };

                var savedNames = new List<string>();
                var savedPaths = new List<string>();

                foreach (var file in dto.Files)
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
                    savedPaths.Add($"/uploads/tickets/remote_{model.Id}/{file.FileName}");
                }

                attachmentName = string.Join(";\n", savedNames);
                attachmentPath = string.Join(";\n", savedPaths);
            }

            // Обновление скрытых полей
            if (attachmentName != null || attachmentPath != null)
            {
                _context.Entry(model).Property("AttachmentName").CurrentValue = attachmentName;
                _context.Entry(model).Property("AttachmentPath").CurrentValue = attachmentPath;
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                model.Id,
                model.TicketNumber,
                model.RemoteUrl,
                model.ComplainType,
                Files = dto.Files?.Select(f => f.FileName).ToList()
            });
        }


        [HttpGet("complain-types")]
        public IActionResult GetComplainTypes()
        {
            var types = Enum.GetValues(typeof(ComplainType))
                .Cast<ComplainType>()
                .Select(ct => new
                {
                    Value = (int)ct,
                    Name = ct.ToString(),
                    Description = ct.GetDescription()
                });

            return Ok(types);
        }


        [HttpGet("test-loki")]
        public IActionResult Test()
        {
            using var activity = activitySource.StartActivity("Test Trace");

            var traceId = Activity.Current?.TraceId.ToString();
            var spanId = Activity.Current?.SpanId.ToString();

            _logger.LogInformation("Test log with TraceId: {TraceId} and SpanId: {SpanId}", traceId, spanId);

            return Ok(new { message = "Logged and Traced", traceId, spanId });
        }




    }



}
