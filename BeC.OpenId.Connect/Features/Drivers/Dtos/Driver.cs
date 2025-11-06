
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
    public required string Type { get; set; } // drivers_license, insurance, vehicle_registration, etc.

    [Required]
    [MaxLength(255)]
    public required string FileName { get; set; }

    [Required]
    public required string FileUrl { get; set; }

    // Dates
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiryDate { get; set; }

    // Verification
    [MaxLength(20)]
    public string Status { get; set; } = "pending"; // pending, verified, rejected, expired

    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedDate { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
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

    public bool HasInsurance { get; set; }
    public DateTime? InsuranceExpiry { get; set; }

    // Status
    [MaxLength(20)]
    public string Status { get; set; } = "active"; // active, inactive, maintenance, retired

    public DateTime? LastInspectionDate { get; set; }
    public DateTime? NextInspectionDue { get; set; }
    public int? Mileage { get; set; }

    // Photos (JSON array)
    [Column(TypeName = "nvarchar(max)")]
    public string? Photos { get; set; }

    public bool IsActive { get; set; } = true;

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}