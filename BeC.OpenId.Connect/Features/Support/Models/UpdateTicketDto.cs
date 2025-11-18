using System.ComponentModel.DataAnnotations;

namespace BeC.OpenId.Connect.Features.Support.Models;

public class UpdateTicketDto
{
    [MaxLength(20)]
    public string? Status { get; set; }

    [MaxLength(20)]
    public string? Priority { get; set; }

    public string? AssignedToId { get; set; }
    public string? Resolution { get; set; }
    public string? InternalNotes { get; set; }
}
