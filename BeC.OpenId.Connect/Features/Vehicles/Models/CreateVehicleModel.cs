namespace BeC.OpenId.Connect.Features.Vehicles.Models;

/// <summary>
/// Model for creating a new vehicle
/// </summary>
public class CreateVehicleModel
{
    public Guid? DriverId { get; set; } // Only for admin use
    public required string Type { get; set; }
    public required string Make { get; set; }
    public required string Model { get; set; }
    public required int Year { get; set; }
    public required string RegistrationNumber { get; set; }
    public string? VinNumber { get; set; }
    public required int CargoCapacity { get; set; }
    public required decimal MaxPayloadWeight { get; set; }
    public required decimal MaxGrossWeight { get; set; }
    public decimal? CargoLength { get; set; }
    public decimal? CargoWidth { get; set; }
    public decimal? CargoHeight { get; set; }
    public List<string>? Features { get; set; }
    public required bool HasInsurance { get; set; }
    public DateTime? InsuranceExpiry { get; set; }
    public DateTime? LastInspectionDate { get; set; }
    public DateTime? NextInspectionDue { get; set; }
    public int? Mileage { get; set; }
    public List<string>? Photos { get; set; }
}
