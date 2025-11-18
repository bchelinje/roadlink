using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Users.Dtos;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using AuthRoles = BeC.OpenId.Connect.Infrastructure.Authorization.Roles;

namespace BeC.OpenId.Connect.Features.Admin.Controllers;

/// <summary>
/// Unified admin dashboard with comprehensive metrics and analytics
/// </summary>
[ApiController]
[Route("api/admin/dashboard")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
           Roles = $"{AuthRoles.Admin},{AuthRoles.SuperAdmin}")]
[Produces("application/json")]
public class AdminDashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<AdminDashboardController> _logger;

    public AdminDashboardController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IActivityLogService activityLogService,
        ILogger<AdminDashboardController> logger)
    {
        _context = context;
        _userManager = userManager;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    /// <summary>
    /// Get comprehensive dashboard overview with all key metrics
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetDashboard()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var now = DateTime.UtcNow;
        var today = now.Date;
        var thisMonth = new DateTime(now.Year, now.Month, 1);
        var lastMonth = thisMonth.AddMonths(-1);

        // User metrics
        var totalUsers = await _userManager.Users.CountAsync();
        var totalCustomers = await _context.Customers.CountAsync();
        var totalDrivers = await _context.Drivers.CountAsync();
        var activeCustomers = await _context.Customers.CountAsync(c => c.Status == "active");
        var activeDrivers = await _context.Drivers.CountAsync(d => d.Status == "active");
        var pendingDriverApprovals = await _context.Drivers.CountAsync(d => d.ApprovalStatus == "pending");
        var pendingCustomerApprovals = await _context.Customers.CountAsync(c => c.ApprovalStatus == "pending");

        var newUsersThisMonth = await _userManager.Users
            .CountAsync(u => u.CreatedAt >= thisMonth);
        var newUsersLastMonth = await _userManager.Users
            .CountAsync(u => u.CreatedAt >= lastMonth && u.CreatedAt < thisMonth);

        // Job metrics
        var totalJobs = await _context.Jobs.CountAsync();
        var activeJobs = await _context.Jobs.CountAsync(j =>
            j.Status == "pending" || j.Status == "assigned" || j.Status == "in_progress");
        var completedJobs = await _context.Jobs.CountAsync(j => j.Status == "completed");
        var cancelledJobs = await _context.Jobs.CountAsync(j => j.Status == "cancelled");

        var jobsToday = await _context.Jobs.CountAsync(j => j.CreatedAt >= today);
        var jobsThisMonth = await _context.Jobs.CountAsync(j => j.CreatedAt >= thisMonth);
        var jobsLastMonth = await _context.Jobs.CountAsync(j =>
            j.CreatedAt >= lastMonth && j.CreatedAt < thisMonth);

        var averageJobCompletionTime = await _context.Jobs
            .Where(j => j.Status == "completed" && j.CompletedAt.HasValue)
            .Select(j => EF.Functions.DateDiffMinute(j.CreatedAt, j.CompletedAt!.Value))
            .AverageAsync();

        // Payment & Revenue metrics
        var totalRevenue = await _context.Payments
            .Where(p => p.Status == "succeeded")
            .SumAsync(p => (decimal?)p.TotalAmount) ?? 0;

        var revenueThisMonth = await _context.Payments
            .Where(p => p.Status == "succeeded" && p.CreatedAt >= thisMonth)
            .SumAsync(p => (decimal?)p.TotalAmount) ?? 0;

        var revenueLastMonth = await _context.Payments
            .Where(p => p.Status == "succeeded" && p.CreatedAt >= lastMonth && p.CreatedAt < thisMonth)
            .SumAsync(p => (decimal?)p.TotalAmount) ?? 0;

        var totalPlatformFees = await _context.Payments
            .Where(p => p.Status == "succeeded")
            .SumAsync(p => (decimal?)p.PlatformFee) ?? 0;

        var totalDriverEarnings = await _context.Payments
            .Where(p => p.Status == "succeeded")
            .SumAsync(p => (decimal?)p.DriverEarnings) ?? 0;

        var pendingPayouts = await _context.Payouts
            .Where(p => p.Status == "pending")
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        // Support & Service metrics
        var totalTickets = await _context.SupportTickets.CountAsync();
        var openTickets = await _context.SupportTickets.CountAsync(t =>
            t.Status == "open" || t.Status == "in_progress");
        var resolvedTicketsThisMonth = await _context.SupportTickets
            .CountAsync(t => t.Status == "resolved" && t.ResolvedAt >= thisMonth);

        var averageTicketResolutionTime = await _context.SupportTickets
            .Where(t => t.ResolutionTimeMinutes.HasValue)
            .AverageAsync(t => (double?)t.ResolutionTimeMinutes) ?? 0;

        var totalComplaints = await _context.Complaints.CountAsync();
        var openComplaints = await _context.Complaints
            .CountAsync(c => c.Status != "resolved" && c.Status != "dismissed");
        var escalatedComplaints = await _context.Complaints.CountAsync(c => c.IsEscalated);

        // Chat/Messaging metrics
        var totalMessages = await _context.ChatMessages.CountAsync();
        var messagesToday = await _context.ChatMessages.CountAsync(m => m.CreatedAt >= today);
        var activeConversations = await _context.Conversations
            .CountAsync(c => c.Status == "active");

        // Review metrics
        var totalReviews = await _context.Reviews.CountAsync();
        var averageRating = await _context.Reviews.AverageAsync(r => (double?)r.Rating) ?? 0;
        var flaggedReviews = await _context.Reviews.CountAsync(r => r.IsFlagged);

        // Vehicle metrics
        var totalVehicles = await _context.Vehicles.CountAsync();
        var activeVehicles = await _context.Vehicles.CountAsync(v => v.Status == "active");
        var pendingVehicleApprovals = await _context.Vehicles
            .CountAsync(v => v.ApprovalStatus == "pending");

        // Calculate growth rates
        var userGrowthRate = newUsersLastMonth > 0
            ? Math.Round(((newUsersThisMonth - newUsersLastMonth) / (double)newUsersLastMonth) * 100, 2)
            : 0;

        var jobGrowthRate = jobsLastMonth > 0
            ? Math.Round(((jobsThisMonth - jobsLastMonth) / (double)jobsLastMonth) * 100, 2)
            : 0;

        var revenueGrowthRate = revenueLastMonth > 0
            ? Math.Round(((double)(revenueThisMonth - revenueLastMonth) / (double)revenueLastMonth) * 100, 2)
            : 0;

        return Ok(new
        {
            overview = new
            {
                totalUsers,
                totalCustomers,
                totalDrivers,
                activeCustomers,
                activeDrivers,
                totalJobs,
                activeJobs,
                completedJobs,
                totalRevenue,
                revenueThisMonth,
                totalPlatformFees
            },
            users = new
            {
                total = totalUsers,
                customers = new
                {
                    total = totalCustomers,
                    active = activeCustomers,
                    pendingApproval = pendingCustomerApprovals
                },
                drivers = new
                {
                    total = totalDrivers,
                    active = activeDrivers,
                    pendingApproval = pendingDriverApprovals
                },
                newThisMonth = newUsersThisMonth,
                newLastMonth = newUsersLastMonth,
                growthRate = userGrowthRate
            },
            jobs = new
            {
                total = totalJobs,
                active = activeJobs,
                completed = completedJobs,
                cancelled = cancelledJobs,
                today = jobsToday,
                thisMonth = jobsThisMonth,
                lastMonth = jobsLastMonth,
                growthRate = jobGrowthRate,
                averageCompletionTimeMinutes = averageJobCompletionTime != null
                    ? Math.Round((double)averageJobCompletionTime, 2) : 0
            },
            revenue = new
            {
                total = totalRevenue,
                thisMonth = revenueThisMonth,
                lastMonth = revenueLastMonth,
                platformFees = totalPlatformFees,
                driverEarnings = totalDriverEarnings,
                pendingPayouts,
                growthRate = revenueGrowthRate
            },
            support = new
            {
                tickets = new
                {
                    total = totalTickets,
                    open = openTickets,
                    resolvedThisMonth = resolvedTicketsThisMonth,
                    averageResolutionTimeMinutes = Math.Round(averageTicketResolutionTime, 2)
                },
                complaints = new
                {
                    total = totalComplaints,
                    open = openComplaints,
                    escalated = escalatedComplaints
                }
            },
            messaging = new
            {
                totalMessages,
                messagesToday,
                activeConversations
            },
            reviews = new
            {
                total = totalReviews,
                averageRating = Math.Round(averageRating, 2),
                flagged = flaggedReviews
            },
            vehicles = new
            {
                total = totalVehicles,
                active = activeVehicles,
                pendingApproval = pendingVehicleApprovals
            },
            timestamp = DateTime.UtcNow
        });

        await _activityLogService.LogActivityAsync(
            userId,
            "dashboard_viewed",
            "Dashboard",
            "admin",
            "Admin Dashboard",
            "Viewed admin dashboard metrics"
        );
    }

    /// <summary>
    /// Get user analytics and trends
    /// </summary>
    [HttpGet("analytics/users")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetUserAnalytics([FromQuery] int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days).Date;

        var userRegistrations = await _userManager.Users
            .Where(u => u.CreatedAt >= startDate)
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new { date = g.Key, count = g.Count() })
            .OrderBy(x => x.date)
            .ToListAsync();

        var customersByType = await _context.Customers
            .GroupBy(c => c.CustomerType)
            .Select(g => new { type = g.Key, count = g.Count() })
            .ToListAsync();

        var topCustomers = await _context.Customers
            .OrderByDescending(c => c.TotalJobs)
            .Take(10)
            .Select(c => new
            {
                id = c.Id,
                name = $"{c.FirstName} {c.LastName}",
                email = c.Email,
                totalJobs = c.TotalJobs,
                completedJobs = c.CompletedJobs,
                rating = c.Rating
            })
            .ToListAsync();

        var topDrivers = await _context.Drivers
            .OrderByDescending(d => d.TotalJobs)
            .Take(10)
            .Select(d => new
            {
                id = d.Id,
                name = $"{d.FirstName} {d.LastName}",
                email = d.Email,
                totalJobs = d.TotalJobs,
                completedJobs = d.CompletedJobs,
                rating = d.Rating
            })
            .ToListAsync();

        return Ok(new
        {
            registrationTrend = userRegistrations,
            customersByType,
            topCustomers,
            topDrivers
        });
    }

    /// <summary>
    /// Get job analytics and trends
    /// </summary>
    [HttpGet("analytics/jobs")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetJobAnalytics([FromQuery] int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days).Date;

        var jobTrend = await _context.Jobs
            .Where(j => j.CreatedAt >= startDate)
            .GroupBy(j => j.CreatedAt.Date)
            .Select(g => new { date = g.Key, count = g.Count() })
            .OrderBy(x => x.date)
            .ToListAsync();

        var jobsByStatus = await _context.Jobs
            .GroupBy(j => j.Status)
            .Select(g => new { status = g.Key, count = g.Count() })
            .ToListAsync();

        var jobsByVehicleType = await _context.Jobs
            .Where(j => j.VehicleTypeRequired != null)
            .GroupBy(j => j.VehicleTypeRequired)
            .Select(g => new { vehicleType = g.Key, count = g.Count() })
            .ToListAsync();

        var jobsByPriority = await _context.Jobs
            .GroupBy(j => j.Priority)
            .Select(g => new { priority = g.Key, count = g.Count() })
            .ToListAsync();

        var peakHours = await _context.Jobs
            .GroupBy(j => j.ScheduledDate.Hour)
            .Select(g => new { hour = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .Take(5)
            .ToListAsync();

        return Ok(new
        {
            jobTrend,
            jobsByStatus,
            jobsByVehicleType,
            jobsByPriority,
            peakHours
        });
    }

    /// <summary>
    /// Get revenue analytics and trends
    /// </summary>
    [HttpGet("analytics/revenue")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetRevenueAnalytics([FromQuery] int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days).Date;

        var revenueTrend = await _context.Payments
            .Where(p => p.Status == "succeeded" && p.CreatedAt >= startDate)
            .GroupBy(p => p.CreatedAt.Date)
            .Select(g => new
            {
                date = g.Key,
                revenue = g.Sum(p => p.TotalAmount),
                platformFees = g.Sum(p => p.PlatformFee),
                driverEarnings = g.Sum(p => p.DriverEarnings)
            })
            .OrderBy(x => x.date)
            .ToListAsync();

        var paymentMethodBreakdown = await _context.Payments
            .Where(p => p.Status == "succeeded")
            .GroupBy(p => p.PaymentMethod)
            .Select(g => new
            {
                method = g.Key,
                count = g.Count(),
                amount = g.Sum(p => p.TotalAmount)
            })
            .ToListAsync();

        var refundStats = await _context.Payments
            .Where(p => p.RefundAmount > 0)
            .GroupBy(p => 1)
            .Select(g => new
            {
                totalRefunds = g.Count(),
                totalRefundAmount = g.Sum(p => p.RefundAmount)
            })
            .FirstOrDefaultAsync();

        return Ok(new
        {
            revenueTrend,
            paymentMethodBreakdown,
            refunds = refundStats != null
                ? new { refundStats.totalRefunds, refundStats.totalRefundAmount }
                : new { totalRefunds = 0, totalRefundAmount = (decimal?)0 }
        });
    }

    /// <summary>
    /// Get platform health indicators
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetPlatformHealth()
    {
        var now = DateTime.UtcNow;
        var last24h = now.AddHours(-24);
        var last7days = now.AddDays(-7);

        // User activity
        var activeUsersLast24h = await _context.ActivityLogs
            .Where(a => a.Timestamp >= last24h)
            .Select(a => a.UserId)
            .Distinct()
            .CountAsync();

        var activeUsersLast7days = await _context.ActivityLogs
            .Where(a => a.Timestamp >= last7days)
            .Select(a => a.UserId)
            .Distinct()
            .CountAsync();

        // Job completion rate
        var totalJobsLast7days = await _context.Jobs
            .CountAsync(j => j.CreatedAt >= last7days);

        var completedJobsLast7days = await _context.Jobs
            .CountAsync(j => j.Status == "completed" && j.CreatedAt >= last7days);

        var jobCompletionRate = totalJobsLast7days > 0
            ? Math.Round((completedJobsLast7days / (double)totalJobsLast7days) * 100, 2)
            : 0;

        // Customer satisfaction
        var averageReviewRatingLast7days = await _context.Reviews
            .Where(r => r.CreatedAt >= last7days)
            .AverageAsync(r => (double?)r.Rating) ?? 0;

        var ticketSatisfaction = await _context.SupportTickets
            .Where(t => t.CustomerSatisfactionRating.HasValue && t.ClosedAt >= last7days)
            .AverageAsync(t => (double?)t.CustomerSatisfactionRating) ?? 0;

        // Error rates
        var errorLogsLast24h = await _context.ActivityLogs
            .CountAsync(a => a.Timestamp >= last24h && a.Severity == "error");

        var criticalLogsLast24h = await _context.ActivityLogs
            .CountAsync(a => a.Timestamp >= last24h && a.Severity == "critical");

        // Response times
        var averageFirstResponseTime = await _context.SupportTickets
            .Where(t => t.FirstResponseTimeMinutes.HasValue && t.CreatedAt >= last7days)
            .AverageAsync(t => (double?)t.FirstResponseTimeMinutes) ?? 0;

        return Ok(new
        {
            userActivity = new
            {
                activeUsersLast24h,
                activeUsersLast7days
            },
            jobMetrics = new
            {
                totalJobsLast7days,
                completedJobsLast7days,
                completionRate = jobCompletionRate
            },
            customerSatisfaction = new
            {
                averageReviewRating = Math.Round(averageReviewRatingLast7days, 2),
                averageTicketSatisfaction = Math.Round(ticketSatisfaction, 2)
            },
            systemHealth = new
            {
                errorLogsLast24h,
                criticalLogsLast24h,
                averageFirstResponseTimeMinutes = Math.Round(averageFirstResponseTime, 2)
            },
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get recent activity across the platform (Admin only)
    /// </summary>
    [HttpGet("recent-activity")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetRecentActivity([FromQuery] int limit = 50)
    {
        var recentActivity = await _context.ActivityLogs
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .Select(a => new
            {
                id = a.Id,
                userId = a.UserId,
                action = a.Action,
                entityType = a.EntityType,
                entityId = a.EntityId,
                description = a.Description,
                metadata = a.MetadataJson,
                severity = a.Severity,
                timestamp = a.Timestamp
            })
            .ToListAsync();

        return Ok(new
        {
            activities = recentActivity,
            count = recentActivity.Count
        });
    }
}
