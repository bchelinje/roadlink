using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeC.OpenId.Connect.Features.Messages.Dtos;

/// <summary>
/// In-app chat messages between users (customers and drivers)
/// </summary>
[Table("ChatMessages")]
public class ChatMessage
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? ConversationId { get; set; } // FK to Conversations

    [Required]
    [MaxLength(100)]
    public required string SenderId { get; set; } // FK to AspNetUsers

    [Required]
    [MaxLength(100)]
    public required string SenderName { get; set; }

    [Required]
    [MaxLength(20)]
    public required string SenderType { get; set; } // customer, driver, admin

    [Required]
    [MaxLength(100)]
    public required string RecipientId { get; set; } // FK to AspNetUsers

    [Required]
    [MaxLength(100)]
    public required string RecipientName { get; set; }

    [Required]
    [MaxLength(20)]
    public required string RecipientType { get; set; } // customer, driver, admin

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public required string Message { get; set; }

    [MaxLength(20)]
    public string MessageType { get; set; } = "text"; // text, image, file, location, system

    // Related entities
    public Guid? JobId { get; set; } // If message is job-related

    // Message status
    public bool IsDelivered { get; set; } = false;
    public DateTime? DeliveredAt { get; set; }

    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Attachments (JSON object with url, type, name, size)
    [Column(TypeName = "nvarchar(max)")]
    public string? Attachment { get; set; }

    // Location data for location messages (JSON)
    [Column(TypeName = "nvarchar(max)")]
    public string? LocationData { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ConversationId))]
    public virtual Conversation? Conversation { get; set; }
}
