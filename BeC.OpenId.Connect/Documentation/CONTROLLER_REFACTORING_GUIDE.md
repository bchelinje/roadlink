# Controller Refactoring Guide - Repository Pattern

## Status Overview

### ✅ Completed
- **Program.cs** - IRepository registered
- **DriversController** - 5 methods refactored (partial)
- **CustomersController** - FULLY refactored (all 7 CRUD methods)

### 🔧 In Progress (Using statements added, constructors ready)
- **DocumentsController** - Constructor updated
- **NotificationsController** - Using statement added
- **PaymentsController** - Using statement added
- **ReviewsController** - Using statement added
- **VehiclesController** - Using statement added
- **LocationController** - Using statement added
- **PricingController** - Using statement added

### ⏳ Remaining
- Complete DocumentsController refactoring
- Complete NotificationsController refactoring
- Complete PaymentsController refactoring
- Complete ReviewsController refactoring
- Complete VehiclesController refactoring
- Complete LocationController refactoring
- Complete PricingController refactoring
- Complete remaining DriversController methods

---

## Quick Reference: Common Patterns

Based on the completed CustomersController refactoring, here are the most common patterns:

### Pattern 1: Constructor Update

**Before:**
```csharp
public class MyController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public MyController(ApplicationDbContext context)
    {
        _context = context;
    }
}
```

**After:**
```csharp
public class MyController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IRepository _repository;

    public MyController(
        ApplicationDbContext context,
        IRepository repository)
    {
        _context = context;
        _repository = repository;
    }
}
```

---

### Pattern 2: Insert Entity

**Before:**
```csharp
var entity = new MyEntity { /* properties */ };
_context.MyEntities.Add(entity);
await _context.SaveChangesAsync();
```

**After:**
```csharp
var entity = new MyEntity { /* properties */ };
await _repository.InsertEntity(entity);
// SaveChanges is automatic!
```

**Real Example from CustomersController:**
```csharp
// Before
_context.Jobs.Add(job);
await _context.SaveChangesAsync();

// After
await _repository.InsertEntity(job);
```

---

### Pattern 3: Get Entity by ID or Filter

**Before:**
```csharp
var entity = await _context.MyEntities
    .Include(e => e.RelatedEntity)
    .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
```

**After:**
```csharp
var entity = await _repository.GetEntity<MyEntity>(
    predicate: e => e.Id == id && e.UserId == userId,
    includeProperties: "RelatedEntity"
);
```

**Real Example from CustomersController:**
```csharp
// Before
var job = await _context.Jobs
    .Include(j => j.Driver)
    .FirstOrDefaultAsync(j => j.Id == id && j.CustomerId == userId);

// After
var job = await _repository.GetEntity<Job>(
    predicate: j => j.Id == id && j.CustomerId == userId,
    includeProperties: "Driver"
);
```

---

### Pattern 4: Get Paginated List

**Before:**
```csharp
var query = _context.MyEntities.Where(e => e.Status == status);
var total = await query.CountAsync();
var items = await query
    .OrderByDescending(e => e.CreatedAt)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

**After:**
```csharp
var result = await _repository.GetEntitiesPaged<MyEntity>(
    pageNumber: page,
    pageSize: pageSize,
    predicate: e => e.Status == status,
    orderBy: q => q.OrderByDescending(e => e.CreatedAt)
);

var total = result.TotalCount;
var items = result.Items;
```

**Real Example from CustomersController:**
```csharp
// Before
var query = _context.Jobs
    .Where(j => j.CustomerId == userId)
    .Include(j => j.Driver);

if (!string.IsNullOrWhiteSpace(status))
    query = query.Where(j => j.Status == status);

var totalCount = await query.CountAsync();
var jobs = await query
    .OrderByDescending(j => j.CreatedAt)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

// After
Expression<Func<Job, bool>> predicate = j => j.CustomerId == userId;

if (!string.IsNullOrWhiteSpace(status))
{
    var statusFilter = predicate;
    predicate = j => statusFilter.Compile()(j) && j.Status == status;
}

