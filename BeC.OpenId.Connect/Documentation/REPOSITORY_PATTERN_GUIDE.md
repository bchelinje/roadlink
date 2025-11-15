# BeC.Common.Data Repository Pattern Guide

## Overview

This guide demonstrates how to use the `BeC.Common.Data` package (v1.0.50) for implementing the repository pattern in your controllers. The repository pattern provides a clean abstraction layer over Entity Framework Core, making code more testable and maintainable.

## Setup

### 1. Package Installation

The package is already installed in your project:
```xml
<PackageReference Include="BeC.Common.Data" Version="1.0.50" />
```

### 2. Service Registration

In `Program.cs`, the IRepository is registered as a scoped service:

```csharp
using BeC.Common.Data.Repositories;
using BeC.Common.Data.Repositories.Interfaces;

// Register BeC.Common.Data Repository
builder.Services.AddScoped<IRepository, Repository>();
```

### 3. Controller Setup

Inject `IRepository` into your controller:

```csharp
using BeC.Common.Data.Repositories.Interfaces;

public class DriversController : ControllerBase
{
    private readonly IRepository _repository;
    private readonly ApplicationDbContext _context; // Keep for complex operations

    public DriversController(
        IRepository repository,
        ApplicationDbContext context)
    {
        _repository = repository;
        _context = context;
    }
}
```

## Common Repository Methods

### 1. GetEntity - Get Single Entity

**Use Case:** Retrieve a single entity by ID or filter

#### Before (Direct DbContext):
```csharp
var driver = await _context.Drivers
    .Include(d => d.Vehicles)
    .Include(d => d.Documents)
    .FirstOrDefaultAsync(d => d.Id == id);
```

#### After (Repository Pattern):
```csharp
var driver = await _repository.GetEntity<Driver>(
    predicate: d => d.Id == id,
    includeProperties: "Vehicles,Documents"
);
```

**Method Signature:**
```csharp
Task<TEntity?> GetEntity<TEntity>(
    Expression<Func<TEntity, bool>> predicate,
    string? includeProperties = null
) where TEntity : class
```

---

### 2. GetEntities - Get Multiple Entities

**Use Case:** Retrieve multiple entities with filtering and includes

#### Before (Direct DbContext):
```csharp
var activeDrivers = await _context.Drivers
    .Where(d => d.Status == "active")
    .Include(d => d.Vehicles)
    .OrderBy(d => d.LastName)
    .ToListAsync();
```

#### After (Repository Pattern):
```csharp
var activeDrivers = await _repository.GetEntities<Driver>(
    predicate: d => d.Status == "active",
    orderBy: q => q.OrderBy(d => d.LastName),
    includeProperties: "Vehicles"
);
```

**Method Signature:**
```csharp
Task<IEnumerable<TEntity>> GetEntities<TEntity>(
    Expression<Func<TEntity, bool>>? predicate = null,
    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
    string? includeProperties = null
) where TEntity : class
```

---

### 3. GetEntitiesPaged - Paginated Results

**Use Case:** Retrieve entities with pagination for API endpoints

#### Before (Direct DbContext):
```csharp
var query = _context.Drivers.AsQueryable();
if (!string.IsNullOrEmpty(status))
    query = query.Where(d => d.Status == status);

var total = await query.CountAsync();
var drivers = await query
    .OrderByDescending(d => d.CreatedAt)
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

#### After (Repository Pattern):
```csharp
var result = await _repository.GetEntitiesPaged<Driver>(
    pageNumber: pageNumber,
    pageSize: pageSize,
    predicate: d => d.Status == status, // Optional filter
    orderBy: q => q.OrderByDescending(d => d.CreatedAt),
    includeProperties: "Vehicles"
);

