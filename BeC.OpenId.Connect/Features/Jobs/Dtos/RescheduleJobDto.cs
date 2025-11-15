namespace BeC.OpenId.Connect.Features.Jobs.Dtos;

public class RescheduleJobDto
{
    public required DateTime NewScheduledDate { get; set; }
    public required string NewScheduledTime { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
}

public class CancelJobDto
{
    public required string Reason { get; set; }
    public string? CancellationNotes { get; set; }
    public bool RequestRefund { get; set; } = false;
}

public class RescheduleHistoryDto
{
    public DateTime OldScheduledDate { get; set; }
    public string OldScheduledTime { get; set; } = string.Empty;
    public DateTime NewScheduledDate { get; set; }
    public string NewScheduledTime { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime RescheduledAt { get; set; }
    public string RescheduledBy { get; set; } = string.Empty;
}
