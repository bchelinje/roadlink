namespace BeC.OpenId.Connect.Features.Documents.Models;

/// <summary>
/// Model for uploading a document
/// </summary>
public class UploadDocumentModel
{
    public string Type { get; set; } = string.Empty;
    public IFormFile? File { get; set; }
    public DateTime? ExpiryDate { get; set; }
}