// Access: result.Items, result.TotalCount, result.PageNumber, result.PageSize
```

**Method Signature:**
```csharp
Task<PagedResult<TEntity>> GetEntitiesPaged<TEntity>(
    int pageNumber,
    int pageSize,
    Expression<Func<TEntity, bool>>? predicate = null,
    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
    string? includeProperties = null
) where TEntity : class
```

**PagedResult Object:**
```csharp
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
```

---

### 4. InsertEntity - Create New Entity

**Use Case:** Add a new entity to the database

#### Before (Direct DbContext):
```csharp
var driver = new Driver { /* properties */ };
_context.Drivers.Add(driver);
await _context.SaveChangesAsync();
```

#### After (Repository Pattern):
```csharp
var driver = new Driver { /* properties */ };
await _repository.InsertEntity(driver);
// Auto-saves changes
```

**Method Signature:**
```csharp
Task InsertEntity<TEntity>(TEntity entity) where TEntity : class
```

---

### 5. UpdateEntity - Update Existing Entity

**Use Case:** Modify an existing entity

#### Before (Direct DbContext):
```csharp
var driver = await _context.Drivers.FindAsync(id);
driver.Phone = newPhone;
driver.UpdatedAt = DateTime.UtcNow;
await _context.SaveChangesAsync();
```

#### After (Repository Pattern):
```csharp
var driver = await _repository.GetEntity<Driver>(d => d.Id == id);
driver.Phone = newPhone;
driver.UpdatedAt = DateTime.UtcNow;
await _repository.UpdateEntity(driver);
// Auto-saves changes
```

**Method Signature:**
```csharp
Task UpdateEntity<TEntity>(TEntity entity) where TEntity : class
```

---

### 6. RemoveEntity - Delete Entity

**Use Case:** Delete an entity from the database

#### Before (Direct DbContext):
```csharp
var driver = await _context.Drivers.FindAsync(id);
if (driver != null)
{
    _context.Drivers.Remove(driver);
    await _context.SaveChangesAsync();
}
```

#### After (Repository Pattern):
```csharp
var driver = await _repository.GetEntity<Driver>(d => d.Id == id);
if (driver != null)
{
    await _repository.RemoveEntity(driver);
    // Auto-saves changes
}
```

**Method Signature:**
```csharp
Task RemoveEntity<TEntity>(TEntity entity) where TEntity : class
```

---

### 7. Exists - Check Entity Existence

**Use Case:** Check if an entity exists without loading it

#### Before (Direct DbContext):
```csharp
var exists = await _context.Drivers.AnyAsync(d => d.Email == email);
```

#### After (Repository Pattern):
```csharp
var exists = await _repository.Exists<Driver>(d => d.Email == email);
```

**Method Signature:**
```csharp
Task<bool> Exists<TEntity>(
    Expression<Func<TEntity, bool>> predicate
) where TEntity : class
```

---

### 8. Count - Count Entities

**Use Case:** Get count of entities matching a condition

#### Before (Direct DbContext):
```csharp
var activeCount = await _context.Drivers.CountAsync(d => d.Status == "active");
```

#### After (Repository Pattern):
```csharp
var activeCount = await _repository.Count<Driver>(d => d.Status == "active");
```

**Method Signature:**
```csharp
Task<int> Count<TEntity>(
    Expression<Func<TEntity, bool>>? predicate = null
) where TEntity : class
```

---

### 9. UpsertEntity - Insert or Update

**Use Case:** Insert if doesn't exist, update if it does

```csharp
var driver = new Driver { /* properties */ };
await _repository.UpsertEntity(driver);
// Automatically determines whether to insert or update
```

**Method Signature:**
```csharp
Task UpsertEntity<TEntity>(TEntity entity) where TEntity : class
```

---

### 10. PatchEntity - Partial Update

**Use Case:** Update only specific fields using JSON Patch

```csharp
using Microsoft.AspNetCore.JsonPatch;

var patchDoc = new JsonPatchDocument<Driver>();
patchDoc.Replace(d => d.Phone, newPhone);
patchDoc.Replace(d => d.Status, "active");

