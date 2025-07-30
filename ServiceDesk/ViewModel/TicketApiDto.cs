using ServiceDesk.Entities;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

public class TicketApiDto
{
    [Required, MaxLength(50)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public ComplainType ComplainType { get; set; }

    public List<string> ContactPhones { get; set; } = new();

    [Required(ErrorMessage = "RemoteUrl обязательно для API")]
    public string RemoteUrl { get; set; }

    [MaxFileCount(5, ErrorMessage = "Можно загрузить максимум 5 файлов")]
    public List<IFormFile>? Files { get; set; }

}

public class MaxFileCountAttribute : ValidationAttribute
{
    private readonly int _max;

    public MaxFileCountAttribute(int max)
    {
        _max = max;
    }

    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value is List<IFormFile> files)
        {
            if (files.Count > _max)
                return new ValidationResult(ErrorMessage ?? $"Максимум {_max} файлов разрешено.");
        }

        return ValidationResult.Success!;
    }
}
