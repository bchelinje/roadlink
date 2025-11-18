// Run this with: dotnet script FixMigrations.cs
// Or add it to a temporary console app

using Microsoft.Data.SqlClient;

var connectionString = "YOUR_CONNECTION_STRING_HERE"; // Get from appsettings.json

var sql = @"
-- Clear any existing records
DELETE FROM [__EFMigrationsHistory];

-- Insert all existing migrations
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES
    ('20251106162034_InitialCreate', '9.0.0'),
    ('20251110092453_AddVehicleTypeRequiredToJob', '9.0.0'),
    ('20251112140702_AddEarningsTable', '9.0.0'),
    ('20251112233210_AddReviewsTable', '9.0.0'),
    ('20251116005401_newFields', '9.0.0'),
    ('20251116010109_AddNewFields', '9.0.0');
";

using (var connection = new SqlConnection(connectionString))
{
    connection.Open();
    using (var command = new SqlCommand(sql, connection))
    {
        command.ExecuteNonQuery();
        Console.WriteLine("✓ Migration history fixed successfully!");
    }
}

// Verify
using (var connection = new SqlConnection(connectionString))
{
    connection.Open();
    using (var command = new SqlCommand("SELECT [MigrationId] FROM [__EFMigrationsHistory] ORDER BY [MigrationId]", connection))
    {
        using (var reader = command.ExecuteReader())
        {
            Console.WriteLine("\nMigrations in history:");
            while (reader.Read())
            {
                Console.WriteLine($"  - {reader.GetString(0)}");
            }
        }
    }
}

Console.WriteLine("\nYou can now run: dotnet ef database update");
