-- ============================================================================
-- Database Schema Update Script
-- This script updates the database to support all new features:
-- - Reviews table schema update
-- - SMS support in Notifications
-- - Notification preferences
-- - Saved addresses
-- - Multi-stop deliveries (Job stops)
-- - Recurring jobs
-- - Job templates
-- ============================================================================

BEGIN TRANSACTION;

-- ============================================================================
-- 1. UPDATE REVIEWS TABLE SCHEMA
-- ============================================================================
PRINT 'Updating Reviews table schema...';

-- Drop existing foreign keys and indexes
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Reviews_Drivers_DriverId')
    ALTER TABLE [Reviews] DROP CONSTRAINT [FK_Reviews_Drivers_DriverId];

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Reviews_Jobs_JobId')
    ALTER TABLE [Reviews] DROP CONSTRAINT [FK_Reviews_Jobs_JobId];

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Reviews_DriverId' AND object_id = OBJECT_ID('Reviews'))
    DROP INDEX [IX_Reviews_DriverId] ON [Reviews];

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Reviews_JobId' AND object_id = OBJECT_ID('Reviews'))
    DROP INDEX [IX_Reviews_JobId] ON [Reviews];

-- Drop old columns if they exist
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reviews') AND name = 'CustomerEmail')
    ALTER TABLE [Reviews] DROP COLUMN [CustomerEmail];

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reviews') AND name = 'CustomerName')
    ALTER TABLE [Reviews] DROP COLUMN [CustomerName];

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reviews') AND name = 'DriverId')
    ALTER TABLE [Reviews] DROP COLUMN [DriverId];

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reviews') AND name = 'PunctualityRating')
    ALTER TABLE [Reviews] DROP COLUMN [PunctualityRating];

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reviews') AND name = 'ProfessionalismRating')
    ALTER TABLE [Reviews] DROP COLUMN [ProfessionalismRating];

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reviews') AND name = 'CareOfItemsRating')
    ALTER TABLE [Reviews] DROP COLUMN [CareOfItemsRating];

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reviews') AND name = 'CommunicationRating')
    ALTER TABLE [Reviews] DROP COLUMN [CommunicationRating];

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reviews') AND name = 'HelpfulVotes')
    ALTER TABLE [Reviews] DROP COLUMN [HelpfulVotes];

-- Rename DriverResponse to Response if it exists
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reviews') AND name = 'DriverResponse')
    AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reviews') AND name = 'Response')
BEGIN
    EXEC sp_rename 'Reviews.DriverResponse', 'Response', 'COLUMN';
    ALTER TABLE [Reviews] ALTER COLUMN [Response] NVARCHAR(1000) NULL;
END

-- Make JobId nullable
ALTER TABLE [Reviews] ALTER COLUMN [JobId] UNIQUEIDENTIFIER NULL;

-- Add new columns if they don't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reviews') AND name = 'ReviewerId')
    ALTER TABLE [Reviews] ADD [ReviewerId] NVARCHAR(450) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reviews') AND name = 'ReviewerName')
    ALTER TABLE [Reviews] ADD [ReviewerName] NVARCHAR(255) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reviews') AND name = 'ReviewerType')
    ALTER TABLE [Reviews] ADD [ReviewerType] NVARCHAR(50) NOT NULL DEFAULT 'customer';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reviews') AND name = 'RevieweeId')
    ALTER TABLE [Reviews] ADD [RevieweeId] NVARCHAR(450) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reviews') AND name = 'RevieweeName')
    ALTER TABLE [Reviews] ADD [RevieweeName] NVARCHAR(255) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reviews') AND name = 'RevieweeType')
    ALTER TABLE [Reviews] ADD [RevieweeType] NVARCHAR(50) NOT NULL DEFAULT 'driver';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reviews') AND name = 'Photos')
    ALTER TABLE [Reviews] ADD [Photos] NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reviews') AND name = 'RespondedBy')
    ALTER TABLE [Reviews] ADD [RespondedBy] NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reviews') AND name = 'IsFlagged')
    ALTER TABLE [Reviews] ADD [IsFlagged] BIT NOT NULL DEFAULT 0;

-- Handle FlagReason - drop if exists and recreate to avoid conflicts
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reviews') AND name = 'FlagReason')
    ALTER TABLE [Reviews] DROP COLUMN [FlagReason];
