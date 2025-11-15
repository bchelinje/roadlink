using AutoMapper;
using BeC.Common.Data.Repositories.Interfaces;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using BeC.OpenId.Connect.Features.Documents.Models;
using BeC.OpenId.Connect.Features.Documents.Services.Interfaces;
using BeC.OpenId.Connect.Features.Documents.ViewModels;
using BeC.OpenId.Connect.Features.Drivers.Dtos;
using BeC.OpenId.Connect.Infrastructure.Authorization;
using Microsoft.EntityFrameworkCore;
using AuthRoles = BeC.OpenId.Connect.Infrastructure.Authorization.Roles;

namespace BeC.OpenId.Connect.Features.Documents.Services;

/// <summary>
/// Implementation of document service
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IRepository _repository;
    private readonly IActivityLogService _activityLogService;
    private readonly IWebHostEnvironment _environment;
    private readonly IMapper _mapper;
    private readonly ILogger<DocumentService> _logger;

    private static readonly string[] AllowedTypes =
        { "drivers_license", "insurance", "vehicle_registration", "mot_certificate", "id_proof", "address_proof" };

    private static readonly string[] AllowedExtensions =
        { ".pdf", ".jpg", ".jpeg", ".png" };

    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

    public DocumentService(
        ApplicationDbContext context,
        IRepository repository,
        IActivityLogService activityLogService,
        IWebHostEnvironment environment,
        IMapper mapper,
        ILogger<DocumentService> logger)
    {
        _context = context;
        _repository = repository;
        _activityLogService = activityLogService;
        _environment = environment;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<DocumentViewModel>> GetDriverDocumentsByUserIdAsync(string userId)
    {
        var driver = await _repository.GetEntity<Driver>(d => d.UserId == userId);
        if (driver == null)
        {
            return new List<DocumentViewModel>();
        }

        var documents = await _repository.GetEntities<DriverDocument, DateTime>(
            d => d.DriverId == driver.Id,
            d => d.UploadedDate,
            isDescending: true
        );

        return _mapper.Map<List<DocumentViewModel>>(documents);
    }

    public async Task<DocumentViewModel> UploadDocumentAsync(UploadDocumentModel model, string userId)
    {
        var driver = await _repository.GetEntity<Driver>(d => d.UserId == userId);
        if (driver == null)
        {
            throw new InvalidOperationException("Driver profile not found");
        }

        // Validate file
        if (model.File == null || model.File.Length == 0)
        {
            throw new ArgumentException("No file uploaded");
        }

        if (!AllowedTypes.Contains(model.Type))
        {
            throw new ArgumentException($"Invalid document type. Allowed types: {string.Join(", ", AllowedTypes)}");
        }

        var extension = Path.GetExtension(model.File.FileName).ToLower();
        if (!AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException($"Invalid file type. Allowed: {string.Join(", ", AllowedExtensions)}");
        }

        if (model.File.Length > MaxFileSize)
        {
            throw new ArgumentException("File size must be less than 5MB");
        }

        // Generate unique filename
        var fileName = $"{driver.Id}_{model.Type}_{Guid.NewGuid()}{extension}";
        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "drivers", driver.Id.ToString());
        Directory.CreateDirectory(uploadsFolder);

        var filePath = Path.Combine(uploadsFolder, fileName);
        var fileUrl = $"/uploads/drivers/{driver.Id}/{fileName}";

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await model.File.CopyToAsync(stream);
        }

        var document = new DriverDocument
        {
            DriverId = driver.Id,
            Type = model.Type,
            FileName = model.File.FileName,
            FileUrl = fileUrl,
            ExpiryDate = model.ExpiryDate,
            Status = "pending"
        };

        await _repository.InsertEntity(document);

        await _activityLogService.LogActivityAsync(
            userId,
            "document_uploaded",
            "DriverDocument",
            document.Id.ToString(),
            model.Type,
            $"Driver uploaded {model.Type} document"
        );

        return _mapper.Map<DocumentViewModel>(document);
    }

    public async Task<DocumentViewModel?> GetDocumentByIdAsync(Guid id)
    {
        var document = await _context.DriverDocuments
            .Include(d => d.Driver)
            .FirstOrDefaultAsync(d => d.Id == id);

        return document != null ? _mapper.Map<DocumentViewModel>(document) : null;
    }

    public async Task<(bool success, string? errorMessage)> DeleteDocumentAsync(Guid id, string userId)
    {
        var driver = await _repository.GetEntity<Driver>(d => d.UserId == userId);
        if (driver == null)
        {
            return (false, "Driver profile not found");
        }

        var document = await _repository.GetEntity<DriverDocument>(
            d => d.Id == id && d.DriverId == driver.Id
        );

        if (document == null)
        {
            return (false, "Document not found");
        }

        // Cannot delete verified documents
        if (document.Status == "verified")
        {
            return (false, "Cannot delete verified documents. Please contact support.");
        }

        // Delete physical file
        var filePath = Path.Combine(_environment.WebRootPath, document.FileUrl.TrimStart('/'));
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        await _repository.RemoveEntity(document);

        await _activityLogService.LogActivityAsync(
            userId,
            "document_deleted",
            "DriverDocument",
            document.Id.ToString(),
            document.Type,
            $"Driver deleted {document.Type} document"
        );

        return (true, null);
    }

    public async Task<DocumentListViewModel> GetPendingDocumentsAsync(int page, int pageSize)
    {
        var query = _context.DriverDocuments
            .Include(d => d.Driver)
            .Where(d => d.Status == "pending");

        var totalCount = await query.CountAsync();
        var documents = await query
            .OrderBy(d => d.UploadedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new DocumentListViewModel
        {
            Documents = _mapper.Map<List<DocumentViewModel>>(documents),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<(bool success, DocumentViewModel? document, string? errorMessage)> VerifyDocumentAsync(
        Guid id, string userId)
    {
        var document = await _context.DriverDocuments
            .Include(d => d.Driver)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null)
        {
            return (false, null, "Document not found");
        }

        document.Status = "verified";
        document.VerifiedBy = userId;
        document.VerifiedDate = DateTime.UtcNow;

        await _repository.UpdateEntity(document);

        await _activityLogService.LogActivityAsync(
            userId,
            "document_verified",
            "DriverDocument",
            document.Id.ToString(),
            document.Type,
            $"Admin verified {document.Type} document for driver {document.Driver.FirstName} {document.Driver.LastName}"
        );

        return (true, _mapper.Map<DocumentViewModel>(document), null);
    }

    public async Task<(bool success, DocumentViewModel? document, string? errorMessage)> RejectDocumentAsync(
        Guid id, string userId, RejectDocumentModel? model)
    {
        var document = await _context.DriverDocuments
            .Include(d => d.Driver)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null)
        {
            return (false, null, "Document not found");
        }

        document.Status = "rejected";
        document.VerifiedBy = userId;
        document.VerifiedDate = DateTime.UtcNow;

        await _repository.UpdateEntity(document);

        var reason = model?.Reason ?? "Document did not meet verification requirements";

        await _activityLogService.LogActivityAsync(
            userId,
            "document_rejected",
            "DriverDocument",
            document.Id.ToString(),
            document.Type,
            $"Admin rejected {document.Type} document for driver {document.Driver.FirstName} {document.Driver.LastName}. Reason: {reason}"
        );

        return (true, _mapper.Map<DocumentViewModel>(document), null);
    }

    public async Task<List<DocumentViewModel>> GetExpiringDocumentsAsync(int daysAhead)
    {
        var expiryThreshold = DateTime.UtcNow.AddDays(daysAhead);

        var documents = await _context.DriverDocuments
            .Include(d => d.Driver)
            .Where(d => d.ExpiryDate.HasValue &&
                       d.ExpiryDate.Value <= expiryThreshold &&
                       d.ExpiryDate.Value >= DateTime.UtcNow &&
                       d.Status == "verified")
            .OrderBy(d => d.ExpiryDate)
            .ToListAsync();

        return _mapper.Map<List<DocumentViewModel>>(documents);
    }

    public async Task<List<DocumentViewModel>> GetDriverDocumentsByDriverIdAsync(Guid driverId)
    {
        var documents = await _repository.GetEntities<DriverDocument, DateTime>(
            d => d.DriverId == driverId,
            d => d.UploadedDate,
            isDescending: true
        );

        return _mapper.Map<List<DocumentViewModel>>(documents);
    }

    public async Task<DocumentStatisticsViewModel> GetDocumentStatisticsAsync()
    {
        var allDocuments = await _context.DriverDocuments.ToListAsync();

        var stats = new DocumentStatisticsViewModel
        {
            TotalDocuments = allDocuments.Count,
            PendingVerification = allDocuments.Count(d => d.Status == "pending"),
            Verified = allDocuments.Count(d => d.Status == "verified"),
            Rejected = allDocuments.Count(d => d.Status == "rejected"),
            Expired = allDocuments.Count(d => d.ExpiryDate.HasValue && d.ExpiryDate.Value < DateTime.UtcNow),
            ExpiringSoon = allDocuments.Count(d => d.ExpiryDate.HasValue &&
                                                   d.ExpiryDate.Value >= DateTime.UtcNow &&
                                                   d.ExpiryDate.Value <= DateTime.UtcNow.AddDays(30)),
            DocumentsByType = allDocuments
                .GroupBy(d => d.Type)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return stats;
    }

    public async Task<bool> CanUserAccessDocumentAsync(Guid documentId, string userId, IEnumerable<string> userRoles)
    {
        var document = await _context.DriverDocuments
            .Include(d => d.Driver)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
        {
            return false;
        }

        var isAdmin = userRoles.Contains(AuthRoles.Admin) || userRoles.Contains(AuthRoles.SuperAdmin);
        var isOwner = document.Driver.UserId == userId;

        return isAdmin || isOwner;
    }
}
