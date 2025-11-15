namespace BeC.OpenId.Connect.Features.Reviews.Dtos;

public class CreateReviewResponseDto
{
    public required string Response { get; set; }
}

public class ReportReviewDto
{
    public required string Reason { get; set; }
    public string? Details { get; set; }
}

public class ModerateReviewDto
{
    public required string Action { get; set; } // "approve", "hide", "remove"
    public string? ModeratorNotes { get; set; }
}

public class ReviewWithResponseDto
{
    public Guid Id { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public string ReviewerType { get; set; } = string.Empty;
    public string RevieweeName { get; set; } = string.Empty;
    public string RevieweeType { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Response { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? RespondedBy { get; set; }
    public bool IsFlagged { get; set; }
    public bool IsHidden { get; set; }
}