ALTER TABLE [Reviews] ADD [FlagReason] NVARCHAR(500) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reviews') AND name = 'FlaggedBy')
    ALTER TABLE [Reviews] ADD [FlaggedBy] NVARCHAR(450) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reviews') AND name = 'FlaggedDate')
    ALTER TABLE [Reviews] ADD [FlaggedDate] DATETIME2 NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reviews') AND name = 'ModeratorNotes')
    ALTER TABLE [Reviews] ADD [ModeratorNotes] NVARCHAR(1000) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reviews') AND name = 'IsHidden')
    ALTER TABLE [Reviews] ADD [IsHidden] BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reviews') AND name = 'ModeratedBy')
    ALTER TABLE [Reviews] ADD [ModeratedBy] NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reviews') AND name = 'ModeratedDate')
    ALTER TABLE [Reviews] ADD [ModeratedDate] DATETIME2 NULL;

-- Recreate foreign key for JobId
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Reviews_Jobs_JobId')
    ALTER TABLE [Reviews] ADD CONSTRAINT [FK_Reviews_Jobs_JobId]
    FOREIGN KEY ([JobId]) REFERENCES [Jobs]([Id]) ON DELETE SET NULL;

-- Recreate index
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Reviews_JobId' AND object_id = OBJECT_ID('Reviews'))
    CREATE INDEX [IX_Reviews_JobId] ON [Reviews]([JobId]);

PRINT 'Reviews table updated successfully.';

-- ============================================================================
-- 2. UPDATE NOTIFICATIONS TABLE - ADD SMS SUPPORT
-- ============================================================================
PRINT 'Adding SMS support to Notifications table...';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Notifications') AND name = 'SendSms')
    ALTER TABLE [Notifications] ADD [SendSms] BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Notifications') AND name = 'SmsSent')
    ALTER TABLE [Notifications] ADD [SmsSent] BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Notifications') AND name = 'SmsSentAt')
    ALTER TABLE [Notifications] ADD [SmsSentAt] DATETIME2 NULL;

PRINT 'Notifications table updated successfully.';

-- ============================================================================
-- 3. CREATE NOTIFICATION PREFERENCES TABLE
-- ============================================================================
PRINT 'Creating NotificationPreferences table...';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'NotificationPreferences')
BEGIN
    CREATE TABLE [NotificationPreferences] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [UserId] NVARCHAR(450) NOT NULL,
        [EmailEnabled] BIT NOT NULL DEFAULT 1,
        [SmsEnabled] BIT NOT NULL DEFAULT 0,
        [PushEnabled] BIT NOT NULL DEFAULT 1,
        [JobAssignedEmail] BIT NOT NULL DEFAULT 1,
        [JobAssignedSms] BIT NOT NULL DEFAULT 0,
        [JobAssignedPush] BIT NOT NULL DEFAULT 1,
        [JobCompletedEmail] BIT NOT NULL DEFAULT 1,
        [JobCompletedSms] BIT NOT NULL DEFAULT 0,
        [JobCompletedPush] BIT NOT NULL DEFAULT 1,
        [JobCancelledEmail] BIT NOT NULL DEFAULT 1,
        [JobCancelledSms] BIT NOT NULL DEFAULT 1,
        [JobCancelledPush] BIT NOT NULL DEFAULT 1,
        [JobRescheduledEmail] BIT NOT NULL DEFAULT 1,
        [JobRescheduledSms] BIT NOT NULL DEFAULT 0,
        [JobRescheduledPush] BIT NOT NULL DEFAULT 1,
        [PaymentReceivedEmail] BIT NOT NULL DEFAULT 1,
        [PaymentReceivedSms] BIT NOT NULL DEFAULT 0,
        [PaymentReceivedPush] BIT NOT NULL DEFAULT 1,
        [PayoutProcessedEmail] BIT NOT NULL DEFAULT 1,
        [PayoutProcessedSms] BIT NOT NULL DEFAULT 1,
        [PayoutProcessedPush] BIT NOT NULL DEFAULT 1,
        [RefundProcessedEmail] BIT NOT NULL DEFAULT 1,
        [RefundProcessedSms] BIT NOT NULL DEFAULT 0,
        [RefundProcessedPush] BIT NOT NULL DEFAULT 1,
        [ReviewReceivedEmail] BIT NOT NULL DEFAULT 1,
        [ReviewReceivedSms] BIT NOT NULL DEFAULT 0,
        [ReviewReceivedPush] BIT NOT NULL DEFAULT 1,
        [ReviewResponseEmail] BIT NOT NULL DEFAULT 1,
        [ReviewResponseSms] BIT NOT NULL DEFAULT 0,
        [ReviewResponsePush] BIT NOT NULL DEFAULT 1,
        [SystemAlertsEmail] BIT NOT NULL DEFAULT 1,
        [SystemAlertsSms] BIT NOT NULL DEFAULT 0,
        [SystemAlertsPush] BIT NOT NULL DEFAULT 1,
        [AccountUpdatesEmail] BIT NOT NULL DEFAULT 1,
        [AccountUpdatesSms] BIT NOT NULL DEFAULT 0,
        [AccountUpdatesPush] BIT NOT NULL DEFAULT 0,
        [PromotionalEmail] BIT NOT NULL DEFAULT 1,
        [PromotionalSms] BIT NOT NULL DEFAULT 0,
        [PromotionalPush] BIT NOT NULL DEFAULT 0,
        [EnableQuietHours] BIT NOT NULL DEFAULT 0,
        [QuietHoursStart] TIME NULL,
        [QuietHoursEnd] TIME NULL,
        [EnableEmailDigest] BIT NOT NULL DEFAULT 0,
        [DigestFrequency] NVARCHAR(20) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [FK_NotificationPreferences_AspNetUsers_UserId]
            FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX [IX_NotificationPreferences_UserId] ON [NotificationPreferences]([UserId]);
    PRINT 'NotificationPreferences table created successfully.';