var result = await _repository.GetEntitiesPaged<Job>(
    pageNumber: page,
    pageSize: pageSize,
    predicate: predicate,
    orderBy: q => q.OrderByDescending(j => j.CreatedAt),
    includeProperties: "Driver"
);
```

---

### Pattern 5: Update Entity

**Before:**
```csharp
var entity = await _context.MyEntities.FindAsync(id);
entity.SomeProperty = newValue;
entity.UpdatedAt = DateTime.UtcNow;
await _context.SaveChangesAsync();
```

**After:**
```csharp
var entity = await _repository.GetEntity<MyEntity>(e => e.Id == id);
entity.SomeProperty = newValue;
entity.UpdatedAt = DateTime.UtcNow;
await _repository.UpdateEntity(entity);
```

**Real Example from CustomersController:**
```csharp
// Before
var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == id);
job.Status = "cancelled";
job.UpdatedAt = DateTime.UtcNow;
await _context.SaveChangesAsync();

// After
var job = await _repository.GetEntity<Job>(j => j.Id == id);
job.Status = "cancelled";
job.UpdatedAt = DateTime.UtcNow;
await _repository.UpdateEntity(job);
```

---

### Pattern 6: Existence Check

**Before:**
```csharp
var exists = await _context.MyEntities.AnyAsync(e => e.Email == email);
if (exists)
    return BadRequest("Already exists");
```

**After:**
```csharp
var exists = await _repository.Exists<MyEntity>(e => e.Email == email);
if (exists)
    return BadRequest("Already exists");
```

**Real Example from CustomersController:**
```csharp
// Before
var existingReview = await _context.Reviews
    .FirstOrDefaultAsync(r => r.JobId == jobId && r.ReviewerId == userId);
if (existingReview != null)
    return BadRequest("You have already reviewed this job");

// After
var alreadyReviewed = await _repository.Exists<Review>(
    r => r.JobId == jobId && r.ReviewerId == userId
);
if (alreadyReviewed)
    return BadRequest("You have already reviewed this job");
```

---

### Pattern 7: Get List with Ordering

**Before:**
```csharp
var items = await _context.MyEntities
    .Where(e => e.UserId == userId)
    .OrderByDescending(e => e.CreatedAt)
    .ToListAsync();
```

**After:**
```csharp
var items = await _repository.GetEntities<MyEntity>(
    predicate: e => e.UserId == userId,
    orderBy: q => q.OrderByDescending(e => e.CreatedAt)
);
```

**Real Example from CustomersController:**
```csharp
// Before
var reviews = await _context.Reviews
    .Where(r => r.ReviewerId == userId)
    .OrderByDescending(r => r.CreatedAt)
    .ToListAsync();

// After
var reviews = await _repository.GetEntities<Review>(
    predicate: r => r.ReviewerId == userId,
    orderBy: q => q.OrderByDescending(r => r.CreatedAt)
);
```

---

## Step-by-Step Refactoring Process

For each controller, follow these steps:

### Step 1: Add IRepository to Constructor
1. Add `private readonly IRepository _repository;`
2. Add parameter to constructor
3. Assign in constructor body

### Step 2: Identify Simple CRUD Methods
Look for methods that:
- Use `FirstOrDefaultAsync()` → Use `GetEntity`
- Use `ToListAsync()` → Use `GetEntities`
- Use pagination logic → Use `GetEntitiesPaged`
- Use `Add()` + `SaveChangesAsync()` → Use `InsertEntity`
- Modify entity + `SaveChangesAsync()` → Use `UpdateEntity`
- Use `AnyAsync()` → Use `Exists`
- Use `CountAsync()` → Use `Count`

### Step 3: Keep Complex Queries in DbContext
Keep using `_context` for:
- GroupBy operations
- Complex joins across multiple tables
- Raw SQL queries
- Transactions
- Aggregations (Sum, Average, etc.)

### Step 4: Test After Refactoring
- Run the build
- Test the endpoints
- Verify data is saved correctly

---

## Specific Controller Refactoring Guides

### DocumentsController

**Methods to Refactor:**

1. **GetMyDocuments()**
```csharp
// Before
var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
var documents = await _context.DriverDocuments
    .Where(d => d.DriverId == driver.Id)
    .OrderByDescending(d => d.UploadedDate)
    .ToListAsync();

