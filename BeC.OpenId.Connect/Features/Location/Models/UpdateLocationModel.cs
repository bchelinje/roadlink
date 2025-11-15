using System.ComponentModel.DataAnnotations;

namespace BeC.OpenId.Connect.Features.Location.Models;

/// <summary>
/// Model for updating driver location
/// </summary>
public class UpdateLocationModel
{
    [Required]
    public double Latitude { get; set; }

    [Required]
    public double Longitude { get; set; }

    public double? Accuracy { get; set; }

    public double? Speed { get; set; }

    public double? Heading { get; set; }

    public Guid? CurrentJobId { get; set; }
}
