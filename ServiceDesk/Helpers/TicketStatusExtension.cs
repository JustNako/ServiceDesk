using ServiceDesk.Entities;

public static class TicketStatusExtensions
{
    public static string GetBadgeClass(this TicketStatus status)
    {
        return status switch
        {
            TicketStatus.Created => "bg-primary",
            TicketStatus.InProgress => "bg-warning text-dark",
            TicketStatus.OnHold => "bg-secondary",
            TicketStatus.Closed => "bg-success",
            _ => "bg-light"
        };
    }
}
