namespace BeC.OpenId.Connect.Features.Vehicles.ViewModels;

/// <summary>
/// View model for vehicle maintenance history
/// </summary>
public class MaintenanceHistoryViewModel
{
    public DateTime Timestamp { get; set; }
    public string Description { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}
