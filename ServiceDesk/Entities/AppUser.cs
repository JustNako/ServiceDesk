using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ServiceDesk.Entities
{
    public class AppUser : IdentityUser
    {
        [Required(ErrorMessage = "Поле 'Имя' обязательно")]
        public string FullName { get; set; } = string.Empty;
        public string? ContactPhonesJson { get; set; }

        [NotMapped]
        [PhoneListValidation(ErrorMessage = "Неправильный формат номера")]
        public List<string> ContactPhones
        {
            get => string.IsNullOrEmpty(ContactPhonesJson)
                   ? new List<string>()
                   : JsonSerializer.Deserialize<List<string>>(ContactPhonesJson) ?? new();
            set => ContactPhonesJson = JsonSerializer.Serialize(value);
        }
    }
}

public class PhoneListValidationAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        var list = value as List<string>;
        if (list == null) return true;

        var phoneRegex = new Regex(@"^\+7 \(\d{3}\) \d{3}-\d{2}-\d{2}$");

        foreach (var phone in list)
        {
            if (!string.IsNullOrWhiteSpace(phone) && !phoneRegex.IsMatch(phone))
            {
                return false;
            }
        }

        return true;
    }
}