using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeC.OpenId.Connect.Features.Messages.Dtos;

/// <summary>
/// Message entity for in-app messaging between customers and drivers
/// </summary>
[Table("Messages")]
public class Message
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid JobId { get; set; }

    [Required]
    public required string SenderId { get; set; }

    [MaxLength(100)]
    public string? SenderName { get; set; }

    [Required]
    [MaxLength(20)]
    public required string SenderType { get; set; } // customer, driver

    [Required]
    public required string ReceiverId { get; set; }

    [MaxLength(100)]
    public string? ReceiverName { get; set; }

    [Required]
    [MaxLength(20)]
    public required string ReceiverType { get; set; } // customer, driver

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public required string Content { get; set; }

    [MaxLength(20)]
    public string MessageType { get; set; } = "text"; // text, image, location, system

    // Attachments (JSON array)
    [Column(TypeName = "nvarchar(max)")]
    public string? Attachments { get; set; }

    // Read status
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }

    // Message status
    [MaxLength(20)]
    public string Status { get; set; } = "sent"; // sent, delivered, read, failed

    // System message flag
    public bool IsSystemMessage { get; set; } = false;

    // Metadata (JSON)
    [Column(TypeName = "nvarchar(max)")]
    public string? Metadata { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
