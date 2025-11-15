namespace BeC.OpenId.Connect.Features.Documents.Models;

/// <summary>
/// Model for uploading a document
/// </summary>
public class UploadDocumentModel
{
    public required string Type { get; set; }
    public required IFormFile File { get; set; }
    public DateTime? ExpiryDate { get; set; }
}
