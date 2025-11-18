using System.ComponentModel.DataAnnotations;

namespace BeC.OpenId.Connect.Features.HelpCenter.Models;

public class CreateArticleDto
{
    [Required]
    [MaxLength(200)]
    public required string Title { get; set; }

    [Required]
    public required string Content { get; set; }

    public string? Summary { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Category { get; set; }

    public string? Subcategory { get; set; }
    public string TargetAudience { get; set; } = "all";
    public string ArticleType { get; set; } = "guide";
    public int DisplayOrder { get; set; } = 0;
    public bool IsPublished { get; set; } = false;
    public bool IsFeatured { get; set; } = false;

    [Required]
    [MaxLength(200)]
    public required string Slug { get; set; }

    public string? MetaDescription { get; set; }
    public List<string>? Tags { get; set; }
    public List<string>? Keywords { get; set; }
    public string? FeaturedImage { get; set; }
    public string? VideoUrl { get; set; }
}