END
ELSE
    PRINT 'NotificationPreferences table already exists.';

-- ============================================================================
-- 4. CREATE SAVED ADDRESSES TABLE
-- ============================================================================
PRINT 'Creating SavedAddresses table...';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SavedAddresses')
BEGIN
    CREATE TABLE [SavedAddresses] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [CustomerId] NVARCHAR(450) NOT NULL,
        [Label] NVARCHAR(100) NOT NULL,
        [AddressLine1] NVARCHAR(200) NOT NULL,
        [AddressLine2] NVARCHAR(200) NULL,
        [City] NVARCHAR(100) NOT NULL,
        [County] NVARCHAR(100) NULL,
        [PostalCode] NVARCHAR(20) NOT NULL,
        [Country] NVARCHAR(50) NOT NULL DEFAULT 'UK',
        [Latitude] FLOAT NULL,
        [Longitude] FLOAT NULL,
        [SpecialInstructions] NVARCHAR(1000) NULL,
        [IsDefault] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE INDEX [IX_SavedAddresses_CustomerId] ON [SavedAddresses]([CustomerId]);
    CREATE INDEX [IX_SavedAddresses_IsDefault] ON [SavedAddresses]([IsDefault]);
    PRINT 'SavedAddresses table created successfully.';
END
ELSE
    PRINT 'SavedAddresses table already exists.';

-- ============================================================================
-- 5. CREATE JOB STOPS TABLE (Multi-Stop Deliveries)
-- ============================================================================
PRINT 'Creating JobStops table...';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'JobStops')
BEGIN
    CREATE TABLE [JobStops] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [JobId] UNIQUEIDENTIFIER NOT NULL,
        [StopOrder] INT NOT NULL,
        [StopType] NVARCHAR(20) NOT NULL,
        [Location] NVARCHAR(500) NOT NULL,
        [Latitude] FLOAT NULL,
        [Longitude] FLOAT NULL,
        [ContactName] NVARCHAR(100) NULL,
        [ContactPhone] NVARCHAR(20) NULL,
        [SpecialInstructions] NVARCHAR(1000) NULL,
        [Items] NVARCHAR(MAX) NULL,
        [ScheduledArrival] DATETIME2 NULL,
        [ActualArrival] DATETIME2 NULL,
        [ActualDeparture] DATETIME2 NULL,
        [Status] NVARCHAR(20) NOT NULL DEFAULT 'pending',
        [Photos] NVARCHAR(MAX) NULL,
        [Signature] NVARCHAR(MAX) NULL,
        [Notes] NVARCHAR(1000) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [FK_JobStops_Jobs_JobId]
            FOREIGN KEY ([JobId]) REFERENCES [Jobs]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_JobStops_JobId] ON [JobStops]([JobId]);
    CREATE INDEX [IX_JobStops_StopOrder] ON [JobStops]([StopOrder]);
    PRINT 'JobStops table created successfully.';
