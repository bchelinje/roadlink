using BeC.Common.Data;
using BeC.OpenId.Connect.Features.Drivers.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BeC.OpenId.Connect.Infrastructure.Payments;

/// <summary>
/// Background service for processing automated driver payouts on schedule
/// Runs weekly (or as configured) to batch all pending driver earnings into payouts
/// </summary>
public class PayoutSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PayoutSchedulerService> _logger;
    private readonly StripeSettings _settings;

    public PayoutSchedulerService(
        IServiceProvider serviceProvider,
        ILogger<PayoutSchedulerService> logger,
        IOptions<StripeSettings> settings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payout Scheduler Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessScheduledPayoutsAsync();

                // Wait based on schedule (default: weekly)
                var delay = _settings.AutoPayoutSchedule?.ToLower() switch
                {
                    "daily" => TimeSpan.FromDays(1),
                    "weekly" => TimeSpan.FromDays(7),
                    "monthly" => TimeSpan.FromDays(30),
                    _ => TimeSpan.FromDays(7) // Default weekly
                };

                _logger.LogInformation(
                    "Next payout processing scheduled in {Delay}",
                    delay);

                await Task.Delay(delay, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in payout scheduler");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Retry in 1 hour on error
            }
        }

        _logger.LogInformation("Payout Scheduler Service stopped");
    }

    /// <summary>
    /// Process all pending driver earnings and create payouts
    /// </summary>
    private async Task ProcessScheduledPayoutsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var paymentService = scope.ServiceProvider.GetRequiredService<IStripePaymentService>();

        try
        {
            // Get all drivers with pending earnings
            var driversWithPendingEarnings = await context.Earnings
                .Where(e => e.PaymentStatus == "pending")
                .GroupBy(e => e.DriverId)
                .Select(g => new
                {
                    DriverId = g.Key,
                    TotalAmount = g.Sum(e => e.NetAmount),
                    Count = g.Count()
                })
                .ToListAsync();

            if (!driversWithPendingEarnings.Any())
            {
                _logger.LogInformation("No pending earnings to process");
                return;
            }

            _logger.LogInformation(
                "Processing payouts for {Count} drivers",
                driversWithPendingEarnings.Count);

            foreach (var driver in driversWithPendingEarnings)
            {
                try
                {
                    var payoutNumber = await paymentService.CreateDriverPayoutAsync(
                        driver.DriverId,
                        driver.TotalAmount,
                        $"Weekly payout for {driver.Count} completed jobs");

                    _logger.LogInformation(
                        "Created payout {PayoutNumber} for driver {DriverId}: ${Amount} ({Count} jobs)",
                        payoutNumber, driver.DriverId, driver.TotalAmount, driver.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to create payout for driver {DriverId}",
                        driver.DriverId);
                }
            }

            _logger.LogInformation("Payout processing completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing scheduled payouts");
            throw;
        }
    }
}
