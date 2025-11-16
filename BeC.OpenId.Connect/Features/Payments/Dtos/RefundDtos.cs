namespace BeC.OpenId.Connect.Features.Payments.Dtos;

public class ProcessRefundDto
{
    public decimal? Amount { get; set; } // Null = full refund, specific amount = partial refund
    public required string Reason { get; set; }
    public string? Notes { get; set; }
}

public class RefundResponseDto
{
    public Guid PaymentId { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
    public decimal OriginalAmount { get; set; }
    public string RefundStatus { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime RefundedAt { get; set; }
    public string? RefundReference { get; set; }
}
