using System.ComponentModel.DataAnnotations;

namespace BeC.OpenId.Connect.Features.Complaints.Models;

public class UpdateComplaintDto
{
    public string? Status { get; set; }
    public string? Severity { get; set; }
    public string? AssignedToId { get; set; }
    public string? InvestigationNotes { get; set; }
    public string? Resolution { get; set; }
    public string? ResolutionType { get; set; }
    public List<string>? ActionsTaken { get; set; }
    public bool? IsEscalated { get; set; }
    public string? EscalationReason { get; set; }
}
