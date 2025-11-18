using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.HelpCenter.Dtos;
using BeC.OpenId.Connect.Features.HelpCenter.Models;
using BeC.OpenId.Connect.Features.Users.Dtos;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using AuthRoles = BeC.OpenId.Connect.Infrastructure.Authorization.Roles;

namespace BeC.OpenId.Connect.Features.HelpCenter.Controllers;

/// <summary>
/// Help Center with FAQs and knowledge base articles
/// </summary>
[ApiController]
[Route("api/help")]
[Produces("application/json")]
public class HelpCenterController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<HelpCenterController> _logger;

    public HelpCenterController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IActivityLogService activityLogService,
        ILogger<HelpCenterController> logger)
    {
        _context = context;
        _userManager = userManager;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    #region FAQ Endpoints

    /// <summary>
    /// Get all published FAQs (public or authenticated)
    /// </summary>
    [HttpGet("faqs")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<FAQ>), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetFAQs(
        [FromQuery] string? category = null,
        [FromQuery] string? targetAudience = null,
        [FromQuery] string? search = null,
        [FromQuery] bool? isFeatured = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.FAQs.Where(f => f.IsPublished);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(f => f.Category == category);

        if (!string.IsNullOrEmpty(targetAudience))
            query = query.Where(f => f.TargetAudience == targetAudience || f.TargetAudience == "all");

        if (isFeatured.HasValue)
            query = query.Where(f => f.IsFeatured == isFeatured.Value);

        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(f =>
                f.Question.ToLower().Contains(searchLower) ||
                f.Answer.ToLower().Contains(searchLower) ||
                (f.Tags != null && f.Tags.ToLower().Contains(searchLower)));
        }

        var total = await query.CountAsync();
        var faqs = await query
            .OrderBy(f => f.DisplayOrder)
            .ThenByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            data = faqs,
            pagination = new
            {
                page,
                pageSize,
                total,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        });
    }

    /// <summary>
    /// Get FAQ by ID or slug
    /// </summary>
    [HttpGet("faqs/{identifier}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FAQ), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FAQ>> GetFAQ(string identifier)
    {
        FAQ? faq = null;

        // Try to parse as GUID first
        if (Guid.TryParse(identifier, out var id))
        {
            faq = await _context.FAQs.FindAsync(id);
        }
        else
        {
            // Otherwise treat as slug
            faq = await _context.FAQs.FirstOrDefaultAsync(f => f.Slug == identifier);
        }

        if (faq == null)
            return NotFound();

        // Increment view count
        faq.ViewCount++;
        faq.LastViewedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(faq);
    }

    /// <summary>
    /// Create FAQ (Admin only)
    /// </summary>
    [HttpPost("faqs")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
               Roles = $"{AuthRoles.Admin},{AuthRoles.SuperAdmin}")]
    [ProducesResponseType(typeof(FAQ), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FAQ>> CreateFAQ([FromBody] CreateFAQDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Generate slug from question if not provided
        var slug = request.Question.ToLower()
            .Replace(" ", "-")
            .Replace("?", "")
            .Replace("!", "")
            .Substring(0, Math.Min(100, request.Question.Length));

        var faq = new FAQ
        {
            Question = request.Question,
            Answer = request.Answer,
            Category = request.Category,
            Subcategory = request.Subcategory,
            TargetAudience = request.TargetAudience,
            DisplayOrder = request.DisplayOrder,
            IsPublished = request.IsPublished,
            IsFeatured = request.IsFeatured,
            Slug = slug,
            Tags = request.Tags != null ? JsonSerializer.Serialize(request.Tags) : null,
            Keywords = request.Keywords != null ? JsonSerializer.Serialize(request.Keywords) : null,
            RelatedFAQs = request.RelatedFAQs != null ? JsonSerializer.Serialize(request.RelatedFAQs) : null,
            CreatedBy = userId
        };

        _context.FAQs.Add(faq);
        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "faq_created",
            "FAQ",
            faq.Id.ToString(),
            $"FAQ: {faq.Question}",
            "Created new FAQ"
        );

        return CreatedAtAction(nameof(GetFAQ), new { identifier = faq.Id }, faq);
    }

    /// <summary>
    /// Update FAQ (Admin only)
    /// </summary>
    [HttpPatch("faqs/{id}")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
               Roles = $"{AuthRoles.Admin},{AuthRoles.SuperAdmin}")]
    [ProducesResponseType(typeof(FAQ), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FAQ>> UpdateFAQ(Guid id, [FromBody] UpdateFAQDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var faq = await _context.FAQs.FindAsync(id);
        if (faq == null)
            return NotFound();

        if (request.Question != null)
            faq.Question = request.Question;

        if (request.Answer != null)
            faq.Answer = request.Answer;

        if (request.Category != null)
            faq.Category = request.Category;

        if (request.Subcategory != null)
            faq.Subcategory = request.Subcategory;

        if (request.TargetAudience != null)
            faq.TargetAudience = request.TargetAudience;

        if (request.DisplayOrder.HasValue)
            faq.DisplayOrder = request.DisplayOrder.Value;

        if (request.IsPublished.HasValue)
            faq.IsPublished = request.IsPublished.Value;

        if (request.IsFeatured.HasValue)
            faq.IsFeatured = request.IsFeatured.Value;

        if (request.Tags != null)
            faq.Tags = JsonSerializer.Serialize(request.Tags);

        if (request.Keywords != null)
            faq.Keywords = JsonSerializer.Serialize(request.Keywords);

        faq.Version++;
        faq.LastEditedBy = userId;
        faq.LastEditedAt = DateTime.UtcNow;
        faq.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "faq_updated",
            "FAQ",
            faq.Id.ToString(),
            $"FAQ: {faq.Question}",
            "Updated FAQ"
        );

        return Ok(faq);
    }

    /// <summary>
    /// Delete FAQ (Admin only)
    /// </summary>
    [HttpDelete("faqs/{id}")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
               Roles = $"{AuthRoles.Admin},{AuthRoles.SuperAdmin}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteFAQ(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var faq = await _context.FAQs.FindAsync(id);
        if (faq == null)
            return NotFound();

        _context.FAQs.Remove(faq);
        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "faq_deleted",
            "FAQ",
            faq.Id.ToString(),
            $"FAQ: {faq.Question}",
            "Deleted FAQ"
        );

        return Ok(new { message = "FAQ deleted successfully" });
    }

    /// <summary>
    /// Mark FAQ as helpful
    /// </summary>
    [HttpPost("faqs/{id}/helpful")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> MarkFAQHelpful(Guid id, [FromQuery] bool helpful = true)
    {
        var faq = await _context.FAQs.FindAsync(id);
        if (faq == null)
            return NotFound();

        if (helpful)
            faq.HelpfulCount++;
        else
            faq.NotHelpfulCount++;

        await _context.SaveChangesAsync();

        return Ok(new { helpful = faq.HelpfulCount, notHelpful = faq.NotHelpfulCount });
    }

    #endregion

    #region Article Endpoints

    /// <summary>
    /// Get all published articles
    /// </summary>
    [HttpGet("articles")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<HelpArticle>), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetArticles(
        [FromQuery] string? category = null,
        [FromQuery] string? targetAudience = null,
        [FromQuery] string? articleType = null,
        [FromQuery] string? search = null,
        [FromQuery] bool? isFeatured = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.HelpArticles.Where(a => a.IsPublished);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(a => a.Category == category);

        if (!string.IsNullOrEmpty(targetAudience))
            query = query.Where(a => a.TargetAudience == targetAudience || a.TargetAudience == "all");

        if (!string.IsNullOrEmpty(articleType))
            query = query.Where(a => a.ArticleType == articleType);

        if (isFeatured.HasValue)
            query = query.Where(a => a.IsFeatured == isFeatured.Value);

        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(a =>
                a.Title.ToLower().Contains(searchLower) ||
                a.Content.ToLower().Contains(searchLower) ||
                (a.Summary != null && a.Summary.ToLower().Contains(searchLower)));
        }

        var total = await query.CountAsync();
        var articles = await query
            .OrderBy(a => a.DisplayOrder)
            .ThenByDescending(a => a.PublishedAt ?? a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            data = articles,
            pagination = new
            {
                page,
                pageSize,
                total,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        });
    }

    /// <summary>
    /// Get article by ID or slug
    /// </summary>
    [HttpGet("articles/{identifier}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HelpArticle), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HelpArticle>> GetArticle(string identifier)
    {
        HelpArticle? article = null;

        // Try to parse as GUID first
        if (Guid.TryParse(identifier, out var id))
        {
            article = await _context.HelpArticles.FindAsync(id);
        }
        else
        {
            // Otherwise treat as slug
            article = await _context.HelpArticles.FirstOrDefaultAsync(a => a.Slug == identifier);
        }

        if (article == null)
            return NotFound();

        // Increment view count
        article.ViewCount++;
        article.LastViewedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(article);
    }

    /// <summary>
    /// Create article (Admin only)
    /// </summary>
    [HttpPost("articles")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
               Roles = $"{AuthRoles.Admin},{AuthRoles.SuperAdmin}")]
    [ProducesResponseType(typeof(HelpArticle), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HelpArticle>> CreateArticle([FromBody] CreateArticleDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Check if slug already exists
        var existingSlug = await _context.HelpArticles.FirstOrDefaultAsync(a => a.Slug == request.Slug);
        if (existingSlug != null)
            return BadRequest("Slug already exists");

        // Estimate reading time (assuming 200 words per minute)
        var wordCount = request.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var estimatedReadingTime = (int)Math.Ceiling(wordCount / 200.0);

        var article = new HelpArticle
        {
            Title = request.Title,
            Content = request.Content,
            Summary = request.Summary,
            Category = request.Category,
            Subcategory = request.Subcategory,
            TargetAudience = request.TargetAudience,
            ArticleType = request.ArticleType,
            DisplayOrder = request.DisplayOrder,
            IsPublished = request.IsPublished,
            IsFeatured = request.IsFeatured,
            Slug = request.Slug,
            MetaDescription = request.MetaDescription,
            Tags = request.Tags != null ? JsonSerializer.Serialize(request.Tags) : null,
            Keywords = request.Keywords != null ? JsonSerializer.Serialize(request.Keywords) : null,
            FeaturedImage = request.FeaturedImage,
            VideoUrl = request.VideoUrl,
            EstimatedReadingTime = estimatedReadingTime,
            CreatedBy = userId,
            PublishedAt = request.IsPublished ? DateTime.UtcNow : null
        };

        _context.HelpArticles.Add(article);
        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "article_created",
            "HelpArticle",
            article.Id.ToString(),
            $"Article: {article.Title}",
            "Created new help article"
        );

        return CreatedAtAction(nameof(GetArticle), new { identifier = article.Id }, article);
    }

    /// <summary>
    /// Mark article as helpful
    /// </summary>
    [HttpPost("articles/{id}/helpful")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> MarkArticleHelpful(Guid id, [FromQuery] bool helpful = true)
    {
        var article = await _context.HelpArticles.FindAsync(id);
        if (article == null)
            return NotFound();

        if (helpful)
            article.HelpfulCount++;
        else
            article.NotHelpfulCount++;

        await _context.SaveChangesAsync();

        return Ok(new { helpful = article.HelpfulCount, notHelpful = article.NotHelpfulCount });
    }

    /// <summary>
    /// Search help content (FAQs and articles)
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> SearchHelp([FromQuery] string query, [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Search query is required");

        var searchLower = query.ToLower();

        var faqs = await _context.FAQs
            .Where(f => f.IsPublished &&
                       (f.Question.ToLower().Contains(searchLower) ||
                        f.Answer.ToLower().Contains(searchLower)))
            .OrderByDescending(f => f.ViewCount)
            .Take(limit)
            .Select(f => new { type = "faq", id = f.Id, title = f.Question, slug = f.Slug })
            .ToListAsync();

        var articles = await _context.HelpArticles
            .Where(a => a.IsPublished &&
                       (a.Title.ToLower().Contains(searchLower) ||
                        a.Content.ToLower().Contains(searchLower) ||
                        (a.Summary != null && a.Summary.ToLower().Contains(searchLower))))
            .OrderByDescending(a => a.ViewCount)
            .Take(limit)
            .Select(a => new { type = "article", id = a.Id, title = a.Title, slug = a.Slug })
            .ToListAsync();

        return Ok(new
        {
            query,
            results = new
            {
                faqs,
                articles
            }
        });
    }

    #endregion
}
