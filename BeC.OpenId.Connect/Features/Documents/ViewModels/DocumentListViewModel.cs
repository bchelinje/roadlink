namespace BeC.OpenId.Connect.Features.Documents.ViewModels;

/// <summary>
/// View model for paginated document list
/// </summary>
public class DocumentListViewModel
{
    public List<DocumentViewModel> Documents { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
