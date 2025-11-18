using System.ComponentModel.DataAnnotations;

namespace BeC.OpenId.Connect.Features.DataPrivacy.Models;

public class ReviewDeletionRequestDto
{
    [Required]
    public required string Decision { get; set; } // approve, reject

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [MaxLength(1000)]
    public string? RejectionReason { get; set; }

    public DateTime? ScheduledDeletionDate { get; set; }
}
