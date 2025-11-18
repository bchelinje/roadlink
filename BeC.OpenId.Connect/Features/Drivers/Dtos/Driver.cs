
// Models/Driver.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeC.OpenId.Connect.Features.Drivers.Dtos;

[Table("Drivers")]
public class Driver
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public required string UserId { get; set; } // FK to AspNetUsers

    [Required]
    [MaxLength(100)]
    public required string FirstName { get; set; }

    [Required]
    [MaxLength(100)]
    public required string LastName { get; set; }

    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [MaxLength(20)]
    [Phone]
    public required string Phone { get; set; }

    // License Information
    [Required]
    [MaxLength(50)]
    public required string LicenseNumber { get; set; }

    [Required]
    public DateTime LicenseExpiry { get; set; }

    // Vehicle Information
    [MaxLength(50)]
    public string? VehicleType { get; set; }

    [MaxLength(50)]
    public string? VehicleRegistration { get; set; }

    // Approval and Vetting
    [Required]
    [MaxLength(20)]
    public string ApprovalStatus { get; set; } = "pending"; // pending, approved, rejected, suspended

    public string? ApprovedBy { get; set; } // FK to AspNetUsers (admin who approved)
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }

    // UK-Specific KYC Requirements
    [MaxLength(20)]
    public string? NationalInsuranceNumber { get; set; }

    [MaxLength(50)]
    public string? DrivingLicenseType { get; set; } // Full UK, International, etc.

    public bool BackgroundCheckCompleted { get; set; } = false;
    public DateTime? BackgroundCheckDate { get; set; }
    public string? BackgroundCheckReference { get; set; }

    public bool RightToWorkVerified { get; set; } = false;
    public DateTime? RightToWorkExpiry { get; set; }

    // Proof of address verification
    public bool ProofOfAddressVerified { get; set; } = false;
    public DateTime? ProofOfAddressDate { get; set; }

    // Status and Statistics
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "inactive"; // active, inactive, on_job, available, unavailable, suspended

    [Column(TypeName = "decimal(3,2)")]
    public decimal Rating { get; set; } = 0;

    public int TotalJobs { get; set; } = 0;
    public int CompletedJobs { get; set; } = 0;
    public int ActiveJobs { get; set; } = 0;

    // Dates
    public DateTime JoinedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastActiveDate { get; set; }

    // Profile
    public string? ProfileImage { get; set; }

    // JSON Fields for flexible data
    [Column(TypeName = "nvarchar(max)")]
    public string? Address { get; set; } // JSON

    [Column(TypeName = "nvarchar(max)")]
    public string? EmergencyContact { get; set; } // JSON

    [Column(TypeName = "nvarchar(max)")]
    public string? AvailabilitySchedule { get; set; } // JSON

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Admin/Vetting Notes
    [Column(TypeName = "nvarchar(max)")]
    public string? AdminNotes { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? VettingNotes { get; set; }

    // Navigation properties
    public virtual ICollection<Job> Jobs { get; set; } = new List<Job>();
    public virtual ICollection<DriverDocument> Documents { get; set; } = new List<DriverDocument>();
    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}

