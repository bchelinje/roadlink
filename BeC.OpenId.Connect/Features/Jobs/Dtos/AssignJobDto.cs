using System.ComponentModel.DataAnnotations;

namespace BeC.OpenId.Connect.Features.Jobs.Dtos;

public class AssignJobDto
{
    [Required]
    public required Guid DriverId { get; set; }

    public string? InternalNotes { get; set; }
}
