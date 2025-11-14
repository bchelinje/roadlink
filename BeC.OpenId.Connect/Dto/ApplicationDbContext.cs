using BeC.OpenId.Connect.Features.ActivityLogs;
using BeC.OpenId.Connect.Features.Drivers.Dtos;
using BeC.OpenId.Connect.Features.Users.Dtos;
using BeC.OpenId.Connect.Features.Reviews.Dtos;
using BeC.OpenId.Connect.Features.Notifications.Dtos;
using BeC.OpenId.Connect.Features.Payments.Dtos;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BeC.OpenId.Connect.Dto;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public virtual DbSet<ActivityLog> ActivityLogs { get; set; }
    public DbSet<Driver> Drivers { get; set; }
    public DbSet<Job> Jobs { get; set; }
    public DbSet<DriverDocument> DriverDocuments { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Payout> Payouts { get; set; }
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
    }
}