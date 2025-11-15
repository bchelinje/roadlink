namespace BeC.OpenId.Connect.Features.Vehicles.Models;

/// <summary>
/// Model for updating vehicle status
/// </summary>
public class UpdateVehicleStatusModel
{
    public required string Status { get; set; } // active, inactive, maintenance, retired
}
