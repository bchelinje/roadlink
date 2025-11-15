using BeC.OpenId.Connect.Features.Documents.Models;
using BeC.OpenId.Connect.Features.Documents.ViewModels;

namespace BeC.OpenId.Connect.Features.Documents.Services.Interfaces;

/// <summary>
/// Service for managing driver documents
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Get documents for a specific driver by user ID
    /// </summary>
    Task<List<DocumentViewModel>> GetDriverDocumentsByUserIdAsync(string userId);

    /// <summary>
    /// Upload a document for a driver
    /// </summary>
    Task<DocumentViewModel> UploadDocumentAsync(UploadDocumentModel model, string userId);

    /// <summary>
    /// Get document by ID
    /// </summary>
    Task<DocumentViewModel?> GetDocumentByIdAsync(Guid id);

    /// <summary>
    /// Delete a document
    /// </summary>
    Task<(bool success, string? errorMessage)> DeleteDocumentAsync(Guid id, string userId);

    /// <summary>
    /// Get pending documents with pagination (Admin)
    /// </summary>
    Task<DocumentListViewModel> GetPendingDocumentsAsync(int page, int pageSize);

    /// <summary>
    /// Verify a document (Admin)
    /// </summary>
    Task<(bool success, DocumentViewModel? document, string? errorMessage)> VerifyDocumentAsync(Guid id, string userId);

    /// <summary>
    /// Reject a document (Admin)
    /// </summary>
    Task<(bool success, DocumentViewModel? document, string? errorMessage)> RejectDocumentAsync(Guid id, string userId, RejectDocumentModel? model);

    /// <summary>
    /// Get documents expiring soon (Admin)
    /// </summary>
    Task<List<DocumentViewModel>> GetExpiringDocumentsAsync(int daysAhead);

    /// <summary>
    /// Get all documents for a specific driver by driver ID (Admin)
    /// </summary>
    Task<List<DocumentViewModel>> GetDriverDocumentsByDriverIdAsync(Guid driverId);

    /// <summary>
    /// Get document statistics (Admin)
    /// </summary>
    Task<DocumentStatisticsViewModel> GetDocumentStatisticsAsync();

    /// <summary>
    /// Check if user has access to a document
    /// </summary>
    Task<bool> CanUserAccessDocumentAsync(Guid documentId, string userId, IEnumerable<string> userRoles);
}
