using System.ComponentModel.DataAnnotations;

namespace BeC.OpenId.Connect.Features.Support.Models;

public class CreateTicketDto
{
    [Required]
    [MaxLength(200)]
    public required string Subject { get; set; }

    [Required]
    public required string Description { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Category { get; set; } // payment, job, driver, technical, account, other

    public string Priority { get; set; } = "medium";

    public Guid? JobId { get; set; }
    public Guid? DriverId { get; set; }
    public Guid? PaymentId { get; set; }

    public List<string>? Attachments { get; set; }
    public List<string>? Tags { get; set; }
}
