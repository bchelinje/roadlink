-- Fix migration history table to match current database state
-- This script marks all existing migrations as applied

-- First, check if migration history table exists
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END

-- Clear any existing records (in case of partial updates)
DELETE FROM [__EFMigrationsHistory];

-- Insert all existing migrations that have already been applied to the database
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES
    ('20251106162034_InitialCreate', '9.0.0'),
    ('20251110092453_AddVehicleTypeRequiredToJob', '9.0.0'),
    ('20251112140702_AddEarningsTable', '9.0.0'),
    ('20251112233210_AddReviewsTable', '9.0.0'),
    ('20251116005401_newFields', '9.0.0'),
    ('20251116010109_AddNewFields', '9.0.0');

-- Verify the records were inserted
SELECT [MigrationId], [ProductVersion]
FROM [__EFMigrationsHistory]
ORDER BY [MigrationId];

PRINT 'Migration history has been fixed. You can now run: dotnet ef database update';
