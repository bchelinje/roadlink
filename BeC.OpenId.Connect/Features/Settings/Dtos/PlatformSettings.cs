using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeC.OpenId.Connect.Features.Settings.Dtos;

/// <summary>
/// Platform-wide settings (Admin only)
/// </summary>
[Table("PlatformSettings")]
public class PlatformSettings
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public required string SettingKey { get; set; } // Unique setting identifier

    [Required]
    [MaxLength(255)]
    public required string SettingName { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? SettingValue { get; set; }

    [MaxLength(50)]
    public string? ValueType { get; set; } = "string"; // string, number, boolean, json

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? Category { get; set; } // general, payment, email, maps, etc.

    public bool IsPublic { get; set; } = false; // Can be accessed by non-admin users

    public bool IsEditable { get; set; } = true; // Can be edited via UI

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? UpdatedBy { get; set; } // User ID who last updated
}
