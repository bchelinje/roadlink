namespace BeC.OpenId.Connect.Features.Jobs.Dtos;

public class UpdateJobStatusDto
{
    public required string Status { get; set; }
    public string? Notes { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }
}

public class JobPhotoDto
{
    public required string PhotoUrl { get; set; }
    public string? Caption { get; set; }
    public string PhotoType { get; set; } = "delivery"; // delivery, pickup, damage, other
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
