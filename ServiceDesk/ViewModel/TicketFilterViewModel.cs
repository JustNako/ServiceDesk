using ServiceDesk.Entities;

namespace ServiceDesk.ViewModels
{
    public class TicketFilterViewModel
    {
        public string? Subject { get; set; }
        public ComplainType? ComplaintType { get; set; }
        public TicketStatus? Status { get; set; }

        public List<TicketModels> Tickets { get; set; } = new();

        public int Page { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;

    }
}
