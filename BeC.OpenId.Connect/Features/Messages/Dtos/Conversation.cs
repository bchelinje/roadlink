using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeC.OpenId.Connect.Features.Messages.Dtos;

/// <summary>
/// Conversation thread between two users
/// </summary>
[Table("Conversations")]
public class Conversation
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public required string User1Id { get; set; } // FK to AspNetUsers

    [Required]
    [MaxLength(100)]
    public required string User1Name { get; set; }

    [Required]
    [MaxLength(20)]
    public required string User1Type { get; set; } // customer, driver, admin

    [Required]
    [MaxLength(100)]
    public required string User2Id { get; set; } // FK to AspNetUsers

    [Required]
    [MaxLength(100)]
    public required string User2Name { get; set; }

    [Required]
    [MaxLength(20)]
    public required string User2Type { get; set; } // customer, driver, admin

    // Related job if conversation is job-specific
    public Guid? JobId { get; set; }

    // Last message info for quick display
    [Column(TypeName = "nvarchar(max)")]
    public string? LastMessage { get; set; }

    public DateTime? LastMessageAt { get; set; }

    [MaxLength(100)]
    public string? LastMessageSenderId { get; set; }

    // Unread counts
    public int User1UnreadCount { get; set; } = 0;
    public int User2UnreadCount { get; set; } = 0;

    // Status
    [MaxLength(20)]
    public string Status { get; set; } = "active"; // active, archived, blocked

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