END
ELSE
    PRINT 'JobStops table already exists.';

-- ============================================================================
-- 6. CREATE RECURRING JOBS TABLE
-- ============================================================================
PRINT 'Creating RecurringJobs table...';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RecurringJobs')
BEGIN
    CREATE TABLE [RecurringJobs] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [CustomerId] NVARCHAR(450) NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [JobType] NVARCHAR(50) NOT NULL,
        [VehicleTypeRequired] NVARCHAR(50) NULL,
        [Priority] NVARCHAR(20) NULL,
        [PickupLocation] NVARCHAR(500) NOT NULL,
        [DeliveryLocation] NVARCHAR(500) NOT NULL,
        [Distance] FLOAT NULL,
        [Items] NVARCHAR(MAX) NULL,
        [SpecialInstructions] NVARCHAR(1000) NULL,
        [Frequency] NVARCHAR(20) NOT NULL,
        [RecurrenceDays] NVARCHAR(MAX) NULL,
        [DayOfMonth] INT NULL,
        [PreferredTime] NVARCHAR(10) NULL,
        [StartDate] DATETIME2 NOT NULL,
        [EndDate] DATETIME2 NULL,
        [OccurrenceCount] INT NULL,
        [Status] NVARCHAR(20) NOT NULL DEFAULT 'active',
        [LastGeneratedDate] DATETIME2 NULL,
        [NextScheduledDate] DATETIME2 NULL,
        [JobsCreated] INT NOT NULL DEFAULT 0,
        [TemplateId] UNIQUEIDENTIFIER NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE INDEX [IX_RecurringJobs_CustomerId] ON [RecurringJobs]([CustomerId]);
    CREATE INDEX [IX_RecurringJobs_Status] ON [RecurringJobs]([Status]);
    CREATE INDEX [IX_RecurringJobs_NextScheduledDate] ON [RecurringJobs]([NextScheduledDate]);
    PRINT 'RecurringJobs table created successfully.';
END
ELSE
    PRINT 'RecurringJobs table already exists.';

-- ============================================================================
-- 7. CREATE JOB TEMPLATES TABLE
-- ============================================================================
PRINT 'Creating JobTemplates table...';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'JobTemplates')
BEGIN
    CREATE TABLE [JobTemplates] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [CustomerId] NVARCHAR(450) NOT NULL,
        [TemplateName] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [JobType] NVARCHAR(50) NOT NULL,
        [VehicleTypeRequired] NVARCHAR(50) NULL,
        [Priority] NVARCHAR(20) NULL,
        [PickupLocation] NVARCHAR(500) NOT NULL,
        [PickupLatitude] FLOAT NULL,
        [PickupLongitude] FLOAT NULL,
        [DeliveryLocation] NVARCHAR(500) NOT NULL,
        [DeliveryLatitude] FLOAT NULL,
        [DeliveryLongitude] FLOAT NULL,
        [EstimatedDistance] FLOAT NULL,
        [EstimatedDuration] INT NULL,
        [Items] NVARCHAR(MAX) NULL,
        [SpecialInstructions] NVARCHAR(1000) NULL,
        [CustomerNotes] NVARCHAR(1000) NULL,
        [StopsConfiguration] NVARCHAR(MAX) NULL,
        [BasePrice] DECIMAL(18,2) NULL,
        [TimesUsed] INT NOT NULL DEFAULT 0,
        [LastUsedDate] DATETIME2 NULL,
        [Tags] NVARCHAR(500) NULL,
        [Status] NVARCHAR(20) NOT NULL DEFAULT 'active',
        [IsDefault] BIT NOT NULL DEFAULT 0,
        [IsShared] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE INDEX [IX_JobTemplates_CustomerId] ON [JobTemplates]([CustomerId]);
    CREATE INDEX [IX_JobTemplates_Status] ON [JobTemplates]([Status]);
    CREATE INDEX [IX_JobTemplates_IsDefault] ON [JobTemplates]([IsDefault]);
    PRINT 'JobTemplates table created successfully.';
END
ELSE
    PRINT 'JobTemplates table already exists.';

-- ============================================================================
-- COMMIT TRANSACTION
-- ============================================================================
COMMIT TRANSACTION;
PRINT 'Database schema update completed successfully!';
