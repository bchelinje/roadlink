using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeC.OpenId.Connect.Features.Support.Dtos;

/// <summary>
/// Messages/replies within a support ticket
/// </summary>
[Table("TicketMessages")]
public class TicketMessage
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TicketId { get; set; } // FK to SupportTickets

    [Required]
    [MaxLength(100)]
    public required string SenderId { get; set; } // FK to AspNetUsers

    [Required]
    [MaxLength(100)]
    public required string SenderName { get; set; }

    [Required]
    [MaxLength(20)]
    public required string SenderType { get; set; } // customer, support_agent, system

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public required string Message { get; set; }

    // Attachments (JSON array)
    [Column(TypeName = "nvarchar(max)")]
    public string? Attachments { get; set; }

    public bool IsInternal { get; set; } = false; // Internal messages only visible to support team

    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(TicketId))]
    public virtual SupportTicket? Ticket { get; set; }
}
