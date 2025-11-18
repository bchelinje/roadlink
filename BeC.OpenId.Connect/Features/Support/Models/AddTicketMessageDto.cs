using System.ComponentModel.DataAnnotations;

namespace BeC.OpenId.Connect.Features.Support.Models;

public class AddTicketMessageDto
{
    [Required]
    public required string Message { get; set; }

    public List<string>? Attachments { get; set; }
    public bool IsInternal { get; set; } = false;
}
