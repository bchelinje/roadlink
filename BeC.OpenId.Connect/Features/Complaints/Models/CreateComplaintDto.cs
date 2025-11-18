using System.ComponentModel.DataAnnotations;

namespace BeC.OpenId.Connect.Features.Complaints.Models;

public class CreateComplaintDto
{
    [Required]
    [MaxLength(200)]
    public required string Title { get; set; }

    [Required]
    public required string Description { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Category { get; set; }

    public string Severity { get; set; } = "medium";

    public string? SubjectId { get; set; }
    public string? SubjectType { get; set; }

    public Guid? JobId { get; set; }
    public Guid? PaymentId { get; set; }

    public DateTime? IncidentDate { get; set; }
    public string? IncidentLocation { get; set; }

    public List<object>? Evidence { get; set; }
    public List<object>? Witnesses { get; set; }
}
