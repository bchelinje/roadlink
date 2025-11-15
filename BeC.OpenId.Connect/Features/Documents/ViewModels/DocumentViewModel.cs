namespace BeC.OpenId.Connect.Features.Documents.ViewModels;

/// <summary>
/// View model for document response
/// </summary>
public class DocumentViewModel
{
    public Guid Id { get; set; }
    public Guid DriverId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public string Status { get; set; } = "pending";
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedDate { get; set; }
    public DateTime UploadedDate { get; set; }

    // Driver information (when included)
    public DocumentDriverInfo? Driver { get; set; }
}

/// <summary>
/// Nested driver information in document response
/// </summary>
public class DocumentDriverInfo
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
}
