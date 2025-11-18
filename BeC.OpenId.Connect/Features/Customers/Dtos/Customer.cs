using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeC.OpenId.Connect.Features.Customers.Dtos;

/// <summary>
/// Customer entity for tracking customer-specific information
/// Linked to ApplicationUser via UserId
/// </summary>
[Table("Customers")]
public class Customer
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

    // Account Status
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "pending"; // pending, approved, active, suspended, deactivated

    [MaxLength(20)]
    public string? ApprovalStatus { get; set; } = "pending"; // pending, approved, rejected

    public string? ApprovedBy { get; set; } // FK to AspNetUsers (admin who approved)
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }

    // Profile
    public string? ProfileImage { get; set; }
    public string? CompanyName { get; set; } // For business customers

    [MaxLength(20)]
    public string? CustomerType { get; set; } = "individual"; // individual, business

    // Address (JSON)
    [Column(TypeName = "nvarchar(max)")]
    public string? PrimaryAddress { get; set; }

    // Preferences (JSON)
    [Column(TypeName = "nvarchar(max)")]
    public string? Preferences { get; set; }

    // Statistics
    [Column(TypeName = "decimal(3,2)")]
    public decimal Rating { get; set; } = 0; // Customer rating from drivers

    public int TotalJobs { get; set; } = 0;
    public int CompletedJobs { get; set; } = 0;
    public int CancelledJobs { get; set; } = 0;

    // Dates
    public DateTime JoinedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastJobDate { get; set; }

    // Payment
    public string? PreferredPaymentMethod { get; set; }
    public bool PaymentMethodVerified { get; set; } = false;

    // Verification
    public bool EmailVerified { get; set; } = false;
    public bool PhoneVerified { get; set; } = false;
    public bool IdentityVerified { get; set; } = false;

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Notes for admin/vetting
    [Column(TypeName = "nvarchar(max)")]
    public string? AdminNotes { get; set; }
}
