using ServiceDesk.Entities;

namespace ServiceDesk.ViewModels
{
    public class TicketFilterViewModel
    {
        public string? Subject { get; set; }
        public ComplainType? ComplaintType { get; set; }
        public TicketStatus? Status { get; set; }

        public List<TicketModels> Tickets { get; set; } = new();
    }
}