await _repository.PatchEntity<Driver>(driverId, patchDoc);
```

**Method Signature:**
```csharp
Task PatchEntity<TEntity>(
    object id,
    JsonPatchDocument<TEntity> patchDoc
) where TEntity : class
```

---

## Bulk Operations

### InsertEntities - Bulk Insert

```csharp
var drivers = new List<Driver> { driver1, driver2, driver3 };
await _repository.InsertEntities(drivers);
```

### UpdateEntities - Bulk Update

```csharp
var drivers = await _repository.GetEntities<Driver>(d => d.Status == "pending");
foreach (var driver in drivers)
    driver.Status = "active";

await _repository.UpdateEntities(drivers);
```

### RemoveEntities - Bulk Delete

```csharp
var inactiveDrivers = await _repository.GetEntities<Driver>(d => d.Status == "inactive");
await _repository.RemoveEntities(inactiveDrivers);
```

---

## Complex Queries - When to Use DbContext

For complex queries that require:
- Multiple joins
- Group by operations
- Raw SQL queries
- Transactions across multiple entities
- Complex LINQ expressions

**Continue using DbContext directly:**

```csharp
var statistics = await _context.Jobs
    .Where(j => j.DriverId == driverId && j.Status == "completed")
    .GroupBy(j => j.CompletedDate.Date)
    .Select(g => new JobStatDto
    {
        Date = g.Key,
        TotalJobs = g.Count(),
        TotalRevenue = g.Sum(j => j.TotalPrice)
    })
    .OrderByDescending(s => s.Date)
    .Take(30)
    .ToListAsync();
```

---

## Best Practices

### 1. Use Repository for Simple CRUD
✅ **DO:** Use repository for basic create, read, update, delete operations
```csharp
var driver = await _repository.GetEntity<Driver>(d => d.Id == id);
```

❌ **DON'T:** Use repository for complex business logic queries
```csharp
// Use DbContext instead for this
var stats = await _context.Jobs.GroupBy(...).Select(...);
```

### 2. Include Related Entities

✅ **DO:** Use comma-separated string for includes
```csharp
includeProperties: "Vehicles,Documents,Jobs"
```

✅ **DO:** Use nested includes with dot notation
```csharp
includeProperties: "Jobs.Customer,Jobs.Payments"
```

### 3. Filtering Best Practices

✅ **DO:** Use simple predicates
```csharp
predicate: d => d.Status == "active" && d.Rating >= 4.5m
```

✅ **DO:** Build complex predicates programmatically
```csharp
Expression<Func<Driver, bool>>? predicate = null;
if (!string.IsNullOrEmpty(status))
    predicate = d => d.Status == status;

