using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Text.Json;

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
        public string TicketNumber{ get; set; } = string.Empty;

        public string? ContactPhonesJson { get; set; }

        [NotMapped]
        public List<string> ContactPhones
        {
            get => string.IsNullOrEmpty(ContactPhonesJson)
                   ? new List<string>()
                   : JsonSerializer.Deserialize<List<string>>(ContactPhonesJson) ?? new();
            set => ContactPhonesJson = JsonSerializer.Serialize(value);
        }

        public TicketStatus Status { get; set; } = TicketStatus.Created;

        public string? AttachmentPath { get; set; }
        public string? AttachmentName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;



    }
}
