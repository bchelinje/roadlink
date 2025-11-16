using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeC.OpenId.Connect.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedJobManagementFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add SMS columns to Notifications table
            migrationBuilder.AddColumn<bool>(
                name: "SendSms",
                table: "Notifications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SmsSent",
                table: "Notifications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SmsSentAt",
                table: "Notifications",
                type: "datetime2",
                nullable: true);

            // Add moderation columns to Reviews table
            migrationBuilder.AddColumn<string>(
                name: "RespondedBy",
                table: "Reviews",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModeratorNotes",
                table: "Reviews",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "Reviews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModeratedBy",
                table: "Reviews",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModeratedDate",
                table: "Reviews",
                type: "datetime2",
                nullable: true);

            // Create NotificationPreferences table
            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SmsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PushEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    JobAssignedEmail = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    JobAssignedSms = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    JobAssignedPush = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    JobCompletedEmail = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    JobCompletedSms = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    JobCompletedPush = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    JobCancelledEmail = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    JobCancelledSms = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    JobCancelledPush = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    JobRescheduledEmail = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    JobRescheduledSms = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    JobRescheduledPush = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PaymentReceivedEmail = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PaymentReceivedSms = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PaymentReceivedPush = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PayoutProcessedEmail = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PayoutProcessedSms = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PayoutProcessedPush = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RefundProcessedEmail = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RefundProcessedSms = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RefundProcessedPush = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ReviewReceivedEmail = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ReviewReceivedSms = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ReviewReceivedPush = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ReviewResponseEmail = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ReviewResponseSms = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ReviewResponsePush = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SystemAlertsEmail = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SystemAlertsSms = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SystemAlertsPush = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AccountUpdatesEmail = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AccountUpdatesSms = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AccountUpdatesPush = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PromotionalEmail = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PromotionalSms = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PromotionalPush = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    EnableQuietHours = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    QuietHoursStart = table.Column<TimeSpan>(type: "time", nullable: true),
                    QuietHoursEnd = table.Column<TimeSpan>(type: "time", nullable: true),
                    EnableEmailDigest = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DigestFrequency = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create SavedAddresses table
            migrationBuilder.CreateTable(
                name: "SavedAddresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    County = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    SpecialInstructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedAddresses", x => x.Id);
                });

            // Create JobStops table
            migrationBuilder.CreateTable(
                name: "JobStops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StopOrder = table.Column<int>(type: "int", nullable: false),
                    StopType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SpecialInstructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Items = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScheduledArrival = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualArrival = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualDeparture = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Photos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Signature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobStops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobStops_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create RecurringJobs table
            migrationBuilder.CreateTable(
                name: "RecurringJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    JobType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VehicleTypeRequired = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PickupLocation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DeliveryLocation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Distance = table.Column<double>(type: "float", nullable: true),
                    Items = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpecialInstructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Frequency = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RecurrenceDays = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DayOfMonth = table.Column<int>(type: "int", nullable: true),
                    PreferredTime = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OccurrenceCount = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LastGeneratedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    JobsCreated = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringJobs", x => x.Id);
                });

            // Create JobTemplates table
            migrationBuilder.CreateTable(
                name: "JobTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TemplateName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    JobType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VehicleTypeRequired = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PickupLocation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PickupLatitude = table.Column<double>(type: "float", nullable: true),
                    PickupLongitude = table.Column<double>(type: "float", nullable: true),
                    DeliveryLocation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DeliveryLatitude = table.Column<double>(type: "float", nullable: true),
                    DeliveryLongitude = table.Column<double>(type: "float", nullable: true),
                    EstimatedDistance = table.Column<double>(type: "float", nullable: true),
                    EstimatedDuration = table.Column<int>(type: "int", nullable: true),
                    Items = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpecialInstructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CustomerNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StopsConfiguration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BasePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TimesUsed = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastUsedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsShared = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTemplates", x => x.Id);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_UserId",
                table: "NotificationPreferences",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedAddresses_CustomerId",
                table: "SavedAddresses",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedAddresses_IsDefault",
                table: "SavedAddresses",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_JobStops_JobId",
                table: "JobStops",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobStops_StopOrder",
                table: "JobStops",
                column: "StopOrder");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringJobs_CustomerId",
                table: "RecurringJobs",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringJobs_Status",
                table: "RecurringJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringJobs_NextScheduledDate",
                table: "RecurringJobs",
                column: "NextScheduledDate");

            migrationBuilder.CreateIndex(
                name: "IX_JobTemplates_CustomerId",
                table: "JobTemplates",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTemplates_Status",
                table: "JobTemplates",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_JobTemplates_IsDefault",
                table: "JobTemplates",
                column: "IsDefault");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop tables
            migrationBuilder.DropTable(name: "NotificationPreferences");
            migrationBuilder.DropTable(name: "SavedAddresses");
            migrationBuilder.DropTable(name: "JobStops");
            migrationBuilder.DropTable(name: "RecurringJobs");
            migrationBuilder.DropTable(name: "JobTemplates");

            // Drop columns from Notifications
            migrationBuilder.DropColumn(name: "SendSms", table: "Notifications");
            migrationBuilder.DropColumn(name: "SmsSent", table: "Notifications");
            migrationBuilder.DropColumn(name: "SmsSentAt", table: "Notifications");

            // Drop columns from Reviews
            migrationBuilder.DropColumn(name: "RespondedBy", table: "Reviews");
            migrationBuilder.DropColumn(name: "ModeratorNotes", table: "Reviews");
            migrationBuilder.DropColumn(name: "IsHidden", table: "Reviews");
            migrationBuilder.DropColumn(name: "ModeratedBy", table: "Reviews");
            migrationBuilder.DropColumn(name: "ModeratedDate", table: "Reviews");
        }
    }
}