// After
var driver = await _repository.GetEntity<Driver>(d => d.UserId == userId);
var documents = await _repository.GetEntities<DriverDocument>(
    predicate: d => d.DriverId == driver.Id,
    orderBy: q => q.OrderByDescending(d => d.UploadedDate)
);
```

2. **UploadDocument()**
```csharp
// Before
_context.DriverDocuments.Add(document);
await _context.SaveChangesAsync();

// After
await _repository.InsertEntity(document);
```

3. **GetDocument()**
```csharp
// Before
var document = await _context.DriverDocuments.FindAsync(id);

// After
var document = await _repository.GetEntity<DriverDocument>(d => d.Id == id);
```

4. **UpdateDocumentStatus()**
```csharp
// Before
var document = await _context.DriverDocuments.FindAsync(id);
document.Status = status;
await _context.SaveChangesAsync();

// After
var document = await _repository.GetEntity<DriverDocument>(d => d.Id == id);
document.Status = status;
await _repository.UpdateEntity(document);
```

---

### NotificationsController

**Methods to Refactor:**

1. **GetMyNotifications()**
```csharp
// Use GetEntitiesPaged for pagination
var result = await _repository.GetEntitiesPaged<Notification>(
    pageNumber: page,
    pageSize: pageSize,
    predicate: n => n.UserId == userId,
    orderBy: q => q.OrderByDescending(n => n.CreatedAt)
);
```

2. **MarkAsRead()**
```csharp
var notification = await _repository.GetEntity<Notification>(n => n.Id == id);
notification.IsRead = true;
notification.ReadAt = DateTime.UtcNow;
await _repository.UpdateEntity(notification);
```

3. **MarkAllAsRead()**
```csharp
var notifications = await _repository.GetEntities<Notification>(
    n => n.UserId == userId && !n.IsRead
);
foreach (var notification in notifications)
{
    notification.IsRead = true;
    notification.ReadAt = DateTime.UtcNow;
}
await _repository.UpdateEntities(notifications.ToList());
```

---

### PaymentsController

**Methods to Refactor:**

1. **GetPayments()**
```csharp
var result = await _repository.GetEntitiesPaged<Payment>(
    pageNumber: page,
    pageSize: pageSize,
    predicate: p => p.CustomerId == userId,
    orderBy: q => q.OrderByDescending(p => p.CreatedAt),
    includeProperties: "Job,Driver"
);
```

2. **CreatePayment()**
```csharp
await _repository.InsertEntity(payment);
```

3. **UpdatePaymentStatus()**
```csharp
var payment = await _repository.GetEntity<Payment>(p => p.Id == id);
payment.Status = status;
payment.UpdatedAt = DateTime.UtcNow;
await _repository.UpdateEntity(payment);
```

---

### ReviewsController

**Methods to Refactor:**

1. **GetReviews()**
```csharp
var result = await _repository.GetEntitiesPaged<Review>(
    pageNumber: page,
    pageSize: pageSize,
    predicate: r => r.RevieweeId == driverId && r.RevieweeType == "driver",
    orderBy: q => q.OrderByDescending(r => r.CreatedAt)
);
```

2. **CreateReview()**
```csharp
// Check if already exists
if (await _repository.Exists<Review>(r => r.JobId == jobId && r.ReviewerId == userId))
    return BadRequest("Already reviewed");

await _repository.InsertEntity(review);
```

3. **UpdateReview()**
```csharp
var review = await _repository.GetEntity<Review>(r => r.Id == id);
review.Comment = updatedComment;
review.Rating = updatedRating;
await _repository.UpdateEntity(review);
```

---

### VehiclesController

**Methods to Refactor:**

1. **GetMyVehicles()**
```csharp
var driver = await _repository.GetEntity<Driver>(d => d.UserId == userId);
var vehicles = await _repository.GetEntities<Vehicle>(
    predicate: v => v.DriverId == driver.Id,
    orderBy: q => q.OrderByDescending(v => v.CreatedAt)
);
```

2. **CreateVehicle()**
```csharp
await _repository.InsertEntity(vehicle);
```

3. **UpdateVehicle()**
```csharp
var vehicle = await _repository.GetEntity<Vehicle>(v => v.Id == id);
vehicle.Status = status;
await _repository.UpdateEntity(vehicle);
```

---

### LocationController

**Methods to Refactor:**

1. **UpdateDriverLocation()**
```csharp
var driver = await _repository.GetEntity<Driver>(d => d.UserId == userId);

