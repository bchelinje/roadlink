namespace BeC.OpenId.Connect.Features.DataPrivacy.Services;

public interface IDataAnonymizationService
{
    /// <summary>
    /// Anonymizes all user data across the platform (GDPR compliant)
    /// </summary>
    Task<(bool success, string? errorMessage, Dictionary<string, int> affectedRecords)> AnonymizeUserDataAsync(string userId);

    /// <summary>
    /// Exports all user data in a portable format (GDPR Article 20)
    /// </summary>
    Task<(bool success, string? errorMessage, object? data)> ExportUserDataAsync(string userId);

    /// <summary>
    /// Completely removes user data (hard delete - use with caution)
    /// </summary>
    Task<(bool success, string? errorMessage)> HardDeleteUserDataAsync(string userId);

    /// <summary>
    /// Gets a summary of all data related to a user
    /// </summary>
    Task<Dictionary<string, int>> GetUserDataSummaryAsync(string userId);
}
