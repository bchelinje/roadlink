namespace BeC.OpenId.Connect.Features.Vehicles.Models;

/// <summary>
/// Model for logging vehicle maintenance
/// </summary>
public class LogMaintenanceModel
{
    public DateTime? MaintenanceDate { get; set; }
    public DateTime? NextInspectionDue { get; set; }
    public int? Mileage { get; set; }
    public string? Description { get; set; }
}
