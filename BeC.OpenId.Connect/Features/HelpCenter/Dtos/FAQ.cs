using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeC.OpenId.Connect.Features.HelpCenter.Dtos;

/// <summary>
/// Frequently Asked Questions for self-service help
/// </summary>
[Table("FAQs")]
public class FAQ
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public required string Question { get; set; }

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public required string Answer { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Category { get; set; } // getting_started, jobs, payments, account,
                                                    // driver, customer, safety, technical, policies

    [MaxLength(100)]
    public string? Subcategory { get; set; }

    // Target audience
    [Required]
    [MaxLength(20)]
    public string TargetAudience { get; set; } = "all"; // all, customer, driver, admin

    // Display order
    public int DisplayOrder { get; set; } = 0;

    // Status
    public bool IsPublished { get; set; } = true;
    public bool IsFeatured { get; set; } = false;

    // SEO
    [MaxLength(200)]
    public string? Slug { get; set; } // URL-friendly identifier

    [MaxLength(500)]
    public string? MetaDescription { get; set; }

    // Tags for search (JSON array)
    [Column(TypeName = "nvarchar(max)")]
    public string? Tags { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? Keywords { get; set; }

    // Related FAQs (JSON array of FAQ IDs)
    [Column(TypeName = "nvarchar(max)")]
    public string? RelatedFAQs { get; set; }

    // Attachments (JSON array - images, videos, PDFs)
    [Column(TypeName = "nvarchar(max)")]
    public string? Attachments { get; set; }

    // Analytics
    public int ViewCount { get; set; } = 0;
    public int HelpfulCount { get; set; } = 0;
    public int NotHelpfulCount { get; set; } = 0;
    public DateTime? LastViewedAt { get; set; }

    // Version control
    public int Version { get; set; } = 1;

    [MaxLength(100)]
    public string? LastEditedBy { get; set; }

    public DateTime? LastEditedAt { get; set; }

    // Audit
    [Required]
    [MaxLength(100)]
    public required string CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
