namespace BeC.OpenId.Connect.Features.Vehicles.ViewModels;

/// <summary>
/// View model for vehicle response
/// </summary>
public class VehicleViewModel
{
    public Guid Id { get; set; }
    public Guid DriverId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public string? VinNumber { get; set; }
    public int CargoCapacity { get; set; }
    public decimal MaxPayloadWeight { get; set; }
    public decimal MaxGrossWeight { get; set; }
    public decimal? CargoLength { get; set; }
    public decimal? CargoWidth { get; set; }
    public decimal? CargoHeight { get; set; }
    public List<string>? Features { get; set; }
    public bool HasInsurance { get; set; }
    public DateTime? InsuranceExpiry { get; set; }
    public string Status { get; set; } = "active";
    public DateTime? LastInspectionDate { get; set; }
    public DateTime? NextInspectionDue { get; set; }
    public int? Mileage { get; set; }
    public List<string>? Photos { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Driver information (when included)
    public DriverInfo? Driver { get; set; }
}

/// <summary>
/// Nested driver information in vehicle response
/// </summary>
public class DriverInfo
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
}