var location = new DriverLocation
{
    DriverId = driver.Id,
    Latitude = latitude,
    Longitude = longitude,
    Timestamp = DateTime.UtcNow
};

await _repository.InsertEntity(location);
```

2. **GetDriverLocation()**
```csharp
var location = await _repository.GetEntity<DriverLocation>(
    predicate: l => l.DriverId == driverId,
    orderBy: q => q.OrderByDescending(l => l.Timestamp)
);
```

---

### PricingController

**Methods to Refactor:**

1. **GetPricingRules()**
```csharp
var rules = await _repository.GetEntities<PricingRule>(
    predicate: r => r.IsActive,
    orderBy: q => q.OrderBy(r => r.Priority)
);
```

2. **CreatePricingRule()**
```csharp
await _repository.InsertEntity(rule);
```

3. **UpdatePricingRule()**
```csharp
var rule = await _repository.GetEntity<PricingRule>(r => r.Id == id);
rule.IsActive = isActive;
await _repository.UpdateEntity(rule);
```

4. **CalculatePrice()** - Keep using DbContext or PricingCalculatorService
This method likely has complex business logic - keep existing implementation.

---

## Common Pitfalls to Avoid

### ❌ Don't: Mix repository and DbContext for same entity

```csharp
// Bad - inconsistent
var driver = await _repository.GetEntity<Driver>(d => d.Id == id);
_context.Drivers.Update(driver);
await _context.SaveChangesAsync();
```

```csharp
// Good - consistent
var driver = await _repository.GetEntity<Driver>(d => d.Id == id);
driver.SomeProperty = newValue;
await _repository.UpdateEntity(driver);
```

### ❌ Don't: Forget to handle null results

```csharp
// Bad
var entity = await _repository.GetEntity<MyEntity>(e => e.Id == id);
entity.Property = value; // NullReferenceException if not found!
```

```csharp
// Good
var entity = await _repository.GetEntity<MyEntity>(e => e.Id == id);
if (entity == null)
    return NotFound();

entity.Property = value;
await _repository.UpdateEntity(entity);
```

### ❌ Don't: Use repository for complex aggregations

```csharp
// Bad - use DbContext instead
var stats = await _repository.GetEntities<Job>(j => j.DriverId == driverId);
var grouped = stats.GroupBy(j => j.Status).Select(...); // Complex logic
```

```csharp
// Good - use DbContext for complex queries
var stats = await _context.Jobs
    .Where(j => j.DriverId == driverId)
    .GroupBy(j => j.Status)
    .Select(g => new {
        Status = g.Key,
        Count = g.Count(),
        Total = g.Sum(j => j.TotalPrice)
    })
    .ToListAsync();
```

---

## Testing Checklist

After refactoring each controller:

- [ ] Code compiles without errors
- [ ] All endpoints return correct status codes
- [ ] Data is saved to database correctly
- [ ] Pagination works correctly
- [ ] Filtering/searching still works
- [ ] Related entities (includes) are loaded
- [ ] Authentication/authorization still enforced
- [ ] Activity logs still generated
- [ ] No null reference exceptions

---

## Bulk Find-and-Replace Patterns

For quick refactoring, you can use these sed/grep patterns:

```bash
# Replace FirstOrDefaultAsync with repository pattern (manual review needed)
# _context.MyEntities.FirstOrDefaultAsync(e => e.Id == id)
# → await _repository.GetEntity<MyEntity>(e => e.Id == id)

# Replace Add + SaveChangesAsync
# _context.MyEntities.Add(entity); await _context.SaveChangesAsync();
# → await _repository.InsertEntity(entity);

# Replace AnyAsync with Exists
# await _context.MyEntities.AnyAsync(e => e.Property == value)
# → await _repository.Exists<MyEntity>(e => e.Property == value)
```

---

## Final Notes

- **CustomersController** is the gold standard - use it as reference
- **Repository pattern** is for simple CRUD - not all queries
- **Keep DbContext** available for complex operations
- **Test thoroughly** after each refactoring
- **Commit frequently** to save progress

See `REPOSITORY_PATTERN_GUIDE.md` for complete API reference.

---

**Last Updated:** 2025-11-15
**Completed:** CustomersController (100%), DriversController (30%)
**Remaining:** 7 controllers, ~2500 lines of code
