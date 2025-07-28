using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ServiceDesk.Entities
{

    public enum TicketStatus
    {
        [Description("Создан")]        
        Created,
        [Description("В процессе")]
        InProgress,
        [Description("В паузе")]
        OnHold,
        [Description("Закрыт")]
        Closed,
        
    }
    public class TicketModels
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        //T-00001


        [System.Text.Json.Serialization.JsonIgnore]
        public string TicketNumber{ get; set; } = string.Empty;

        public string? ContactPhonesJson { get; set; }

        

        public TicketStatus Status { get; set; } = TicketStatus.Created;

        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsDeleted { get; set; } = false;
        public ComplainType ComplainType { get; set; }

        [RequiredForApi(ErrorMessage = "RemoteUrl обязательно для API-запросов")]
        public string? RemoteUrl { get; set; } 


    }
}


public class RequiredForApiAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        var httpContextAccessor = (IHttpContextAccessor)validationContext.GetService(typeof(IHttpContextAccessor))!;
        var path = httpContextAccessor.HttpContext?.Request.Path.Value;

        // Если это вызов из API (например, начинается с /api/)
        if (path != null && path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return new ValidationResult(ErrorMessage ?? "Поле обязательно для API-запросов");
            }
        }

        return ValidationResult.Success!;
    }
}

