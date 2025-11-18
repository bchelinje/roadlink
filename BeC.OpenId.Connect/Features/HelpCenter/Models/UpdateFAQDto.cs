namespace BeC.OpenId.Connect.Features.HelpCenter.Models;

public class UpdateFAQDto
{
    public string? Question { get; set; }
    public string? Answer { get; set; }
    public string? Category { get; set; }
    public string? Subcategory { get; set; }
    public string? TargetAudience { get; set; }
    public int? DisplayOrder { get; set; }
    public bool? IsPublished { get; set; }
    public bool? IsFeatured { get; set; }
    public List<string>? Tags { get; set; }
    public List<string>? Keywords { get; set; }
}
