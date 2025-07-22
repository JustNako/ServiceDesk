namespace ServiceDesk.Models
{
    public class TicketFilterViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
