using BeC.OpenId.Connect.Features.ActivityLogs;
using BeC.OpenId.Connect.Features.Drivers.Dtos;
using BeC.OpenId.Connect.Features.Users.Dtos;
using BeC.OpenId.Connect.Features.Reviews.Dtos;
using BeC.OpenId.Connect.Features.Notifications.Dtos;
using BeC.OpenId.Connect.Features.Payments.Dtos;
using BeC.OpenId.Connect.Features.Pricing.Dtos;
using BeC.OpenId.Connect.Features.Location.Dtos;
using BeC.OpenId.Connect.Features.Customers.Dtos;
using BeC.OpenId.Connect.Features.Jobs.Dtos;
using BeC.OpenId.Connect.Features.Settings.Dtos;
using BeC.OpenId.Connect.Features.Messages.Dtos;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BeC.OpenId.Connect.Dto;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public virtual DbSet<ActivityLog> ActivityLogs { get; set; }
    public DbSet<Driver> Drivers { get; set; }
    public DbSet<Job> Jobs { get; set; }
    public DbSet<JobBid> JobBids { get; set; }
    public DbSet<DriverDocument> DriverDocuments { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Payout> Payouts { get; set; }
    public DbSet<PricingRule> PricingRules { get; set; }
    public DbSet<PricingHistory> PricingHistory { get; set; }
    public DbSet<PromotionCode> PromotionCodes { get; set; }
    public DbSet<PromotionCodeUsage> PromotionCodeUsages { get; set; }
    public DbSet<DriverLocation> DriverLocations { get; set; }
    public DbSet<Earning> Earnings { get; set; }
    public DbSet<SavedAddress> SavedAddresses { get; set; }
    public DbSet<FavoriteDriver> FavoriteDrivers { get; set; }
    public DbSet<NotificationPreferences> NotificationPreferences { get; set; }
    public DbSet<JobStop> JobStops { get; set; }
    public DbSet<RecurringJob> RecurringJobs { get; set; }
    public DbSet<JobTemplate> JobTemplates { get; set; }
    public DbSet<UserSettings> UserSettings { get; set; }
    public DbSet<DriverSettings> DriverSettings { get; set; }
    public DbSet<CustomerSettings> CustomerSettings { get; set; }
    public DbSet<PlatformSettings> PlatformSettings { get; set; }
    public DbSet<Message> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.UseOpenIddict();
        
        // NEW: Driver configuration
        builder.Entity<Driver>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.Rating)
                .HasPrecision(3, 2);

            entity.HasMany(e => e.Jobs)
                .WithOne(e => e.Driver)
                .HasForeignKey(e => e.DriverId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Documents)
                .WithOne(e => e.Driver)
                .HasForeignKey(e => e.DriverId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Vehicles)
                .WithOne(e => e.Driver)
                .HasForeignKey(e => e.DriverId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Job configuration
        builder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.JobNumber).IsUnique();
            entity.HasIndex(e => e.DriverId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ScheduledDate);
            entity.HasIndex(e => e.Priority);

            entity.Property(e => e.Distance)
                .HasPrecision(10, 2);
        });

        // JobBid configuration
        builder.Entity<JobBid>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.DriverId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.BidAmount)
                .HasPrecision(10, 2);

            entity.HasOne(e => e.Job)
                .WithMany()
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Driver)
                .WithMany()
                .HasForeignKey(e => e.DriverId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DriverDocument configuration
        builder.Entity<DriverDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DriverId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Status);
        });

        // Vehicle configuration
        builder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DriverId);
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.MaxPayloadWeight)
                .HasPrecision(10, 2);

            entity.Property(e => e.MaxGrossWeight)
                .HasPrecision(10, 2);

            entity.Property(e => e.CargoLength)
                .HasPrecision(10, 2);

            entity.Property(e => e.CargoWidth)
                .HasPrecision(10, 2);

            entity.Property(e => e.CargoHeight)
                .HasPrecision(10, 2);
        });

        // Review configuration
        builder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReviewerId);
            entity.HasIndex(e => e.RevieweeId);
            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Notification configuration
        builder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsRead);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Payment configuration
        builder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PaymentNumber).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.DriverId);
            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.Amount)
                .HasPrecision(10, 2);

            entity.Property(e => e.TipAmount)
                .HasPrecision(10, 2);

            entity.Property(e => e.PlatformFee)
                .HasPrecision(10, 2);

            entity.Property(e => e.DriverEarnings)
                .HasPrecision(10, 2);

            entity.Property(e => e.RefundAmount)
                .HasPrecision(10, 2);

            entity.Property(e => e.TotalAmount)
                .HasPrecision(10, 2);
        });

        // Payout configuration
        builder.Entity<Payout>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PayoutNumber).IsUnique();
            entity.HasIndex(e => e.DriverId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.Amount)
                .HasPrecision(10, 2);

            entity.HasOne(e => e.Driver)
                .WithMany()
                .HasForeignKey(e => e.DriverId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // PricingRule configuration
        builder.Entity<PricingRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.VehicleType);

            entity.Property(e => e.FixedAmount)
                .HasPrecision(10, 2);

            entity.Property(e => e.PerMileRate)
                .HasPrecision(10, 4);

            entity.Property(e => e.PerMinuteRate)
                .HasPrecision(10, 4);

            entity.Property(e => e.MultiplierPercentage)
                .HasPrecision(5, 2);
        });

        // PricingHistory configuration
        builder.Entity<PricingHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.BaseFare)
                .HasPrecision(10, 2);

            entity.Property(e => e.DistanceCharge)
                .HasPrecision(10, 2);

            entity.Property(e => e.TimeCharge)
                .HasPrecision(10, 2);

            entity.Property(e => e.VehicleTypeCharge)
                .HasPrecision(10, 2);

            entity.Property(e => e.ServiceAddonsCharge)
                .HasPrecision(10, 2);

            entity.Property(e => e.SurgeMultiplier)
                .HasPrecision(5, 2);

            entity.Property(e => e.TotalPrice)
                .HasPrecision(10, 2);
        });

        // PromotionCode configuration
        builder.Entity<PromotionCode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.ValidFrom);
            entity.HasIndex(e => e.ValidUntil);

            entity.Property(e => e.DiscountValue)
                .HasPrecision(10, 2);

            entity.Property(e => e.MaxDiscountAmount)
                .HasPrecision(10, 2);

            entity.Property(e => e.MinOrderValue)
                .HasPrecision(10, 2);
        });

        // PromotionCodeUsage configuration
        builder.Entity<PromotionCodeUsage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PromotionCodeId);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.UsedAt);

            entity.Property(e => e.OriginalAmount)
                .HasPrecision(10, 2);

            entity.Property(e => e.DiscountAmount)
                .HasPrecision(10, 2);

            entity.Property(e => e.FinalAmount)
                .HasPrecision(10, 2);

            entity.HasOne(e => e.PromotionCode)
                .WithMany()
                .HasForeignKey(e => e.PromotionCodeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // DriverLocation configuration
        builder.Entity<DriverLocation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DriverId);
            entity.HasIndex(e => e.CurrentJobId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.DriverId, e.Timestamp }); // Composite index for location history queries
        });

        // Earning configuration
        builder.Entity<Earning>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DriverId);
            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.PaymentStatus);
            entity.HasIndex(e => e.JobCompletedDate);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.BaseAmount)
                .HasPrecision(10, 2);

            entity.Property(e => e.BonusAmount)
                .HasPrecision(10, 2);

            entity.Property(e => e.TipAmount)
                .HasPrecision(10, 2);

            entity.Property(e => e.DeductionAmount)
                .HasPrecision(10, 2);

            entity.Property(e => e.NetAmount)
                .HasPrecision(10, 2);

            entity.Property(e => e.JobDistance)
                .HasPrecision(10, 2);

            entity.HasOne(e => e.Driver)
                .WithMany()
                .HasForeignKey(e => e.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Job)
                .WithMany()
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // JobTemplate configuration
        builder.Entity<JobTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.BasePrice)
                .HasPrecision(10, 2);
        });

        // UserSettings configuration
        builder.Entity<UserSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
        });

        // DriverSettings configuration
        builder.Entity<DriverSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();

            entity.Property(e => e.MaxServiceRadiusMiles)
                .HasPrecision(10, 2);

            entity.Property(e => e.MinimumJobValue)
                .HasPrecision(10, 2);

            entity.Property(e => e.MaximumJobDistanceMiles)
                .HasPrecision(10, 2);

            entity.Property(e => e.MinimumPayoutAmount)
                .HasPrecision(5, 2);
        });

        // CustomerSettings configuration
        builder.Entity<CustomerSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();

            entity.Property(e => e.PreferredMaxDistance)
                .HasPrecision(10, 2);

            entity.Property(e => e.DefaultTipPercentage)
                .HasPrecision(5, 2);
        });

        // PlatformSettings configuration
        builder.Entity<PlatformSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SettingKey).IsUnique();
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsPublic);
        });

        // Message configuration
        builder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.SenderId);
            entity.HasIndex(e => e.ReceiverId);
            entity.HasIndex(e => e.IsRead);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.JobId, e.CreatedAt }); // Composite index for conversation queries
        });
    }
}