using ServiceDesk.Entities;
using System.ComponentModel.DataAnnotations;

public class TicketApiDto
{
    [Required]
    public string RemoteUrl { get; set; }
    public List<IFormFile>? Files { get; set; }

}
