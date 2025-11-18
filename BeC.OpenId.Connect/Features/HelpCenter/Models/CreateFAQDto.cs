using System.ComponentModel.DataAnnotations;

namespace BeC.OpenId.Connect.Features.HelpCenter.Models;

public class CreateFAQDto
{
    [Required]
    [MaxLength(200)]
    public required string Question { get; set; }

    [Required]
    public required string Answer { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Category { get; set; }

    public string? Subcategory { get; set; }
    public string TargetAudience { get; set; } = "all";
    public int DisplayOrder { get; set; } = 0;
    public bool IsPublished { get; set; } = true;
    public bool IsFeatured { get; set; } = false;
    public List<string>? Tags { get; set; }
    public List<string>? Keywords { get; set; }
    public List<Guid>? RelatedFAQs { get; set; }
}
