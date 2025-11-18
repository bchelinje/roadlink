using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Users.Dtos;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;

namespace BeC.OpenId.Connect.Features.DataPrivacy.Services;

public class DataAnonymizationService : IDataAnonymizationService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<DataAnonymizationService> _logger;

    public DataAnonymizationService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IActivityLogService activityLogService,
        ILogger<DataAnonymizationService> logger)
    {
        _context = context;
        _userManager = userManager;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    public async Task<(bool success, string? errorMessage, Dictionary<string, int> affectedRecords)> AnonymizeUserDataAsync(string userId)
    {
        var affectedRecords = new Dictionary<string, int>();

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return (false, "User not found", affectedRecords);

            var anonymizedId = Guid.NewGuid().ToString().Substring(0, 8);
            var anonymizedEmail = $"deleted-user-{anonymizedId}@anonymized.local";
            var anonymizedName = $"[Deleted User {anonymizedId}]";
            var anonymizedPhone = "000-000-0000";

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Anonymize AspNetUsers
                user.Email = anonymizedEmail;
                user.NormalizedEmail = anonymizedEmail.ToUpper();
                user.UserName = anonymizedEmail;
                user.NormalizedUserName = anonymizedEmail.ToUpper();
                user.PhoneNumber = null;
                user.DisplayName = anonymizedName;
                user.ProfilePictureUrl = null;
                user.IsDeleted = true;
                user.DeletedAt = DateTime.UtcNow;
                user.DeletionType = "soft";

                await _userManager.UpdateAsync(user);
                affectedRecords["AspNetUsers"] = 1;

                // 2. Anonymize Customer record
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
                if (customer != null)
                {
                    customer.FirstName = "Deleted";
                    customer.LastName = "User";
                    customer.Email = anonymizedEmail;
                    customer.Phone = anonymizedPhone;
                    customer.ProfileImage = null;
                    customer.CompanyName = null;
                    customer.PrimaryAddress = null;
                    customer.AdminNotes = $"[ANONYMIZED ON {DateTime.UtcNow:yyyy-MM-dd}]";
                    affectedRecords["Customers"] = 1;
                }

                // 3. Anonymize Driver record
                var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
                if (driver != null)
                {
                    driver.FirstName = "Deleted";
                    driver.LastName = "User";
                    driver.Email = anonymizedEmail;
                    driver.Phone = anonymizedPhone;
                    driver.ProfileImage = null;
                    driver.LicenseNumber = $"ANON-{anonymizedId}";
                    driver.Address = null;
                    driver.EmergencyContact = null;
                    driver.AdminNotes = $"[ANONYMIZED ON {DateTime.UtcNow:yyyy-MM-dd}]";
                    driver.VettingNotes = $"[ANONYMIZED ON {DateTime.UtcNow:yyyy-MM-dd}]";
                    affectedRecords["Drivers"] = 1;
                }

                // 4. Anonymize Jobs (as customer)
                var customerJobs = await _context.Jobs
                    .Where(j => j.CustomerId == userId)
                    .ToListAsync();

                foreach (var job in customerJobs)
                {
                    job.CustomerName = anonymizedName;
                    job.CustomerPhone = anonymizedPhone;
                    job.CustomerEmail = anonymizedEmail;
                    job.CustomerNotes = null;
                    job.PickupLocation = JsonSerializer.Serialize(new { address = "[Anonymized]", coordinates = "" });
                    job.DeliveryLocation = JsonSerializer.Serialize(new { address = "[Anonymized]", coordinates = "" });
                }
                affectedRecords["Jobs_Customer"] = customerJobs.Count;

                // 5. Anonymize Jobs (as driver)
                var driverJobs = await _context.Jobs
                    .Where(j => driver != null && j.DriverId == driver.Id)
                    .ToListAsync();

                foreach (var job in driverJobs)
                {
                    job.DriverName = anonymizedName;
                    job.InternalNotes = null;
                }
                affectedRecords["Jobs_Driver"] = driverJobs.Count;

                // 6. Anonymize Reviews (as reviewer)
                var reviews = await _context.Reviews
                    .Where(r => r.ReviewerId == userId)
                    .ToListAsync();

                foreach (var review in reviews)
                {
                    review.ReviewerName = anonymizedName;
                    // CustomerEmail field exists in Review entity
                    review.CustomerEmail = anonymizedEmail;
                }
                affectedRecords["Reviews"] = reviews.Count;

                // 7. Anonymize Payments
                var payments = await _context.Payments
                    .Where(p => p.CustomerId == userId || (driver != null && p.DriverId == driver.Id))
                    .ToListAsync();

                foreach (var payment in payments)
                {
                    if (payment.CustomerId == userId)
                    {
                        payment.CustomerEmail = anonymizedEmail;
                        payment.CustomerName = anonymizedName;
                    }
                    if (driver != null && payment.DriverId == driver.Id)
                    {
                        payment.DriverName = anonymizedName;
                    }
                    payment.PaymentMethodDetails = null;
                    payment.Notes = null;
                }
                affectedRecords["Payments"] = payments.Count;

                // 8. Anonymize Support Tickets
                var tickets = await _context.SupportTickets
                    .Where(t => t.UserId == userId)
                    .ToListAsync();

                foreach (var ticket in tickets)
                {
                    ticket.UserName = anonymizedName;
                    ticket.UserEmail = anonymizedEmail;
                    ticket.InternalNotes = $"[User anonymized on {DateTime.UtcNow:yyyy-MM-dd}]";
                }
                affectedRecords["SupportTickets"] = tickets.Count;

                // 9. Anonymize Chat Messages
                var messages = await _context.ChatMessages
                    .Where(m => m.SenderId == userId || m.RecipientId == userId)
                    .ToListAsync();

                foreach (var message in messages)
                {
                    if (message.SenderId == userId)
                    {
                        message.SenderName = anonymizedName;
                    }
                    if (message.RecipientId == userId)
                    {
                        message.RecipientName = anonymizedName;
                    }
                }
                affectedRecords["ChatMessages"] = messages.Count;

                // 10. Anonymize Conversations
                var conversations = await _context.Conversations
                    .Where(c => c.User1Id == userId || c.User2Id == userId)
                    .ToListAsync();

                foreach (var conversation in conversations)
                {
                    if (conversation.User1Id == userId)
                    {
                        conversation.User1Name = anonymizedName;
                    }
                    if (conversation.User2Id == userId)
                    {
                        conversation.User2Name = anonymizedName;
                    }
                }
                affectedRecords["Conversations"] = conversations.Count;

                // 11. Anonymize Complaints
                var complaints = await _context.Complaints
                    .Where(c => c.ComplainantId == userId || c.SubjectId == userId)
                    .ToListAsync();

                foreach (var complaint in complaints)
                {
                    if (complaint.ComplainantId == userId)
                    {
                        complaint.ComplainantName = anonymizedName;
                        complaint.ComplainantEmail = anonymizedEmail;
                    }
                    if (complaint.SubjectId == userId)
                    {
                        complaint.SubjectName = anonymizedName;
                    }
                }
                affectedRecords["Complaints"] = complaints.Count;

                // 12. Anonymize Notifications
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .ToListAsync();

                foreach (var notification in notifications)
                {
                    notification.Data = null; // Remove any PII in notification data
                }
                affectedRecords["Notifications"] = notifications.Count;

                // 13. Anonymize Activity Logs
                var activityLogs = await _context.ActivityLogs
                    .Where(a => a.UserId == userId)
                    .ToListAsync();

                foreach (var log in activityLogs)
                {
                    log.UserName = anonymizedName;
                    log.UserEmail = anonymizedEmail;
                    log.MetadataJson = null; // Remove metadata that might contain PII
                }
                affectedRecords["ActivityLogs"] = activityLogs.Count;

                // 14. Delete sensitive documents and vehicles
                if (driver != null)
                {
                    var documents = await _context.DriverDocuments
                        .Where(d => d.DriverId == driver.Id)
                        .ToListAsync();
                    _context.DriverDocuments.RemoveRange(documents);
                    affectedRecords["DriverDocuments"] = documents.Count;

                    var vehicles = await _context.Vehicles
                        .Where(v => v.DriverId == driver.Id)
                        .ToListAsync();

                    foreach (var vehicle in vehicles)
                    {
                        vehicle.RegistrationNumber = $"ANON-{anonymizedId}";
                        vehicle.InsuranceProvider = null;
                        vehicle.InsurancePolicyNumber = null;
                    }
                    affectedRecords["Vehicles"] = vehicles.Count;
                }

                // 15. Delete saved addresses and favorites
                var savedAddresses = await _context.SavedAddresses
                    .Where(sa => sa.CustomerId == userId)
                    .ToListAsync();
                _context.SavedAddresses.RemoveRange(savedAddresses);
                affectedRecords["SavedAddresses"] = savedAddresses.Count;

                var favoriteDrivers = await _context.FavoriteDrivers
                    .Where(fd => fd.CustomerId == userId)
                    .ToListAsync();
                _context.FavoriteDrivers.RemoveRange(favoriteDrivers);
                affectedRecords["FavoriteDrivers"] = favoriteDrivers.Count;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Log the anonymization
                await _activityLogService.LogActivityAsync(
                    action: "user_data_anonymized",
                    entityType: "User",
                    entityId: userId,
                    entityName: user?.UserName ?? user?.Email ?? userId,
                    description: $"User data anonymized (GDPR compliance)",
                    severity: "WARNING",
                    userId: "SYSTEM",
                    userName: "System",
                    userEmail: "system@platform.com",
                    metadata: new Dictionary<string, object>
                    {
                        ["AnonymizedEmail"] = anonymizedEmail,
                        ["AffectedRecords"] = affectedRecords
                    }
                );

                return (true, null, affectedRecords);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error anonymizing user data for user {UserId}", userId);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AnonymizeUserDataAsync for user {UserId}", userId);
            return (false, ex.Message, affectedRecords);
        }
    }

    public async Task<(bool success, string? errorMessage, object? data)> ExportUserDataAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return (false, "User not found", null);

            var exportData = new Dictionary<string, object>
            {
                ["ExportDate"] = DateTime.UtcNow,
                ["UserId"] = userId,
                ["PersonalInfo"] = new
                {
                    Email = user.Email,
                    UserName = user.UserName,
                    DisplayName = user.DisplayName,
                    PhoneNumber = user.PhoneNumber,
                    CreatedAt = user.CreatedAt
                }
            };

            // Get Customer data
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer != null)
            {
                exportData["CustomerProfile"] = customer;

                var savedAddresses = await _context.SavedAddresses
                    .Where(sa => sa.CustomerId == userId)
                    .ToListAsync();
                exportData["SavedAddresses"] = savedAddresses;

                var favoriteDrivers = await _context.FavoriteDrivers
                    .Where(fd => fd.CustomerId == userId)
                    .ToListAsync();
                exportData["FavoriteDrivers"] = favoriteDrivers;
            }

            // Get Driver data
            var driver = await _context.Drivers
                .Include(d => d.Documents)
                .Include(d => d.Vehicles)
                .FirstOrDefaultAsync(d => d.UserId == userId);
            if (driver != null)
            {
                exportData["DriverProfile"] = driver;
                exportData["DriverDocuments"] = driver.Documents;
                exportData["Vehicles"] = driver.Vehicles;

                var earnings = await _context.Earnings
                    .Where(e => e.DriverId == driver.Id)
                    .ToListAsync();
                exportData["Earnings"] = earnings;
            }

            // Get Jobs
            var customerJobs = await _context.Jobs
                .Where(j => j.CustomerId == userId)
                .ToListAsync();
            exportData["JobsAsCustomer"] = customerJobs;

            if (driver != null)
            {
                var driverJobs = await _context.Jobs
                    .Where(j => j.DriverId == driver.Id)
                    .ToListAsync();
                exportData["JobsAsDriver"] = driverJobs;
            }

            // Get Reviews
            var reviewsGiven = await _context.Reviews
                .Where(r => r.ReviewerId == userId)
                .ToListAsync();
            exportData["ReviewsGiven"] = reviewsGiven;

            var reviewsReceived = await _context.Reviews
                .Where(r => r.RevieweeId == userId)
                .ToListAsync();
            exportData["ReviewsReceived"] = reviewsReceived;

            // Get Payments
            var payments = await _context.Payments
                .Where(p => p.CustomerId == userId || (driver != null && p.DriverId == driver.Id))
                .ToListAsync();
            exportData["Payments"] = payments;

            // Get Support Tickets
            var tickets = await _context.SupportTickets
                .Where(t => t.UserId == userId)
                .Include(t => _context.TicketMessages.Where(m => m.TicketId == t.Id))
                .ToListAsync();
            exportData["SupportTickets"] = tickets;

            // Get Messages
            var messages = await _context.ChatMessages
                .Where(m => m.SenderId == userId || m.RecipientId == userId)
                .ToListAsync();
            exportData["Messages"] = messages;

            // Get Complaints
            var complaints = await _context.Complaints
                .Where(c => c.ComplainantId == userId || c.SubjectId == userId)
                .ToListAsync();
            exportData["Complaints"] = complaints;

            // Get Notifications
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .ToListAsync();
            exportData["Notifications"] = notifications;

            // Get Activity Logs
            var activityLogs = await _context.ActivityLogs
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.Timestamp)
                .Take(1000) // Limit to last 1000 activities
                .ToListAsync();
            exportData["ActivityLogs"] = activityLogs;

            return (true, null, exportData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting user data for user {UserId}", userId);
            return (false, ex.Message, null);
        }
    }

    public async Task<(bool success, string? errorMessage)> HardDeleteUserDataAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return (false, "User not found");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Delete in reverse order of dependencies
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
                var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

                // Delete saved addresses, favorites
                if (customer != null)
                {
                    var savedAddresses = await _context.SavedAddresses.Where(sa => sa.CustomerId == userId).ToListAsync();
                    _context.SavedAddresses.RemoveRange(savedAddresses);

                    var favoriteDrivers = await _context.FavoriteDrivers.Where(fd => fd.CustomerId == userId).ToListAsync();
                    _context.FavoriteDrivers.RemoveRange(favoriteDrivers);

                    _context.Customers.Remove(customer);
                }

                // Delete driver-related data
                if (driver != null)
                {
                    var documents = await _context.DriverDocuments.Where(d => d.DriverId == driver.Id).ToListAsync();
                    _context.DriverDocuments.RemoveRange(documents);

                    var vehicles = await _context.Vehicles.Where(v => v.DriverId == driver.Id).ToListAsync();
                    _context.Vehicles.RemoveRange(vehicles);

                    var earnings = await _context.Earnings.Where(e => e.DriverId == driver.Id).ToListAsync();
                    _context.Earnings.RemoveRange(earnings);

                    _context.Drivers.Remove(driver);
                }

                // Delete notifications, activity logs
                var notifications = await _context.Notifications.Where(n => n.UserId == userId).ToListAsync();
                _context.Notifications.RemoveRange(notifications);

                var activityLogs = await _context.ActivityLogs.Where(a => a.UserId == userId).ToListAsync();
                _context.ActivityLogs.RemoveRange(activityLogs);

                // Delete user
                await _userManager.DeleteAsync(user);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in hard delete transaction for user {UserId}", userId);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in HardDeleteUserDataAsync for user {UserId}", userId);
            return (false, ex.Message);
        }
    }

    public async Task<Dictionary<string, int>> GetUserDataSummaryAsync(string userId)
    {
        var summary = new Dictionary<string, int>();

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        summary["Jobs_Customer"] = await _context.Jobs.CountAsync(j => j.CustomerId == userId);
        summary["Jobs_Driver"] = driver != null ? await _context.Jobs.CountAsync(j => j.DriverId == driver.Id) : 0;
        summary["Reviews"] = await _context.Reviews.CountAsync(r => r.ReviewerId == userId || r.RevieweeId == userId);
        summary["Payments"] = await _context.Payments.CountAsync(p => p.CustomerId == userId || (driver != null && p.DriverId == driver.Id));
        summary["SupportTickets"] = await _context.SupportTickets.CountAsync(t => t.UserId == userId);
        summary["Messages"] = await _context.ChatMessages.CountAsync(m => m.SenderId == userId || m.RecipientId == userId);
        summary["Complaints"] = await _context.Complaints.CountAsync(c => c.ComplainantId == userId || c.SubjectId == userId);
        summary["Notifications"] = await _context.Notifications.CountAsync(n => n.UserId == userId);
        summary["ActivityLogs"] = await _context.ActivityLogs.CountAsync(a => a.UserId == userId);

        if (customer != null)
        {
            summary["SavedAddresses"] = await _context.SavedAddresses.CountAsync(sa => sa.CustomerId == userId);
            summary["FavoriteDrivers"] = await _context.FavoriteDrivers.CountAsync(fd => fd.CustomerId == userId);
        }

        if (driver != null)
        {
            summary["DriverDocuments"] = await _context.DriverDocuments.CountAsync(d => d.DriverId == driver.Id);
            summary["Vehicles"] = await _context.Vehicles.CountAsync(v => v.DriverId == driver.Id);
            summary["Earnings"] = await _context.Earnings.CountAsync(e => e.DriverId == driver.Id);
        }

        return summary;
    }
}
