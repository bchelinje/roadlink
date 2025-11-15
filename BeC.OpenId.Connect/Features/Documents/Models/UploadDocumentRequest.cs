namespace BeC.OpenId.Connect.Features.Documents.Models;

public class UploadDocumentRequest
{
    public string Type { get; set; }
    public IFormFile File { get; set; }
    public DateTime? ExpiryDate { get; set; }
}