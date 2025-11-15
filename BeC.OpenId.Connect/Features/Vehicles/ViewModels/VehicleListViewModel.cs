namespace BeC.OpenId.Connect.Features.Vehicles.ViewModels;

/// <summary>
/// View model for paginated vehicle list
/// </summary>
public class VehicleListViewModel
{
    public List<VehicleViewModel> Vehicles { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
