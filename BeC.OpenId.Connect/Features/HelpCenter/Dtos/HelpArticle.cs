using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeC.OpenId.Connect.Features.HelpCenter.Dtos;

/// <summary>
/// Detailed help articles and guides for the knowledge base
/// </summary>
[Table("HelpArticles")]
public class HelpArticle
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public required string Title { get; set; }

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public required string Content { get; set; } // Rich text/Markdown content

    [MaxLength(500)]
    public string? Summary { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Category { get; set; }

    [MaxLength(100)]
    public string? Subcategory { get; set; }

    // Target audience
    [Required]
    [MaxLength(20)]
    public string TargetAudience { get; set; } = "all"; // all, customer, driver, admin

    // Article type
    [MaxLength(30)]
    public string ArticleType { get; set; } = "guide"; // guide, tutorial, troubleshooting, policy, announcement

    // Display
    public int DisplayOrder { get; set; } = 0;
    public bool IsPublished { get; set; } = true;
    public bool IsFeatured { get; set; } = false;

    // SEO
    [Required]
    [MaxLength(200)]
    public required string Slug { get; set; }

    [MaxLength(500)]
    public string? MetaDescription { get; set; }

    // Tags and keywords (JSON arrays)
    [Column(TypeName = "nvarchar(max)")]
    public string? Tags { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? Keywords { get; set; }

    // Related content (JSON array of article IDs)
    [Column(TypeName = "nvarchar(max)")]
    public string? RelatedArticles { get; set; }

    // Media attachments (JSON array)
    [Column(TypeName = "nvarchar(max)")]
    public string? Attachments { get; set; }

    [MaxLength(500)]
    public string? FeaturedImage { get; set; }

    [MaxLength(500)]
    public string? VideoUrl { get; set; }

    // Estimated reading time in minutes
    public int? EstimatedReadingTime { get; set; }

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
    public DateTime? PublishedAt { get; set; }
}
