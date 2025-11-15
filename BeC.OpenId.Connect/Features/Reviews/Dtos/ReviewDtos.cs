using System.ComponentModel.DataAnnotations;

namespace BeC.OpenId.Connect.Features.Reviews.Dtos;

public class CreateReviewDto
{
    [Required]
    public Guid JobId { get; set; }

    [Required]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int Rating { get; set; }

    [MaxLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters")]
    public string? Comment { get; set; }

    // Individual rating categories (optional)
    [Range(1, 5, ErrorMessage = "Punctuality rating must be between 1 and 5")]
    public int? PunctualityRating { get; set; }

    [Range(1, 5, ErrorMessage = "Professionalism rating must be between 1 and 5")]
    public int? ProfessionalismRating { get; set; }

    [Range(1, 5, ErrorMessage = "Care of items rating must be between 1 and 5")]
    public int? CareOfItemsRating { get; set; }

    [Range(1, 5, ErrorMessage = "Communication rating must be between 1 and 5")]
    public int? CommunicationRating { get; set; }
}

public class ReviewDto
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public Guid DriverId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }

    // Individual ratings
    public int? PunctualityRating { get; set; }
    public int? ProfessionalismRating { get; set; }
    public int? CareOfItemsRating { get; set; }
    public int? CommunicationRating { get; set; }

    public string Status { get; set; } = string.Empty;
    public string? DriverResponse { get; set; }
    public DateTime? ResponseDate { get; set; }
    public int HelpfulVotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class DriverRatingSummaryDto
{
    public Guid DriverId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public decimal AverageRating { get; set; }
    public int TotalReviews { get; set; }

    // Average category ratings
    public decimal? AveragePunctualityRating { get; set; }
    public decimal? AverageProfessionalismRating { get; set; }
    public decimal? AverageCareOfItemsRating { get; set; }
    public decimal? AverageCommunicationRating { get; set; }

    // Rating distribution
    public int FiveStarCount { get; set; }
    public int FourStarCount { get; set; }
    public int ThreeStarCount { get; set; }
    public int TwoStarCount { get; set; }
    public int OneStarCount { get; set; }

    public List<ReviewDto> RecentReviews { get; set; } = new();
}

public class AddDriverResponseDto
{
    [Required]
    [MaxLength(500, ErrorMessage = "Response cannot exceed 500 characters")]
    public required string Response { get; set; }
}
