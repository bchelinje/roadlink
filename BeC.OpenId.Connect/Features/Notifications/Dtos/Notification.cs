using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeC.OpenId.Connect.Features.Notifications.Dtos;

[Table("Notifications")]
public class Notification
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // Recipient
    [Required]
    public required string UserId { get; set; } // FK to AspNetUsers

    // Notification Content
    [Required]
    [MaxLength(200)]
    public required string Title { get; set; }

    [Required]
    [MaxLength(1000)]
    public required string Message { get; set; }

    // Type and Category
    [Required]
    [MaxLength(50)]
    public required string Type { get; set; } // job, payment, system, review, account, alert

    [MaxLength(50)]
    public string? Category { get; set; } // job_assigned, job_completed, payment_received, etc.

    // Related Entity
    [MaxLength(50)]
    public string? EntityType { get; set; } // job, payment, review, driver, etc.

    public string? EntityId { get; set; }

    // Action/Link
    public string? ActionUrl { get; set; }

    [MaxLength(100)]
    public string? ActionText { get; set; }

    // Additional Data (JSON)
    [Column(TypeName = "nvarchar(max)")]
    public string? Data { get; set; }

    // Status
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }

    [Required]
    [MaxLength(20)]
    public string Priority { get; set; } = "normal"; // low, normal, high, urgent

    // Delivery
    public bool SendEmail { get; set; } = false;
    public bool EmailSent { get; set; } = false;
    public DateTime? EmailSentAt { get; set; }

    public bool SendPush { get; set; } = false;
    public bool PushSent { get; set; } = false;
    public DateTime? PushSentAt { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
}