// Models/Job.cs
[Table("Jobs")]
public class Job
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public required string JobNumber { get; set; }

    // Customer Information
    [Required]
    public required string CustomerId { get; set; }

    [Required]
    [MaxLength(255)]
    public required string CustomerName { get; set; }

    [Required]
    [MaxLength(20)]
    public required string CustomerPhone { get; set; }

    [Required]
    [MaxLength(255)]
    public required string CustomerEmail { get; set; }

    // Driver Assignment
    public Guid? DriverId { get; set; }

    [ForeignKey(nameof(DriverId))]
    public virtual Driver? Driver { get; set; }

    [MaxLength(255)]
    public string? DriverName { get; set; }

    // Job Details
    [Required]
    [MaxLength(50)]
    public required string JobType { get; set; } // local_move, long_distance, commercial, etc.

    [MaxLength(50)]
    public string? VehicleTypeRequired { get; set; } // van, cargo_van, small_truck, large_truck, etc.

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "pending"; // pending, assigned, confirmed, in_progress, completed, cancelled, on_hold

    [Required]
    [MaxLength(20)]
    public string Priority { get; set; } = "normal"; // low, normal, high, urgent

    // Scheduling
    [Required]
    public DateTime ScheduledDate { get; set; }

    [Required]
    [MaxLength(10)]
    public required string ScheduledTime { get; set; } // HH:mm format

    public int EstimatedDuration { get; set; } // minutes

    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }

    // Locations (stored as JSON)
    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public required string PickupLocation { get; set; } // JSON

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public required string DeliveryLocation { get; set; } // JSON

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Distance { get; set; } // miles

    // Job Content
    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public required string Items { get; set; } // JSON array

    [Column(TypeName = "nvarchar(max)")]
    public string? SpecialInstructions { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? InternalNotes { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? CustomerNotes { get; set; }

    // Media and History
    [Column(TypeName = "nvarchar(max)")]
    public string? Photos { get; set; } // JSON array

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string StatusHistory { get; set; } = "[]"; // JSON array

    // Payment
    [Column(TypeName = "nvarchar(max)")]
    public string? Payment { get; set; } // JSON

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

// Models/JobBid.cs
[Table("JobBids")]
public class JobBid
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid JobId { get; set; }

    [ForeignKey(nameof(JobId))]
    public virtual Job Job { get; set; } = null!;

    [Required]
    public Guid DriverId { get; set; }

    [ForeignKey(nameof(DriverId))]
    public virtual Driver Driver { get; set; } = null!;

    // Bid Details
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal BidAmount { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? Message { get; set; }

    public int? EstimatedDuration { get; set; } // minutes

    public DateTime? ProposedPickupTime { get; set; }

    // Status
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "pending"; // pending, accepted, rejected, withdrawn, expired

    // Accepted/Rejected info
    public string? ResponseMessage { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? RespondedBy { get; set; } // Customer user ID

    // Expiry
    public DateTime? ExpiresAt { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// Models/DriverDocument.cs
[Table("DriverDocuments")]
public class DriverDocument
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid DriverId { get; set; }

    [ForeignKey(nameof(DriverId))]
    public virtual Driver Driver { get; set; } = null!;

    // Document Information
    [Required]
    [MaxLength(50)]
    public required string Type { get; set; }
    // UK Document Types:
    // - driving_license (UK Driving License - Front & Back)
    // - driving_license_counterpart
    // - proof_of_address (Utility bill, Bank statement within 3 months)
    // - national_insurance
    // - right_to_work (Passport, BRP, Share code)
    // - dbs_check (DBS/Disclosure Scotland certificate)
    // - profile_photo
    // - vehicle_mot_certificate
    // - vehicle_insurance_certificate
    // - vehicle_road_tax_certificate
    // - vehicle_registration_v5c
    // - vehicle_hire_reward_insurance
    // - vehicle_goods_in_transit_insurance
    // - vehicle_public_liability_insurance
    // - vehicle_photo_front
    // - vehicle_photo_back
    // - vehicle_photo_side
    // - vehicle_photo_interior

    [Required]
    [MaxLength(255)]
    public required string FileName { get; set; }

    [Required]
    public required string FileUrl { get; set; }

    [MaxLength(100)]
    public string? FileSize { get; set; } // In bytes

    [MaxLength(50)]
    public string? MimeType { get; set; }

    // Dates
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiryDate { get; set; }

    // For UK documents with reference numbers
    [MaxLength(100)]
    public string? DocumentNumber { get; set; }

    public DateTime? IssueDate { get; set; }

    [MaxLength(100)]
    public string? IssuingAuthority { get; set; }

    // Verification
    [MaxLength(20)]
    public string Status { get; set; } = "pending"; // pending, verified, rejected, expired

    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedDate { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? RejectionReason { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? Notes { get; set; }

    // Linked to specific vehicle if applicable
    public Guid? VehicleId { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// Models/Vehicle.cs
[Table("Vehicles")]
public class Vehicle
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid DriverId { get; set; }

    [ForeignKey(nameof(DriverId))]
    public virtual Driver Driver { get; set; } = null!;

    // Basic info
    [Required]
    [MaxLength(50)]
    public required string Type { get; set; } // van, cargo_van, small_truck, etc.

    [Required]
    [MaxLength(100)]
    public required string Make { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Model { get; set; }

    public int Year { get; set; }

    [Required]
    [MaxLength(50)]
    public required string RegistrationNumber { get; set; }

    [MaxLength(50)]
    public string? VinNumber { get; set; }

    // Capacity specs
    public int CargoCapacity { get; set; } // cubic feet

    [Column(TypeName = "decimal(10,2)")]
    public decimal MaxPayloadWeight { get; set; } // lbs or kg

    [Column(TypeName = "decimal(10,2)")]
    public decimal MaxGrossWeight { get; set; }

    // Dimensions
    [Column(TypeName = "decimal(10,2)")]
    public decimal? CargoLength { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? CargoWidth { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? CargoHeight { get; set; }

    // Features and equipment (JSON array)
    [Column(TypeName = "nvarchar(max)")]
    public string? Features { get; set; }

    // UK-Specific Vehicle Requirements

    // MOT (Ministry of Transport Test)
    public bool HasValidMOT { get; set; } = false;
    public DateTime? MOTExpiryDate { get; set; }
    [MaxLength(50)]
    public string? MOTCertificateNumber { get; set; }
    public DateTime? MOTTestDate { get; set; }

    // Insurance
    public bool HasInsurance { get; set; }
    public DateTime? InsuranceExpiry { get; set; }
    [MaxLength(200)]
    public string? InsuranceProvider { get; set; }
    [MaxLength(100)]
    public string? InsurancePolicyNumber { get; set; }
    [MaxLength(50)]
    public string? InsuranceType { get; set; } // Comprehensive, Third Party, Hire & Reward, Goods in Transit

    // Road Tax (VED - Vehicle Excise Duty)
    public bool HasValidRoadTax { get; set; } = false;
    public DateTime? RoadTaxExpiryDate { get; set; }
    [MaxLength(50)]
    public string? TaxClass { get; set; } // Private/Light Goods (PLG), etc.

    // Additional UK Requirements
    public bool HireAndRewardInsurance { get; set; } = false; // Required for commercial use
    public bool GoodsInTransitInsurance { get; set; } = false;
    public bool PublicLiabilityInsurance { get; set; } = false;
    public DateTime? PublicLiabilityExpiry { get; set; }

    // Approval and Vetting
    [MaxLength(20)]
    public string ApprovalStatus { get; set; } = "pending"; // pending, approved, rejected

    public string? ApprovedBy { get; set; } // FK to AspNetUsers (admin who approved)
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }

    // Status
    [MaxLength(20)]
    public string Status { get; set; } = "inactive"; // active, inactive, maintenance, retired, pending_approval

    public DateTime? LastInspectionDate { get; set; }
    public DateTime? NextInspectionDue { get; set; }
    public int? Mileage { get; set; }

    // Photos (JSON array)
    [Column(TypeName = "nvarchar(max)")]
    public string? Photos { get; set; }

    public bool IsActive { get; set; } = true;

    // Admin/Vetting Notes
    [Column(TypeName = "nvarchar(max)")]
    public string? AdminNotes { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? VettingNotes { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}