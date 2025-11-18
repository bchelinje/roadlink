using System.ComponentModel.DataAnnotations;

namespace BeC.OpenId.Connect.Features.DataPrivacy.Models;

public class RequestDataDeletionDto
{
    [Required]
    [MinLength(10)]
    [MaxLength(2000)]
    public required string Reason { get; set; }

    public string DeletionType { get; set; } = "soft"; // soft (anonymize), hard (complete removal)

    public bool RequestDataExport { get; set; } = true;

    public int? GracePeriodDays { get; set; } = 30;
}
