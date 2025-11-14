namespace BeC.OpenId.Connect.Features.Drivers.Dtos;

public class EarningSummaryDto
{
    public decimal TotalEarnings { get; set; }
    public decimal PendingEarnings { get; set; }
    public decimal PaidEarnings { get; set; }
    public int TotalJobs { get; set; }
    public int PendingPayments { get; set; }
    public int CompletedPayments { get; set; }
    public decimal AverageEarningPerJob { get; set; }
    public decimal? TotalBonuses { get; set; }
    public decimal? TotalTips { get; set; }
    public decimal? TotalDeductions { get; set; }
    public DateTime? LastPaymentDate { get; set; }
    public DateTime? NextPaymentDate { get; set; }
}

public class PeriodEarningsSummaryDto
{
    public string Period { get; set; } = string.Empty; // "daily", "weekly", "monthly"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalEarnings { get; set; }
    public int JobsCompleted { get; set; }
    public decimal AverageEarningPerJob { get; set; }
    public List<DailyEarningDto> DailyBreakdown { get; set; } = new();
}

public class DailyEarningDto
{
    public DateTime Date { get; set; }
    public decimal TotalEarnings { get; set; }
    public int JobsCompleted { get; set; }
}

public class PaymentStatusSummaryDto
{
    public int PendingCount { get; set; }
    public decimal PendingAmount { get; set; }
    public int ProcessingCount { get; set; }
    public decimal ProcessingAmount { get; set; }
    public int PaidCount { get; set; }
    public decimal PaidAmount { get; set; }
    public int FailedCount { get; set; }
    public decimal FailedAmount { get; set; }
}
