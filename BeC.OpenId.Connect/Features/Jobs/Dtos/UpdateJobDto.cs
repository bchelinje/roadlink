using System.ComponentModel.DataAnnotations;

namespace BeC.OpenId.Connect.Features.Jobs.Dtos;

public class UpdateJobDto
{
    [MaxLength(255)]
    public string? CustomerName { get; set; }

    [Phone]
    [MaxLength(20)]
    public string? CustomerPhone { get; set; }

    [EmailAddress]
    [MaxLength(255)]
    public string? CustomerEmail { get; set; }

    [MaxLength(50)]
    public string? JobType { get; set; }

    [MaxLength(50)]
    public string? VehicleTypeRequired { get; set; }

    [MaxLength(20)]
    public string? Priority { get; set; }

    [MaxLength(20)]
    public string? Status { get; set; } // pending, assigned, confirmed, in_progress, completed, cancelled, on_hold

    public DateTime? ScheduledDate { get; set; }

    [MaxLength(10)]
    public string? ScheduledTime { get; set; }

    public int? EstimatedDuration { get; set; }

    public string? PickupLocation { get; set; }

    public string? DeliveryLocation { get; set; }

    public decimal? Distance { get; set; }

    public string? Items { get; set; }

    public string? SpecialInstructions { get; set; }

    public string? InternalNotes { get; set; }

    public string? CustomerNotes { get; set; }
}
