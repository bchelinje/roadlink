namespace BeC.OpenId.Connect.Features.Documents.ViewModels;

/// <summary>
/// View model for document statistics
/// </summary>
public class DocumentStatisticsViewModel
{
    public int TotalDocuments { get; set; }
    public int PendingVerification { get; set; }
    public int Verified { get; set; }
    public int Rejected { get; set; }
    public int Expired { get; set; }
    public int ExpiringSoon { get; set; }
    public Dictionary<string, int> DocumentsByType { get; set; } = new();
}
