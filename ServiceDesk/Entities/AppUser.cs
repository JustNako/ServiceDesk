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

        public bool IsDeleted { get; set; } = false;
        public bool IsSystemAdmin { get; set; } = false;


    }
}

