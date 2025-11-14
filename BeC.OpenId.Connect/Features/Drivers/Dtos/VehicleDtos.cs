using System.ComponentModel.DataAnnotations;

namespace BeC.OpenId.Connect.Features.Drivers.Dtos;

// Response DTO for Vehicle
public class VehicleDto
{
    public Guid Id { get; set; }
    public Guid DriverId { get; set; }

    public string Type { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public string? VinNumber { get; set; }

    public int? CargoCapacity { get; set; }
    public decimal? MaxPayloadWeight { get; set; }
    public decimal? MaxGrossWeight { get; set; }
    public decimal? CargoLength { get; set; }
    public decimal? CargoWidth { get; set; }
    public decimal? CargoHeight { get; set; }

    public string? Features { get; set; }
    public bool HasInsurance { get; set; }
    public DateTime? InsuranceExpiry { get; set; }

    public string Status { get; set; } = "active";
    public DateTime? LastInspectionDate { get; set; }
    public DateTime? NextInspectionDue { get; set; }
    public int? Mileage { get; set; }
    public string? Photos { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// DTO for creating a new vehicle
public class CreateVehicleDto
{
    [Required]
    [MaxLength(50)]
    public required string Type { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Make { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Model { get; set; }

    [Required]
    [Range(1900, 2100)]
    public int Year { get; set; }

    [Required]
    [MaxLength(50)]
    public required string RegistrationNumber { get; set; }

    [MaxLength(50)]
    public string? VinNumber { get; set; }

    public int? CargoCapacity { get; set; }
    public decimal? MaxPayloadWeight { get; set; }
    public decimal? MaxGrossWeight { get; set; }
    public decimal? CargoLength { get; set; }
    public decimal? CargoWidth { get; set; }
    public decimal? CargoHeight { get; set; }

    public string? Features { get; set; }
    public bool HasInsurance { get; set; }
    public DateTime? InsuranceExpiry { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "active";

    public DateTime? LastInspectionDate { get; set; }
    public DateTime? NextInspectionDue { get; set; }
    public int? Mileage { get; set; }
    public string? Photos { get; set; }

    public bool IsActive { get; set; } = true;
}

// DTO for updating an existing vehicle
public class UpdateVehicleDto
{
    [MaxLength(50)]
    public string? Type { get; set; }

    [MaxLength(100)]
    public string? Make { get; set; }

    [MaxLength(100)]
    public string? Model { get; set; }

    [Range(1900, 2100)]
    public int? Year { get; set; }

    [MaxLength(50)]
    public string? RegistrationNumber { get; set; }

    [MaxLength(50)]
    public string? VinNumber { get; set; }

    public int? CargoCapacity { get; set; }
    public decimal? MaxPayloadWeight { get; set; }
    public decimal? MaxGrossWeight { get; set; }
    public decimal? CargoLength { get; set; }
    public decimal? CargoWidth { get; set; }
    public decimal? CargoHeight { get; set; }

    public string? Features { get; set; }
    public bool? HasInsurance { get; set; }
    public DateTime? InsuranceExpiry { get; set; }

    [MaxLength(20)]
    public string? Status { get; set; }

    public DateTime? LastInspectionDate { get; set; }
    public DateTime? NextInspectionDue { get; set; }
    public int? Mileage { get; set; }
    public string? Photos { get; set; }

    public bool? IsActive { get; set; }
}