if (minRating.HasValue)
{
    var ratingPred = predicate;
    predicate = d => (ratingPred == null || ratingPred.Compile()(d))
                    && d.Rating >= minRating.Value;
}
```

### 4. Pagination Strategy

✅ **DO:** Always use GetEntitiesPaged for list endpoints
```csharp
var result = await _repository.GetEntitiesPaged<Driver>(
    pageNumber: pageNumber,
    pageSize: Math.Min(pageSize, 100), // Limit max page size
    predicate: buildFilter(),
    orderBy: q => q.OrderByDescending(d => d.CreatedAt)
);
```

### 5. Keep Both Repository and DbContext

✅ **DO:** Inject both for flexibility
```csharp
public DriversController(
    IRepository repository,      // For simple CRUD
    ApplicationDbContext context // For complex queries
)
```

---

## Real-World Examples from DriversController

### Example 1: Get Current User's Profile
```csharp
[HttpGet("me")]
public async Task<ActionResult<DriverDto>> GetCurrentDriver()
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    var driver = await _repository.GetEntity<Driver>(
        predicate: d => d.UserId == userId,
        includeProperties: "Vehicles,Documents"
    );

    if (driver == null)
        return NotFound("Driver profile not found");

    return Ok(MapToDto(driver));
}
```

### Example 2: Get Paginated List with Filters
```csharp
[HttpGet]
public async Task<ActionResult<DriverDtoPaginatedResult>> GetDrivers(
    [FromQuery] string? status = null,
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10)
{
    var result = await _repository.GetEntitiesPaged<Driver>(
        pageNumber: pageNumber,
        pageSize: pageSize,
        predicate: string.IsNullOrEmpty(status) ? null : d => d.Status == status,
        orderBy: q => q.OrderByDescending(d => d.CreatedAt),
        includeProperties: "Vehicles"
    );

    return Ok(new DriverDtoPaginatedResult
    {
        Items = result.Items.Select(MapToDto).ToList(),
        Total = result.TotalCount,
        PageNumber = result.PageNumber,
        PageSize = result.PageSize
    });
}
```

### Example 3: Create with Validation
```csharp
[HttpPost]
public async Task<ActionResult<DriverDto>> CreateDriver([FromBody] CreateDriverDto dto)
{
    // Validation using Exists
    if (await _repository.Exists<Driver>(d => d.UserId == dto.UserId))
        return BadRequest("Driver profile already exists");

    if (await _repository.Exists<Driver>(d => d.Email == dto.Email))
        return BadRequest("Email is already in use");

    var driver = new Driver
    {
        UserId = dto.UserId,
        Email = dto.Email,
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        Status = "inactive",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    await _repository.InsertEntity(driver);

    return CreatedAtAction(nameof(GetDriver), new { id = driver.Id }, MapToDto(driver));
}
```

### Example 4: Update with Authorization
```csharp
[HttpPut("{id}")]
public async Task<ActionResult<DriverDto>> UpdateDriver(Guid id, [FromBody] UpdateDriverDto dto)
{
    var driver = await _repository.GetEntity<Driver>(d => d.Id == id);

    if (driver == null)
        return NotFound();

    // Authorization check
    if (!User.IsInRole("Admin") && driver.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
        return Forbid();

    // Update fields
    driver.Phone = dto.Phone ?? driver.Phone;
    driver.Address = dto.Address ?? driver.Address;
    driver.UpdatedAt = DateTime.UtcNow;

    await _repository.UpdateEntity(driver);

    return Ok(MapToDto(driver));
}
```

---

## Migration Guide

To convert existing controllers to use the repository pattern:

1. **Add using statement:**
   ```csharp
   using BeC.Common.Data.Repositories.Interfaces;
   ```

2. **Inject IRepository:**
   ```csharp
   public MyController(IRepository repository, ApplicationDbContext context)
   ```

3. **Replace DbContext calls:**
   - `_context.Drivers.FindAsync(id)` → `_repository.GetEntity<Driver>(d => d.Id == id)`
   - `_context.Drivers.ToListAsync()` → `_repository.GetAllEntities<Driver>()`
   - `_context.Drivers.Add(entity)` → `_repository.InsertEntity(entity)`
   - `_context.SaveChangesAsync()` → (Not needed, repository auto-saves)

4. **Keep DbContext for complex queries**

---

## Summary

The BeC.Common.Data repository pattern provides:

✅ **Benefits:**
- Cleaner, more maintainable code
- Easier unit testing (mock IRepository)
- Consistent data access patterns
- Automatic SaveChanges handling
- Built-in pagination support
- Type-safe queries

⚠️ **Considerations:**
- Not suitable for all complex queries
- Keep DbContext available for advanced scenarios
- Learn the method signatures and parameters
- Use predicates carefully for filtering

---

## Further Reading

- [Repository Pattern Overview](https://learn.microsoft.com/en-us/aspnet/mvc/overview/older-versions/getting-started-with-ef-5-using-mvc-4/implementing-the-repository-and-unit-of-work-patterns-in-an-asp-net-mvc-application)
- [AutoMapper Documentation](https://docs.automapper.org/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)

---

**Last Updated:** 2025-11-15
**Package Version:** BeC.Common.Data 1.0.50
**Author:** BeC Development Team
