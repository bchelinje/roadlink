namespace BeC.OpenId.Connect.Features.Vehicles.Models;

/// <summary>
/// Model for updating an existing vehicle
/// </summary>
public class UpdateVehicleModel
{
    public string? Type { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? VinNumber { get; set; }
    public int? CargoCapacity { get; set; }
    public decimal? MaxPayloadWeight { get; set; }
    public decimal? MaxGrossWeight { get; set; }
    public decimal? CargoLength { get; set; }
    public decimal? CargoWidth { get; set; }
    public decimal? CargoHeight { get; set; }
    public List<string>? Features { get; set; }
    public bool? HasInsurance { get; set; }
    public DateTime? InsuranceExpiry { get; set; }
    public DateTime? LastInspectionDate { get; set; }
    public DateTime? NextInspectionDue { get; set; }
    public int? Mileage { get; set; }
    public List<string>? Photos { get; set; }
}
