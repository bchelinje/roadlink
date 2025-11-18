using System.ComponentModel.DataAnnotations;

namespace BeC.OpenId.Connect.Features.Registration.Dtos;

/// <summary>
/// Customer self-registration request
/// </summary>
public class CustomerRegistrationRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [MinLength(8)]
    public required string Password { get; set; }

    [Required]
    [MaxLength(100)]
    public required string FirstName { get; set; }

    [Required]
    [MaxLength(100)]
    public required string LastName { get; set; }

    [Required]
    [Phone]
    public required string Phone { get; set; }

    [MaxLength(20)]
    public string CustomerType { get; set; } = "individual"; // individual, business

    public string? CompanyName { get; set; }

    public AddressDto? PrimaryAddress { get; set; }

    public bool AcceptedTerms { get; set; } = false;
    public bool AcceptedPrivacyPolicy { get; set; } = false;
}

/// <summary>
/// Driver self-registration request with KYC
/// </summary>
public class DriverRegistrationRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [MinLength(8)]
    public required string Password { get; set; }

    [Required]
    [MaxLength(100)]
    public required string FirstName { get; set; }

    [Required]
    [MaxLength(100)]
    public required string LastName { get; set; }

    [Required]
    [Phone]
    public required string Phone { get; set; }

    // UK-Specific KYC Information
    [Required]
    [MaxLength(50)]
    public required string LicenseNumber { get; set; }

    [Required]
    public DateTime LicenseExpiry { get; set; }

    [MaxLength(50)]
    public string? DrivingLicenseType { get; set; } = "Full UK";

    [MaxLength(20)]
    public string? NationalInsuranceNumber { get; set; }

    public DateTime? RightToWorkExpiry { get; set; }

    public required AddressDto Address { get; set; }

    public EmergencyContactDto? EmergencyContact { get; set; }

    public bool AcceptedTerms { get; set; } = false;
    public bool AcceptedPrivacyPolicy { get; set; } = false;
    public bool ConsentBackgroundCheck { get; set; } = false;
}

/// <summary>
/// Vehicle registration request
/// </summary>
public class VehicleRegistrationRequest
{
    [Required]
    public required string Type { get; set; } // small_van, luton_van, 7.5_tonne_truck

    [Required]
    public required string Make { get; set; }

    [Required]
    public required string Model { get; set; }

    [Required]
    public int Year { get; set; }

    [Required]
    public required string RegistrationNumber { get; set; }

    public string? VinNumber { get; set; }

    // Capacity
    public int CargoCapacity { get; set; }
    public decimal MaxPayloadWeight { get; set; }
    public decimal? CargoLength { get; set; }
    public decimal? CargoWidth { get; set; }
    public decimal? CargoHeight { get; set; }

    // UK Requirements
    [Required]
    public DateTime MOTExpiryDate { get; set; }
    public string? MOTCertificateNumber { get; set; }

    [Required]
    public DateTime InsuranceExpiry { get; set; }
    [Required]
    public required string InsuranceProvider { get; set; }
    [Required]
    public required string InsurancePolicyNumber { get; set; }
    public string? InsuranceType { get; set; }

    [Required]
    public DateTime RoadTaxExpiryDate { get; set; }

    public bool HireAndRewardInsurance { get; set; }
    public bool GoodsInTransitInsurance { get; set; }
    public bool PublicLiabilityInsurance { get; set; }
    public DateTime? PublicLiabilityExpiry { get; set; }

    public List<string>? Features { get; set; }
}

/// <summary>
/// Document upload request
/// </summary>
public class DocumentUploadRequest
{
    [Required]
    public required string Type { get; set; }

    [Required]
    public required string FileName { get; set; }

    [Required]
    public required string FileUrl { get; set; } // URL after upload to cloud storage

    public string? FileSize { get; set; }
    public string? MimeType { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? DocumentNumber { get; set; }
    public DateTime? IssueDate { get; set; }
    public string? IssuingAuthority { get; set; }
    public Guid? VehicleId { get; set; }
}

/// <summary>
/// Registration response
/// </summary>
public class RegistrationResponse
{
    public required string UserId { get; set; }
    public required string Email { get; set; }
    public string? CustomerId { get; set; }
    public string? DriverId { get; set; }
    public required string ApprovalStatus { get; set; }
    public required string Message { get; set; }
    public bool EmailVerificationRequired { get; set; } = true;
}

/// <summary>
/// Address DTO
/// </summary>
public class AddressDto
{
    public required string AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public required string City { get; set; }
    public string? County { get; set; }
    public required string PostalCode { get; set; }
    public string Country { get; set; } = "United Kingdom";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

/// <summary>
/// Emergency contact DTO
/// </summary>
public class EmergencyContactDto
{
    public required string Name { get; set; }
    public required string Relationship { get; set; }
    public required string Phone { get; set; }
    public string? Email { get; set; }
}
