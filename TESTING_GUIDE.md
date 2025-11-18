# Comprehensive Testing Guide for BeC OpenId Connect

## Table of Contents
1. [Overview](#overview)
2. [Test Infrastructure Setup](#test-infrastructure-setup)
3. [Testing Strategy](#testing-strategy)
4. [Unit Testing](#unit-testing)
5. [Integration Testing](#integration-testing)
6. [End-to-End Testing](#end-to-end-testing)
7. [Testing Complex Features](#testing-complex-features)
8. [Mocking External Dependencies](#mocking-external-dependencies)
9. [Test Data Management](#test-data-management)
10. [CI/CD Integration](#cicd-integration)
11. [Performance Testing](#performance-testing)
12. [Security Testing](#security-testing)

---

## Overview

BeC OpenId Connect is a delivery and moving services marketplace platform with complex business logic including:
- **Escrow payment system** with automated commission splits
- **Dynamic pricing engine** with 7 rule types
- **Google Maps integration** for geocoding and routing
- **Real-time features** using SignalR
- **Background job processing** for payouts and notifications
- **OAuth2/OpenID Connect** authentication

**Current State**: No test projects exist in the solution. This guide provides a roadmap for implementing comprehensive testing.

---

## Test Infrastructure Setup

### 1. Create Test Projects

```bash
# Unit Test Project
dotnet new xunit -n BeC.OpenId.Connect.Tests -o tests/BeC.OpenId.Connect.Tests
dotnet sln add tests/BeC.OpenId.Connect.Tests/BeC.OpenId.Connect.Tests.csproj

# Integration Test Project
dotnet new xunit -n BeC.OpenId.Connect.IntegrationTests -o tests/BeC.OpenId.Connect.IntegrationTests
dotnet sln add tests/BeC.OpenId.Connect.IntegrationTests/BeC.OpenId.Connect.IntegrationTests.csproj

# Add project references
cd tests/BeC.OpenId.Connect.Tests
dotnet add reference ../../BeC.OpenId.Connect.csproj

cd ../BeC.OpenId.Connect.IntegrationTests
dotnet add reference ../../BeC.OpenId.Connect.csproj
```

### 2. Install Required Testing Packages

#### Unit Tests
```bash
cd tests/BeC.OpenId.Connect.Tests
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package Moq
dotnet add package FluentAssertions
dotnet add package AutoFixture
dotnet add package AutoFixture.Xunit2
```

#### Integration Tests
```bash
cd tests/BeC.OpenId.Connect.IntegrationTests
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package FluentAssertions
dotnet add package Testcontainers.MsSql
dotnet add package Bogus  # For realistic test data generation
dotnet add package WireMock.Net  # For mocking external HTTP APIs
```

### 3. Project Structure

```
tests/
├── BeC.OpenId.Connect.Tests/
│   ├── Services/
│   │   ├── StripePaymentServiceTests.cs
│   │   ├── PricingCalculatorServiceTests.cs
│   │   ├── JobPaymentAutomationServiceTests.cs
│   │   └── GoogleMapsServiceTests.cs
│   ├── Controllers/
│   │   ├── JobsControllerTests.cs
│   │   ├── PaymentsControllerTests.cs
│   │   └── DriversControllerTests.cs
│   ├── Helpers/
│   │   └── TestDataBuilder.cs
│   └── Fixtures/
│       └── AutoMoqDataAttribute.cs
│
└── BeC.OpenId.Connect.IntegrationTests/
    ├── Fixtures/
    │   ├── WebApplicationFactory.cs
    │   └── DatabaseFixture.cs
    ├── Controllers/
    │   ├── JobsControllerIntegrationTests.cs
    │   └── PaymentsControllerIntegrationTests.cs
    ├── Workflows/
    │   ├── JobBookingWorkflowTests.cs
    │   └── PayoutWorkflowTests.cs
    └── appsettings.Test.json
```

---

## Testing Strategy

### Testing Pyramid

```
           /\
          /  \  E2E Tests (10%)
         /____\
        /      \  Integration Tests (30%)
       /________\
      /          \  Unit Tests (60%)
     /____________\
```

### Coverage Goals
- **Unit Tests**: 80%+ code coverage for business logic
- **Integration Tests**: All critical workflows and API endpoints
- **E2E Tests**: Happy paths for core user journeys

### Test Categories

1. **Fast Tests** (Unit): Run on every build (~milliseconds each)
2. **Medium Tests** (Integration): Run before commits (~seconds each)
3. **Slow Tests** (E2E): Run in CI/CD pipeline (~minutes)

---

## Unit Testing

### 1. Service Layer Testing

#### Example: PricingCalculatorService Tests

```csharp
using Xunit;
using Moq;
using FluentAssertions;
using BeC.OpenId.Connect.Services;
using BeC.OpenId.Connect.Data;
using BeC.OpenId.Connect.Models;

public class PricingCalculatorServiceTests
{
    private readonly Mock<IRepository<PricingRule>> _pricingRuleRepoMock;
    private readonly Mock<IGoogleMapsService> _mapsServiceMock;
    private readonly Mock<IRepository<PricingHistory>> _historyRepoMock;
    private readonly PricingCalculatorService _sut;

    public PricingCalculatorServiceTests()
    {
        _pricingRuleRepoMock = new Mock<IRepository<PricingRule>>();
        _mapsServiceMock = new Mock<IGoogleMapsService>();
        _historyRepoMock = new Mock<IRepository<PricingHistory>>();

        _sut = new PricingCalculatorService(
            _pricingRuleRepoMock.Object,
            _mapsServiceMock.Object,
            _historyRepoMock.Object
        );
    }

    [Fact]
    public async Task CalculatePrice_WithBaseFareOnly_ReturnsCorrectAmount()
    {
        // Arrange
        var rules = new List<PricingRule>
        {
            new PricingRule
            {
                RuleType = PricingRuleType.BaseFare,
                Amount = 10.00m,
                IsActive = true
            }
        };

        _pricingRuleRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<PricingRule, bool>>>()))
            .ReturnsAsync(rules);

        var request = new PriceCalculationRequest
        {
            PickupLocation = "123 Main St",
            DeliveryLocation = "456 Oak Ave",
            VehicleType = VehicleType.Sedan
        };

        // Act
        var result = await _sut.CalculatePriceAsync(request);

        // Assert
        result.TotalPrice.Should().Be(10.00m);
        result.BaseFare.Should().Be(10.00m);
        result.DistanceFare.Should().Be(0);
    }

    [Theory]
    [InlineData(5.0, 2.0, 10.0)]   // 5 miles * $2/mile = $10
    [InlineData(10.5, 2.0, 21.0)]  // 10.5 miles * $2/mile = $21
    [InlineData(0.5, 2.0, 1.0)]    // 0.5 miles * $2/mile = $1
    public async Task CalculatePrice_WithDistanceFare_CalculatesCorrectly(
        double distanceMiles,
        decimal ratePerMile,
        decimal expectedFare)
    {
        // Arrange
        var rules = new List<PricingRule>
        {
            new PricingRule
            {
                RuleType = PricingRuleType.DistanceBased,
                Amount = ratePerMile,
                IsActive = true
            }
        };

        _pricingRuleRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<PricingRule, bool>>>()))
            .ReturnsAsync(rules);

        _mapsServiceMock
            .Setup(m => m.CalculateDistanceAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(distanceMiles);

        var request = new PriceCalculationRequest
        {
            PickupLocation = "123 Main St",
            DeliveryLocation = "456 Oak Ave"
        };

        // Act
        var result = await _sut.CalculatePriceAsync(request);

        // Assert
        result.DistanceFare.Should().Be(expectedFare);
    }

    [Fact]
    public async Task CalculatePrice_WithSurgePricing_AppliesMultiplier()
    {
        // Arrange
        var baseFare = 10.00m;
        var surgeMultiplier = 1.5m;

        var rules = new List<PricingRule>
        {
            new PricingRule { RuleType = PricingRuleType.BaseFare, Amount = baseFare, IsActive = true },
            new PricingRule { RuleType = PricingRuleType.SurgePricing, Multiplier = surgeMultiplier, IsActive = true }
        };

        _pricingRuleRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<PricingRule, bool>>>()))
            .ReturnsAsync(rules);

        // Act
        var result = await _sut.CalculatePriceAsync(new PriceCalculationRequest());

        // Assert
        result.TotalPrice.Should().Be(baseFare * surgeMultiplier); // $15.00
        result.SurgeMultiplier.Should().Be(surgeMultiplier);
    }

    [Fact]
    public async Task CalculatePrice_SavesPricingHistory()
    {
        // Arrange
        _pricingRuleRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<PricingRule, bool>>>()))
            .ReturnsAsync(new List<PricingRule>());

        // Act
        await _sut.CalculatePriceAsync(new PriceCalculationRequest { JobId = 123 });

        // Assert
        _historyRepoMock.Verify(
            r => r.AddAsync(It.Is<PricingHistory>(h => h.JobId == 123)),
            Times.Once
        );
    }
}
```

#### Example: StripePaymentService Tests

```csharp
public class StripePaymentServiceTests
{
    [Fact]
    public async Task ProcessJobPayment_CreatesPaymentIntentWithCorrectAmount()
    {
        // Arrange
        var mockStripeClient = new Mock<IStripeClient>();
        var mockPaymentIntentService = new Mock<PaymentIntentService>();
        var mockRepository = new Mock<IRepository<Payment>>();

        var job = new Job
        {
            Id = 1,
            TotalPrice = 100.00m,
            CustomerId = "customer_123"
        };

        PaymentIntent capturedIntent = null;
        mockPaymentIntentService
            .Setup(s => s.CreateAsync(It.IsAny<PaymentIntentCreateOptions>(), null, default))
            .Callback<PaymentIntentCreateOptions, RequestOptions, CancellationToken>(
                (options, _, _) =>
                {
                    options.Amount.Should().Be(10000); // $100 in cents
                    options.Currency.Should().Be("usd");
                    options.Metadata["JobId"].Should().Be("1");
                })
            .ReturnsAsync(new PaymentIntent { Id = "pi_123", Status = "succeeded" });

        var sut = new StripePaymentService(
            mockPaymentIntentService.Object,
            mockRepository.Object
        );

        // Act
        var result = await sut.ProcessJobPaymentAsync(job);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        mockPaymentIntentService.Verify(
            s => s.CreateAsync(It.IsAny<PaymentIntentCreateOptions>(), null, default),
            Times.Once
        );
    }

    [Fact]
    public async Task CalculateCommission_Applies15PercentPlatformFee()
    {
        // Arrange
        var service = new StripePaymentService(null, null);
        var totalAmount = 100.00m;

        // Act
        var (platformFee, driverEarnings) = service.CalculateCommission(totalAmount);

        // Assert
        platformFee.Should().Be(15.00m);
        driverEarnings.Should().Be(85.00m);
    }

    [Theory]
    [InlineData(100.00, 15.00, 85.00)]
    [InlineData(50.00, 7.50, 42.50)]
    [InlineData(200.00, 30.00, 170.00)]
    [InlineData(33.33, 5.00, 28.33)] // Rounding test
    public async Task CalculateCommission_CalculatesCorrectSplit(
        decimal total,
        decimal expectedPlatform,
        decimal expectedDriver)
    {
        var service = new StripePaymentService(null, null);
        var (platformFee, driverEarnings) = service.CalculateCommission(total);

        platformFee.Should().Be(expectedPlatform);
        driverEarnings.Should().Be(expectedDriver);
    }
}
```

### 2. Controller Testing

```csharp
public class JobsControllerTests
{
    private readonly Mock<IRepository<Job>> _jobRepoMock;
    private readonly Mock<IPricingCalculatorService> _pricingServiceMock;
    private readonly Mock<IJobPaymentAutomationService> _paymentServiceMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly JobsController _sut;

    public JobsControllerTests()
    {
        _jobRepoMock = new Mock<IRepository<Job>>();
        _pricingServiceMock = new Mock<IPricingCalculatorService>();
        _paymentServiceMock = new Mock<IJobPaymentAutomationService>();
        _userManagerMock = MockUserManager();

        _sut = new JobsController(
            _jobRepoMock.Object,
            _pricingServiceMock.Object,
            _paymentServiceMock.Object,
            _userManagerMock.Object
        );

        // Set up fake user context
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "user_123")
                }))
            }
        };
    }

    [Fact]
    public async Task CreateJob_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var createDto = new CreateJobDto
        {
            PickupLocation = "123 Main St",
            DeliveryLocation = "456 Oak Ave",
            VehicleType = VehicleType.Sedan
        };

        _pricingServiceMock
            .Setup(p => p.CalculatePriceAsync(It.IsAny<PriceCalculationRequest>()))
            .ReturnsAsync(new PriceCalculationResult { TotalPrice = 50.00m });

        _jobRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Job>()))
            .ReturnsAsync((Job j) => j);

        // Act
        var result = await _sut.CreateJob(createDto);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        createdResult.Value.Should().BeOfType<JobDto>();
    }

    [Fact]
    public async Task AssignDriver_WithInvalidJobId_ReturnsNotFound()
    {
        // Arrange
        _jobRepoMock
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Job)null);

        // Act
        var result = await _sut.AssignDriver(999, "driver_123");

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateJobStatus_FromPendingToAssigned_SuccessfullyUpdates()
    {
        // Arrange
        var job = new Job
        {
            Id = 1,
            Status = JobStatus.Pending,
            CustomerId = "user_123"
        };

        _jobRepoMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(job);

        // Act
        var result = await _sut.UpdateJobStatus(1, JobStatus.Assigned);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        job.Status.Should().Be(JobStatus.Assigned);
        _jobRepoMock.Verify(r => r.UpdateAsync(job), Times.Once);
    }

    private Mock<UserManager<ApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object, null, null, null, null, null, null, null, null);
    }
}
```

### 3. What to Unit Test

✅ **DO Unit Test:**
- Business logic in services
- Pricing calculations
- Commission splits
- Data validation
- Authorization logic
- Mapping/transformation logic
- Helper methods and utilities

❌ **DON'T Unit Test:**
- Simple properties (auto-properties)
- Configuration classes
- Entity Framework entities (unless complex logic)
- DTOs without behavior
- Third-party library code

---

## Integration Testing

### 1. WebApplicationFactory Setup

```csharp
// IntegrationTestWebAppFactory.cs
public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove production DbContext
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add test database (Testcontainers)
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(GetTestConnectionString());
            });

            // Replace external services with test doubles
            services.Replace(ServiceDescriptor.Singleton<IStripePaymentService, FakeStripePaymentService>());
            services.Replace(ServiceDescriptor.Singleton<IGoogleMapsService, FakeGoogleMapsService>());
            services.Replace(ServiceDescriptor.Singleton<IEmailService, FakeEmailService>());

            // Build service provider and seed database
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
            SeedTestData(db);
        });
    }

    private void SeedTestData(ApplicationDbContext db)
    {
        // Seed test users, drivers, pricing rules, etc.
        var testUser = new ApplicationUser
        {
            Id = "test-customer-1",
            UserName = "testcustomer@test.com",
            Email = "testcustomer@test.com"
        };
        db.Users.Add(testUser);

        var testDriver = new Driver
        {
            Id = 1,
            UserId = "test-driver-1",
            LicenseNumber = "DL123456",
            Rating = 4.8m,
            IsAvailable = true
        };
        db.Drivers.Add(testDriver);

        var baseFareRule = new PricingRule
        {
            RuleType = PricingRuleType.BaseFare,
            Amount = 10.00m,
            IsActive = true
        };
        db.PricingRules.Add(baseFareRule);

        db.SaveChanges();
    }
}
```

### 2. Integration Test Examples

```csharp
public class JobsControllerIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly IntegrationTestWebAppFactory _factory;

    public JobsControllerIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateJob_EndToEnd_CreatesJobAndReturnsDto()
    {
        // Arrange
        var createDto = new CreateJobDto
        {
            PickupLocation = "123 Main St, New York, NY",
            DeliveryLocation = "456 Oak Ave, Brooklyn, NY",
            PickupDate = DateTime.UtcNow.AddDays(1),
            VehicleType = VehicleType.Sedan,
            Description = "Deliver furniture"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(createDto),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _client.PostAsync("/api/jobs", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var responseBody = await response.Content.ReadAsStringAsync();
        var jobDto = JsonSerializer.Deserialize<JobDto>(responseBody);

        jobDto.Should().NotBeNull();
        jobDto.PickupLocation.Should().Be(createDto.PickupLocation);
        jobDto.Status.Should().Be(JobStatus.Pending);
        jobDto.TotalPrice.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetJob_WithValidId_ReturnsJob()
    {
        // Arrange - Create a job first
        var jobId = await CreateTestJob();

        // Act
        var response = await _client.GetAsync($"/api/jobs/{jobId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var job = await response.Content.ReadAsAsync<JobDto>();
        job.Id.Should().Be(jobId);
    }

    [Fact]
    public async Task GetJob_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/jobs/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<int> CreateTestJob()
    {
        var createDto = new CreateJobDto
        {
            PickupLocation = "Test Pickup",
            DeliveryLocation = "Test Delivery",
            PickupDate = DateTime.UtcNow.AddDays(1),
            VehicleType = VehicleType.Sedan
        };

        var content = new StringContent(
            JsonSerializer.Serialize(createDto),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _client.PostAsync("/api/jobs", content);
        var job = await response.Content.ReadAsAsync<JobDto>();
        return job.Id;
    }
}
```

### 3. Database Integration Tests

```csharp
public class JobRepositoryIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly ApplicationDbContext _context;
    private readonly IRepository<Job> _repository;

    public JobRepositoryIntegrationTests(DatabaseFixture fixture)
    {
        _context = fixture.CreateContext();
        _repository = new Repository<Job>(_context);
    }

    [Fact]
    public async Task AddAsync_WithValidJob_SavesToDatabase()
    {
        // Arrange
        var job = new Job
        {
            CustomerId = "test-customer",
            PickupLocation = "123 Main St",
            DeliveryLocation = "456 Oak Ave",
            TotalPrice = 50.00m,
            Status = JobStatus.Pending
        };

        // Act
        var result = await _repository.AddAsync(job);
        await _context.SaveChangesAsync();

        // Assert
        result.Id.Should().BeGreaterThan(0);

        var savedJob = await _context.Jobs.FindAsync(result.Id);
        savedJob.Should().NotBeNull();
        savedJob.PickupLocation.Should().Be("123 Main St");
    }

    [Fact]
    public async Task GetAllAsync_WithFilter_ReturnsMatchingJobs()
    {
        // Arrange
        var customerId = "customer-123";
        await SeedJobs(customerId, 3);

        // Act
        var jobs = await _repository.GetAllAsync(j => j.CustomerId == customerId);

        // Assert
        jobs.Should().HaveCount(3);
        jobs.Should().OnlyContain(j => j.CustomerId == customerId);
    }

    private async Task SeedJobs(string customerId, int count)
    {
        for (int i = 0; i < count; i++)
        {
            await _repository.AddAsync(new Job
            {
                CustomerId = customerId,
                PickupLocation = $"Pickup {i}",
                DeliveryLocation = $"Delivery {i}",
                TotalPrice = 50.00m,
                Status = JobStatus.Pending
            });
        }
        await _context.SaveChangesAsync();
    }
}
```

---

## End-to-End Testing

### 1. E2E Test Framework Setup

Use **Playwright** or **Selenium** for browser automation:

```bash
dotnet new xunit -n BeC.OpenId.Connect.E2ETests
cd BeC.OpenId.Connect.E2ETests
dotnet add package Microsoft.Playwright
dotnet add package Microsoft.Playwright.NUnit
```

### 2. E2E Test Examples

```csharp
[TestFixture]
public class CustomerBookingFlowE2ETests : PageTest
{
    [Test]
    public async Task Customer_CanBookDeliveryJob_EndToEnd()
    {
        // Navigate to login
        await Page.GotoAsync("http://localhost:4200/login");

        // Login as customer
        await Page.FillAsync("#email", "customer@test.com");
        await Page.FillAsync("#password", "Test123!");
        await Page.ClickAsync("button[type=submit]");

        // Navigate to create job
        await Page.ClickAsync("a[href='/jobs/create']");

        // Fill job details
        await Page.FillAsync("#pickupLocation", "123 Main St, New York, NY");
        await Page.FillAsync("#deliveryLocation", "456 Oak Ave, Brooklyn, NY");
        await Page.SelectOptionAsync("#vehicleType", "Sedan");
        await Page.FillAsync("#description", "Deliver furniture");

        // Get price quote
        await Page.ClickAsync("#getPriceQuote");
        await Page.WaitForSelectorAsync("#estimatedPrice");

        var price = await Page.TextContentAsync("#estimatedPrice");
        price.Should().NotBeNullOrEmpty();

        // Confirm and enter payment
        await Page.ClickAsync("#confirmBooking");
        await Page.FillAsync("#cardNumber", "4242424242424242"); // Stripe test card
        await Page.FillAsync("#expiry", "12/25");
        await Page.FillAsync("#cvc", "123");

        // Submit payment
        await Page.ClickAsync("#submitPayment");

        // Verify success
        await Page.WaitForSelectorAsync(".booking-success");
        var successMessage = await Page.TextContentAsync(".booking-success");
        successMessage.Should().Contain("Job booked successfully");
    }

    [Test]
    public async Task Driver_CanCompleteJob_AndReceivePayout()
    {
        // 1. Login as driver
        await LoginAsDriver();

        // 2. Accept a pending job
        await AcceptJob();

        // 3. Update status to in progress
        await UpdateJobStatus("in_progress");

        // 4. Upload proof of delivery
        await UploadProofOfDelivery();

        // 5. Complete job
        await UpdateJobStatus("completed");

        // 6. Verify earnings appear in earnings list
        await Page.GotoAsync("http://localhost:4200/driver/earnings");
        var earnings = await Page.QuerySelectorAllAsync(".earning-item");
        earnings.Should().NotBeEmpty();
    }
}
```

---

## Testing Complex Features

### 1. Escrow Payment System Testing

#### Test Scenarios

```csharp
public class EscrowPaymentWorkflowTests
{
    [Fact]
    public async Task JobBooking_CapturesPaymentImmediately()
    {
        // Arrange
        var job = CreateTestJob(totalPrice: 100.00m);

        // Act
        var result = await _paymentService.ProcessJobPaymentAsync(job);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Payment.Status.Should().Be(PaymentStatus.Captured);
        result.Payment.Amount.Should().Be(100.00m);
        result.Payment.StripeChargeId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task JobCompletion_SplitsCommissionCorrectly()
    {
        // Arrange
        var job = CreateTestJob(totalPrice: 100.00m);
        await _paymentService.ProcessJobPaymentAsync(job);

        // Act
        await _jobPaymentAutomation.HandleJobCompletedAsync(job);

        // Assert
        var earnings = await _earningsRepo.GetAllAsync(e => e.JobId == job.Id);
        earnings.Should().ContainSingle();

        var earning = earnings.First();
        earning.Amount.Should().Be(85.00m); // 85% to driver
        earning.PlatformFee.Should().Be(15.00m); // 15% platform fee
    }

    [Fact]
    public async Task JobCancellation_IssuesFullRefund()
    {
        // Arrange
        var job = CreateTestJob(totalPrice: 100.00m);
        var payment = await _paymentService.ProcessJobPaymentAsync(job);

        // Act
        var refund = await _paymentService.RefundPaymentAsync(payment.Payment.Id);

        // Assert
        refund.IsSuccess.Should().BeTrue();
        refund.RefundAmount.Should().Be(100.00m);
        refund.Status.Should().Be(RefundStatus.Succeeded);
    }

    [Fact]
    public async Task WeeklyPayout_ProcessesAllPendingEarnings()
    {
        // Arrange - Create multiple completed jobs for a driver
        var driverId = 1;
        await CreateCompletedJobsForDriver(driverId, count: 5, earningsEach: 85.00m);

        // Act
        await _payoutScheduler.ProcessWeeklyPayoutsAsync();

        // Assert
        var payouts = await _payoutRepo.GetAllAsync(p => p.DriverId == driverId);
        payouts.Should().ContainSingle();

        var payout = payouts.First();
        payout.Amount.Should().Be(425.00m); // 5 * $85
        payout.Status.Should().Be(PayoutStatus.Pending);
        payout.StripeTransferId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PaymentIntent_IncludesCorrectMetadata()
    {
        // Arrange
        var job = CreateTestJob(jobId: 123, customerId: "cust_456", driverId: 789);

        // Act
        var payment = await _paymentService.ProcessJobPaymentAsync(job);

        // Assert
        payment.Payment.Metadata.Should().ContainKey("JobId");
        payment.Payment.Metadata["JobId"].Should().Be("123");
        payment.Payment.Metadata["CustomerId"].Should().Be("cust_456");
        payment.Payment.Metadata["DriverId"].Should().Be("789");
    }
}
```

### 2. Dynamic Pricing System Testing

```csharp
public class DynamicPricingSystemTests
{
    [Theory]
    [InlineData(PricingRuleType.BaseFare, 10.00, 0, 0, 10.00)]
    [InlineData(PricingRuleType.DistanceBased, 2.00, 5.0, 0, 10.00)]
    [InlineData(PricingRuleType.TimeBased, 1.00, 0, 15, 15.00)]
    public async Task CalculatePrice_WithSingleRule_ReturnsCorrectAmount(
        PricingRuleType ruleType,
        decimal amount,
        double distanceMiles,
        int estimatedMinutes,
        decimal expectedPrice)
    {
        // Arrange
        var rules = new List<PricingRule>
        {
            new PricingRule { RuleType = ruleType, Amount = amount, IsActive = true }
        };

        SetupPricingRules(rules);
        SetupMapsService(distanceMiles, estimatedMinutes);

        // Act
        var result = await _pricingService.CalculatePriceAsync(new PriceCalculationRequest());

        // Assert
        result.TotalPrice.Should().Be(expectedPrice);
    }

    [Fact]
    public async Task CalculatePrice_WithMultipleRules_CombinesCorrectly()
    {
        // Arrange
        var rules = new List<PricingRule>
        {
            new PricingRule { RuleType = PricingRuleType.BaseFare, Amount = 10.00m, IsActive = true },
            new PricingRule { RuleType = PricingRuleType.DistanceBased, Amount = 2.00m, IsActive = true },
            new PricingRule { RuleType = PricingRuleType.VehicleTypeSurcharge, Amount = 5.00m, VehicleType = VehicleType.Van, IsActive = true }
        };

        SetupPricingRules(rules);
        SetupMapsService(distanceMiles: 5.0);

        var request = new PriceCalculationRequest { VehicleType = VehicleType.Van };

        // Act
        var result = await _pricingService.CalculatePriceAsync(request);

        // Assert
        // $10 base + ($2 * 5 miles) + $5 vehicle surcharge = $25
        result.TotalPrice.Should().Be(25.00m);
        result.BaseFare.Should().Be(10.00m);
        result.DistanceFare.Should().Be(10.00m);
        result.VehicleSurcharge.Should().Be(5.00m);
    }

    [Fact]
    public async Task CalculatePrice_WithSurgePricing_AppliesMultiplierLast()
    {
        // Arrange
        var rules = new List<PricingRule>
        {
            new PricingRule { RuleType = PricingRuleType.BaseFare, Amount = 10.00m, IsActive = true },
            new PricingRule { RuleType = PricingRuleType.DistanceBased, Amount = 2.00m, IsActive = true },
            new PricingRule { RuleType = PricingRuleType.SurgePricing, Multiplier = 2.0m, IsActive = true }
        };

        SetupPricingRules(rules);
        SetupMapsService(distanceMiles: 5.0);

        // Act
        var result = await _pricingService.CalculatePriceAsync(new PriceCalculationRequest());

        // Assert
        // ($10 base + $10 distance) * 2.0 surge = $40
        result.TotalPrice.Should().Be(40.00m);
        result.SurgeMultiplier.Should().Be(2.0m);
    }

    [Fact]
    public async Task CalculatePrice_WithDiscount_SubtractsFromTotal()
    {
        // Arrange
        var rules = new List<PricingRule>
        {
            new PricingRule { RuleType = PricingRuleType.BaseFare, Amount = 50.00m, IsActive = true },
            new PricingRule { RuleType = PricingRuleType.Discount, Amount = 10.00m, Code = "SAVE10", IsActive = true }
        };

        SetupPricingRules(rules);

        var request = new PriceCalculationRequest { DiscountCode = "SAVE10" };

        // Act
        var result = await _pricingService.CalculatePriceAsync(request);

        // Assert
        result.TotalPrice.Should().Be(40.00m);
        result.DiscountAmount.Should().Be(10.00m);
    }

    [Fact]
    public async Task CalculatePrice_SavesPricingHistory()
    {
        // Arrange
        SetupPricingRules(new List<PricingRule>
        {
            new PricingRule { RuleType = PricingRuleType.BaseFare, Amount = 10.00m, IsActive = true }
        });

        var request = new PriceCalculationRequest { JobId = 123 };

        // Act
        var result = await _pricingService.CalculatePriceAsync(request);

        // Assert
        var history = await _historyRepo.GetAllAsync(h => h.JobId == 123);
        history.Should().ContainSingle();

        var record = history.First();
        record.BaseFare.Should().Be(10.00m);
        record.TotalPrice.Should().Be(10.00m);
        record.RulesApplied.Should().NotBeNullOrEmpty();
    }
}
```

### 3. Google Maps Integration Testing

```csharp
public class GoogleMapsServiceTests
{
    [Fact]
    public async Task GeocodeAddress_WithValidAddress_ReturnsCoordinates()
    {
        // Arrange
        var address = "1600 Amphitheatre Parkway, Mountain View, CA";

        // Use WireMock to mock Google Maps API
        var mockServer = WireMockServer.Start();
        mockServer
            .Given(Request.Create()
                .WithPath("/maps/api/geocode/json")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(GetGeocodeMockResponse()));

        var service = new GoogleMapsService(mockServer.Urls[0], "test-api-key");

        // Act
        var result = await service.GeocodeAddressAsync(address);

        // Assert
        result.Should().NotBeNull();
        result.Latitude.Should().BeApproximately(37.4224, 0.001);
        result.Longitude.Should().BeApproximately(-122.0842, 0.001);
    }

    [Fact]
    public async Task CalculateDistance_BetweenTwoPoints_ReturnsCorrectMiles()
    {
        // Arrange
        var origin = "123 Main St, New York, NY";
        var destination = "456 Oak Ave, Brooklyn, NY";

        var mockServer = SetupMockDistanceMatrixResponse(distanceMeters: 8046.72); // 5 miles
        var service = new GoogleMapsService(mockServer.Urls[0], "test-api-key");

        // Act
        var miles = await service.CalculateDistanceAsync(origin, destination);

        // Assert
        miles.Should().BeApproximately(5.0, 0.1);
    }

    [Fact]
    public async Task CalculateDistance_WithInvalidAddress_ThrowsException()
    {
        // Arrange
        var mockServer = WireMockServer.Start();
        mockServer
            .Given(Request.Create().WithPath("/maps/api/distancematrix/json"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("{\"status\": \"ZERO_RESULTS\"}"));

        var service = new GoogleMapsService(mockServer.Urls[0], "test-api-key");

        // Act & Assert
        await service.Invoking(s => s.CalculateDistanceAsync("invalid", "invalid"))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task AutocompleteAddress_ReturnsMatchingAddresses()
    {
        // Arrange
        var input = "123 Main";
        var mockServer = SetupMockAutocompleteResponse();
        var service = new GoogleMapsService(mockServer.Urls[0], "test-api-key");

        // Act
        var results = await service.AutocompleteAddressAsync(input);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.Contains("Main"));
    }
}
```

### 4. Job Status Workflow Testing

```csharp
public class JobStatusWorkflowTests
{
    [Theory]
    [InlineData(JobStatus.Pending, JobStatus.Assigned, true)]
    [InlineData(JobStatus.Assigned, JobStatus.InProgress, true)]
    [InlineData(JobStatus.InProgress, JobStatus.Completed, true)]
    [InlineData(JobStatus.Pending, JobStatus.Cancelled, true)]
    [InlineData(JobStatus.Completed, JobStatus.Pending, false)] // Invalid transition
    [InlineData(JobStatus.Cancelled, JobStatus.InProgress, false)] // Invalid transition
    public async Task UpdateJobStatus_ValidatesTransitions(
        JobStatus currentStatus,
        JobStatus newStatus,
        bool shouldSucceed)
    {
        // Arrange
        var job = new Job { Id = 1, Status = currentStatus };
        _jobRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(job);

        // Act
        var result = await _jobService.UpdateStatusAsync(1, newStatus);

        // Assert
        if (shouldSucceed)
        {
            result.IsSuccess.Should().BeTrue();
            job.Status.Should().Be(newStatus);
        }
        else
        {
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Invalid status transition");
        }
    }

    [Fact]
    public async Task UpdateToCompleted_TriggersPaymentDistribution()
    {
        // Arrange
        var job = new Job
        {
            Id = 1,
            Status = JobStatus.InProgress,
            TotalPrice = 100.00m,
            DriverId = 1
        };

        _jobRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(job);

        // Act
        await _jobService.UpdateStatusAsync(1, JobStatus.Completed);

        // Assert
        _paymentAutomationService.Verify(
            p => p.HandleJobCompletedAsync(It.Is<Job>(j => j.Id == 1)),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateToCancelled_TriggersRefund()
    {
        // Arrange
        var job = new Job
        {
            Id = 1,
            Status = JobStatus.Pending,
            TotalPrice = 100.00m
        };

        var payment = new Payment
        {
            Id = 1,
            JobId = 1,
            Amount = 100.00m,
            Status = PaymentStatus.Captured
        };

        _jobRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(job);
        _paymentRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Payment, bool>>>()))
            .ReturnsAsync(new[] { payment });

        // Act
        await _jobService.UpdateStatusAsync(1, JobStatus.Cancelled);

        // Assert
        _paymentService.Verify(
            p => p.RefundPaymentAsync(payment.Id),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateToInProgress_SendsNotificationToCustomer()
    {
        // Arrange
        var job = new Job
        {
            Id = 1,
            Status = JobStatus.Assigned,
            CustomerId = "customer_123"
        };

        _jobRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(job);

        // Act
        await _jobService.UpdateStatusAsync(1, JobStatus.InProgress);

        // Assert
        _notificationService.Verify(
            n => n.SendNotificationAsync(
                "customer_123",
                It.Is<string>(msg => msg.Contains("in progress")),
                NotificationType.JobUpdate
            ),
            Times.Once
        );
    }
}
```

### 5. Driver Payout Automation Testing

```csharp
public class PayoutSchedulerServiceTests
{
    [Fact]
    public async Task ProcessWeeklyPayouts_GroupsByDriver()
    {
        // Arrange
        var driver1Id = 1;
        var driver2Id = 2;

        // Create earnings for two drivers
        await CreateEarnings(driver1Id, new[] { 85.00m, 85.00m, 85.00m });
        await CreateEarnings(driver2Id, new[] { 100.00m, 50.00m });

        // Act
        await _payoutScheduler.ProcessWeeklyPayoutsAsync();

        // Assert
        var payouts = await _payoutRepo.GetAllAsync();
        payouts.Should().HaveCount(2);

        payouts.Should().Contain(p => p.DriverId == driver1Id && p.Amount == 255.00m);
        payouts.Should().Contain(p => p.DriverId == driver2Id && p.Amount == 150.00m);
    }

    [Fact]
    public async Task ProcessWeeklyPayouts_OnlyProcessesPaidEarnings()
    {
        // Arrange
        var driverId = 1;
        await CreateEarnings(driverId, new[] { 85.00m }, isPaid: true);
        await CreateEarnings(driverId, new[] { 85.00m }, isPaid: false);

        // Act
        await _payoutScheduler.ProcessWeeklyPayoutsAsync();

        // Assert
        var payouts = await _payoutRepo.GetAllAsync(p => p.DriverId == driverId);
        payouts.Should().ContainSingle();
        payouts.First().Amount.Should().Be(85.00m); // Only unpaid earnings
    }

    [Fact]
    public async Task ProcessWeeklyPayouts_SkipsDriversBelowMinimum()
    {
        // Arrange
        var driverId = 1;
        await CreateEarnings(driverId, new[] { 5.00m }); // Below $10 minimum

        // Act
        await _payoutScheduler.ProcessWeeklyPayoutsAsync();

        // Assert
        var payouts = await _payoutRepo.GetAllAsync(p => p.DriverId == driverId);
        payouts.Should().BeEmpty();

        var earnings = await _earningsRepo.GetAllAsync(e => e.DriverId == driverId);
        earnings.First().IsPaid.Should().BeFalse(); // Remains unpaid
    }

    [Fact]
    public async Task ProcessWeeklyPayouts_CreatesStripeTransfer()
    {
        // Arrange
        var driverId = 1;
        var driver = new Driver
        {
            Id = driverId,
            StripeAccountId = "acct_123"
        };

        await CreateEarnings(driverId, new[] { 100.00m });
        _driverRepo.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync(driver);

        // Act
        await _payoutScheduler.ProcessWeeklyPayoutsAsync();

        // Assert
        _stripeService.Verify(
            s => s.CreateTransferAsync("acct_123", 100.00m),
            Times.Once
        );
    }

    [Fact]
    public async Task ProcessWeeklyPayouts_MarksEarningsAsPaid()
    {
        // Arrange
        var driverId = 1;
        await CreateEarnings(driverId, new[] { 85.00m, 85.00m });

        // Act
        await _payoutScheduler.ProcessWeeklyPayoutsAsync();

        // Assert
        var earnings = await _earningsRepo.GetAllAsync(e => e.DriverId == driverId);
        earnings.Should().OnlyContain(e => e.IsPaid == true);
        earnings.Should().OnlyContain(e => e.PayoutId != null);
    }
}
```

### 6. Real-Time Notifications Testing

```csharp
public class NotificationServiceTests
{
    [Fact]
    public async Task SendNotification_WithUserPreferences_OnlySendsEnabledChannels()
    {
        // Arrange
        var userId = "user_123";
        var preferences = new NotificationPreferences
        {
            UserId = userId,
            EmailNotifications = true,
            PushNotifications = false,
            SmsNotifications = true
        };

        _preferencesRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<NotificationPreferences, bool>>>()))
            .ReturnsAsync(new[] { preferences });

        // Act
        await _notificationService.SendNotificationAsync(
            userId,
            "Test message",
            NotificationType.JobUpdate
        );

        // Assert
        _emailService.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _pushService.Verify(p => p.SendPushAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _smsService.Verify(s => s.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SendNotification_CreatesNotificationRecord()
    {
        // Arrange
        var userId = "user_123";

        // Act
        await _notificationService.SendNotificationAsync(userId, "Test", NotificationType.JobUpdate);

        // Assert
        _notificationRepo.Verify(
            r => r.AddAsync(It.Is<Notification>(n =>
                n.UserId == userId &&
                n.Message == "Test" &&
                n.Type == NotificationType.JobUpdate
            )),
            Times.Once
        );
    }

    [Fact]
    public async Task SendSignalRNotification_SendsToSpecificUser()
    {
        // Arrange
        var userId = "user_123";
        var mockHubContext = new Mock<IHubContext<NotificationHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();

        mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.User(userId)).Returns(mockClientProxy.Object);

        var service = new NotificationService(mockHubContext.Object, null, null, null, null);

        // Act
        await service.SendSignalRNotificationAsync(userId, "Test message");

        // Assert
        mockClientProxy.Verify(
            c => c.SendCoreAsync(
                "ReceiveNotification",
                It.Is<object[]>(o => o[0].ToString() == "Test message"),
                default
            ),
            Times.Once
        );
    }
}
```

### 7. Document Verification Testing

```csharp
public class DocumentServiceTests
{
    [Theory]
    [InlineData(DocumentType.License, ".jpg", true)]
    [InlineData(DocumentType.License, ".png", true)]
    [InlineData(DocumentType.License, ".pdf", true)]
    [InlineData(DocumentType.Insurance, ".exe", false)]
    [InlineData(DocumentType.Registration, ".txt", false)]
    public async Task UploadDocument_ValidatesFileType(
        DocumentType documentType,
        string extension,
        bool shouldSucceed)
    {
        // Arrange
        var file = CreateMockFile("document" + extension);

        // Act
        var result = await _documentService.UploadDocumentAsync(
            driverId: 1,
            documentType,
            file
        );

        // Assert
        if (shouldSucceed)
            result.IsSuccess.Should().BeTrue();
        else
            result.Errors.Should().Contain(e => e.Contains("file type"));
    }

    [Fact]
    public async Task UploadDocument_ExceedsSizeLimit_ReturnsError()
    {
        // Arrange
        var largeFile = CreateMockFile("document.jpg", sizeInMB: 11); // Limit is 10MB

        // Act
        var result = await _documentService.UploadDocumentAsync(
            driverId: 1,
            DocumentType.License,
            largeFile
        );

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("size"));
    }

    [Fact]
    public async Task VerifyDocument_UpdatesDocumentStatus()
    {
        // Arrange
        var document = new DriverDocument
        {
            Id = 1,
            DriverId = 1,
            DocumentType = DocumentType.License,
            Status = DocumentStatus.Pending
        };

        _documentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(document);

        // Act
        await _documentService.VerifyDocumentAsync(1, isApproved: true);

        // Assert
        document.Status.Should().Be(DocumentStatus.Verified);
        document.VerifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RejectDocument_SendsNotificationToDriver()
    {
        // Arrange
        var document = new DriverDocument
        {
            Id = 1,
            DriverId = 1,
            DocumentType = DocumentType.License
        };

        var driver = new Driver
        {
            Id = 1,
            UserId = "driver_user_123"
        };

        _documentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(document);
        _driverRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(driver);

        // Act
        await _documentService.VerifyDocumentAsync(1, isApproved: false, reason: "Blurry image");

        // Assert
        _notificationService.Verify(
            n => n.SendNotificationAsync(
                "driver_user_123",
                It.Is<string>(msg => msg.Contains("rejected")),
                NotificationType.DocumentUpdate
            ),
            Times.Once
        );
    }
}
```

### 8. Recurring Jobs Testing

```csharp
public class RecurringJobsTests
{
    [Fact]
    public async Task CreateRecurringJob_GeneratesJobsForNextMonth()
    {
        // Arrange
        var recurringJob = new RecurringJob
        {
            Id = 1,
            CustomerId = "customer_123",
            Frequency = RecurrenceFrequency.Weekly,
            DayOfWeek = DayOfWeek.Monday,
            JobTemplateId = 1
        };

        // Act
        var generatedJobs = await _recurringJobService.GenerateJobsAsync(
            recurringJob,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMonths(1)
        );

        // Assert
        generatedJobs.Should().HaveCount(4); // ~4 Mondays in a month
        generatedJobs.Should().OnlyContain(j => j.PickupDate.DayOfWeek == DayOfWeek.Monday);
    }

    [Theory]
    [InlineData(RecurrenceFrequency.Daily, 30)]
    [InlineData(RecurrenceFrequency.Weekly, 4)]
    [InlineData(RecurrenceFrequency.BiWeekly, 2)]
    [InlineData(RecurrenceFrequency.Monthly, 1)]
    public async Task GenerateJobs_CreatesCorrectNumberOfJobs(
        RecurrenceFrequency frequency,
        int expectedCount)
    {
        // Arrange
        var recurringJob = new RecurringJob
        {
            Frequency = frequency,
            DayOfWeek = DayOfWeek.Monday
        };

        // Act
        var jobs = await _recurringJobService.GenerateJobsAsync(
            recurringJob,
            new DateTime(2025, 1, 1),
            new DateTime(2025, 1, 31)
        );

        // Assert
        jobs.Should().HaveCountLessOrEqualTo(expectedCount + 1); // Allow for edge cases
    }

    [Fact]
    public async Task RecurringJob_CopiesTemplateData()
    {
        // Arrange
        var template = new JobTemplate
        {
            Id = 1,
            PickupLocation = "123 Main St",
            DeliveryLocation = "456 Oak Ave",
            VehicleType = VehicleType.Van,
            BasePrice = 75.00m
        };

        var recurringJob = new RecurringJob
        {
            JobTemplateId = 1,
            Frequency = RecurrenceFrequency.Weekly
        };

        _templateRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(template);

        // Act
        var jobs = await _recurringJobService.GenerateJobsAsync(
            recurringJob,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(7)
        );

        // Assert
        jobs.Should().OnlyContain(j =>
            j.PickupLocation == "123 Main St" &&
            j.DeliveryLocation == "456 Oak Ave" &&
            j.VehicleType == VehicleType.Van
        );
    }
}
```

---

## Mocking External Dependencies

### 1. Stripe API Mocking

```csharp
public class FakeStripePaymentService : IStripePaymentService
{
    private readonly List<PaymentIntent> _paymentIntents = new();
    private readonly List<Refund> _refunds = new();

    public async Task<PaymentResult> ProcessJobPaymentAsync(Job job)
    {
        var intent = new PaymentIntent
        {
            Id = $"pi_test_{Guid.NewGuid()}",
            Amount = (long)(job.TotalPrice * 100),
            Status = "succeeded",
            Created = DateTime.UtcNow
        };

        _paymentIntents.Add(intent);

        var payment = new Payment
        {
            JobId = job.Id,
            Amount = job.TotalPrice,
            StripeChargeId = intent.Id,
            Status = PaymentStatus.Captured
        };

        return new PaymentResult
        {
            IsSuccess = true,
            Payment = payment
        };
    }

    public async Task<RefundResult> RefundPaymentAsync(int paymentId)
    {
        var refund = new Refund
        {
            Id = $"re_test_{Guid.NewGuid()}",
            Amount = 10000,
            Status = "succeeded"
        };

        _refunds.Add(refund);

        return new RefundResult
        {
            IsSuccess = true,
            RefundAmount = 100.00m,
            Status = RefundStatus.Succeeded
        };
    }

    public (decimal platformFee, decimal driverEarnings) CalculateCommission(decimal totalAmount)
    {
        var platformFee = Math.Round(totalAmount * 0.15m, 2);
        var driverEarnings = totalAmount - platformFee;
        return (platformFee, driverEarnings);
    }
}
```

### 2. Google Maps API Mocking

```csharp
public class FakeGoogleMapsService : IGoogleMapsService
{
    public async Task<GeocodeResult> GeocodeAddressAsync(string address)
    {
        // Return fake coordinates for any address
        return new GeocodeResult
        {
            Latitude = 40.7128,
            Longitude = -74.0060,
            FormattedAddress = address
        };
    }

    public async Task<double> CalculateDistanceAsync(string origin, string destination)
    {
        // Return a consistent fake distance for testing
        return 5.0; // 5 miles
    }

    public async Task<int> EstimateDurationAsync(string origin, string destination)
    {
        // Return a consistent fake duration
        return 15; // 15 minutes
    }

    public async Task<List<string>> AutocompleteAddressAsync(string input)
    {
        return new List<string>
        {
            $"{input} - Address 1",
            $"{input} - Address 2",
            $"{input} - Address 3"
        };
    }
}
```

### 3. Email Service Mocking

```csharp
public class FakeEmailService : IEmailService
{
    public List<SentEmail> SentEmails { get; } = new();

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        SentEmails.Add(new SentEmail
        {
            To = to,
            Subject = subject,
            Body = body,
            SentAt = DateTime.UtcNow
        });
    }

    public void VerifyEmailSent(string to, Func<string, bool> subjectPredicate)
    {
        SentEmails.Should().Contain(e =>
            e.To == to && subjectPredicate(e.Subject));
    }
}

public class SentEmail
{
    public string To { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public DateTime SentAt { get; set; }
}
```

### 4. WireMock for HTTP Mocking

```csharp
public class GoogleMapsApiMockServer : IDisposable
{
    private readonly WireMockServer _server;

    public GoogleMapsApiMockServer()
    {
        _server = WireMockServer.Start();
        SetupDefaultMocks();
    }

    public string BaseUrl => _server.Urls[0];

    private void SetupDefaultMocks()
    {
        // Mock Geocoding API
        _server
            .Given(Request.Create()
                .WithPath("/maps/api/geocode/json")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""results"": [{
                        ""geometry"": {
                            ""location"": {
                                ""lat"": 40.7128,
                                ""lng"": -74.0060
                            }
                        },
                        ""formatted_address"": ""New York, NY, USA""
                    }],
                    ""status"": ""OK""
                }"));

        // Mock Distance Matrix API
        _server
            .Given(Request.Create()
                .WithPath("/maps/api/distancematrix/json")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(@"{
                    ""rows"": [{
                        ""elements"": [{
                            ""distance"": {
                                ""value"": 8046
                            },
                            ""duration"": {
                                ""value"": 900
                            }
                        }]
                    }],
                    ""status"": ""OK""
                }"));
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }
}
```

---

## Test Data Management

### 1. Using Bogus for Realistic Test Data

```csharp
public class TestDataGenerator
{
    private readonly Faker<ApplicationUser> _userFaker;
    private readonly Faker<Driver> _driverFaker;
    private readonly Faker<Job> _jobFaker;
    private readonly Faker<Vehicle> _vehicleFaker;

    public TestDataGenerator()
    {
        _userFaker = new Faker<ApplicationUser>()
            .RuleFor(u => u.Id, f => Guid.NewGuid().ToString())
            .RuleFor(u => u.UserName, f => f.Internet.Email())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.PhoneNumber, f => f.Phone.PhoneNumber());

        _driverFaker = new Faker<Driver>()
            .RuleFor(d => d.Id, f => f.IndexFaker)
            .RuleFor(d => d.LicenseNumber, f => f.Random.AlphaNumeric(10))
            .RuleFor(d => d.Rating, f => f.Random.Decimal(3.0m, 5.0m))
            .RuleFor(d => d.TotalEarnings, f => f.Random.Decimal(0, 10000))
            .RuleFor(d => d.TotalJobs, f => f.Random.Int(0, 500))
            .RuleFor(d => d.IsAvailable, f => f.Random.Bool());

        _jobFaker = new Faker<Job>()
            .RuleFor(j => j.Id, f => f.IndexFaker)
            .RuleFor(j => j.PickupLocation, f => f.Address.FullAddress())
            .RuleFor(j => j.DeliveryLocation, f => f.Address.FullAddress())
            .RuleFor(j => j.PickupDate, f => f.Date.Future())
            .RuleFor(j => j.TotalPrice, f => f.Random.Decimal(20, 500))
            .RuleFor(j => j.Status, f => f.PickRandom<JobStatus>())
            .RuleFor(j => j.VehicleType, f => f.PickRandom<VehicleType>())
            .RuleFor(j => j.Description, f => f.Lorem.Sentence());

        _vehicleFaker = new Faker<Vehicle>()
            .RuleFor(v => v.Id, f => f.IndexFaker)
            .RuleFor(v => v.Make, f => f.Vehicle.Manufacturer())
            .RuleFor(v => v.Model, f => f.Vehicle.Model())
            .RuleFor(v => v.Year, f => f.Date.Past(10).Year)
            .RuleFor(v => v.LicensePlate, f => f.Random.AlphaNumeric(7))
            .RuleFor(v => v.VehicleType, f => f.PickRandom<VehicleType>())
            .RuleFor(v => v.Color, f => f.Commerce.Color());
    }

    public ApplicationUser GenerateUser() => _userFaker.Generate();
    public List<ApplicationUser> GenerateUsers(int count) => _userFaker.Generate(count);

    public Driver GenerateDriver() => _driverFaker.Generate();
    public List<Driver> GenerateDrivers(int count) => _driverFaker.Generate(count);

    public Job GenerateJob() => _jobFaker.Generate();
    public List<Job> GenerateJobs(int count) => _jobFaker.Generate(count);

    public Vehicle GenerateVehicle() => _vehicleFaker.Generate();
    public List<Vehicle> GenerateVehicles(int count) => _vehicleFaker.Generate(count);
}
```

### 2. Database Seeding for Tests

```csharp
public static class TestDataSeeder
{
    public static async Task SeedTestDatabase(ApplicationDbContext context)
    {
        var generator = new TestDataGenerator();

        // Seed users
        var customers = generator.GenerateUsers(10);
        var driverUsers = generator.GenerateUsers(5);
        context.Users.AddRange(customers);
        context.Users.AddRange(driverUsers);
        await context.SaveChangesAsync();

        // Seed drivers
        var drivers = generator.GenerateDrivers(5);
        for (int i = 0; i < drivers.Count; i++)
        {
            drivers[i].UserId = driverUsers[i].Id;
        }
        context.Drivers.AddRange(drivers);
        await context.SaveChangesAsync();

        // Seed vehicles
        var vehicles = generator.GenerateVehicles(10);
        foreach (var vehicle in vehicles)
        {
            vehicle.DriverId = drivers[Random.Shared.Next(0, drivers.Count)].Id;
        }
        context.Vehicles.AddRange(vehicles);

        // Seed pricing rules
        var pricingRules = new List<PricingRule>
        {
            new() { RuleType = PricingRuleType.BaseFare, Amount = 10.00m, IsActive = true },
            new() { RuleType = PricingRuleType.DistanceBased, Amount = 2.00m, IsActive = true },
            new() { RuleType = PricingRuleType.TimeBased, Amount = 1.00m, IsActive = true }
        };
        context.PricingRules.AddRange(pricingRules);

        // Seed jobs
        var jobs = generator.GenerateJobs(50);
        foreach (var job in jobs)
        {
            job.CustomerId = customers[Random.Shared.Next(0, customers.Count)].Id;
            if (job.Status != JobStatus.Pending)
            {
                job.DriverId = drivers[Random.Shared.Next(0, drivers.Count)].Id;
            }
        }
        context.Jobs.AddRange(jobs);

        await context.SaveChangesAsync();
    }
}
```

---

## CI/CD Integration

### 1. GitHub Actions Workflow

```yaml
# .github/workflows/tests.yml
name: Run Tests

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]

jobs:
  unit-tests:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore

    - name: Run Unit Tests
      run: dotnet test tests/BeC.OpenId.Connect.Tests/BeC.OpenId.Connect.Tests.csproj --no-build --verbosity normal --collect:"XPlat Code Coverage"

    - name: Upload Coverage Reports
      uses: codecov/codecov-action@v3
      with:
        files: '**/coverage.cobertura.xml'

  integration-tests:
    runs-on: ubuntu-latest

    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2022-latest
        env:
          SA_PASSWORD: YourStrong@Passw0rd
          ACCEPT_EULA: Y
        ports:
          - 1433:1433
        options: >-
          --health-cmd "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourStrong@Passw0rd -Q 'SELECT 1'"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore

    - name: Run Integration Tests
      run: dotnet test tests/BeC.OpenId.Connect.IntegrationTests/BeC.OpenId.Connect.IntegrationTests.csproj --no-build --verbosity normal
      env:
        ConnectionStrings__DefaultConnection: "Server=localhost,1433;Database=BeC_Test;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
```

### 2. Test Coverage Configuration

```xml
<!-- coverlet.runsettings -->
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat Code Coverage">
        <Configuration>
          <Format>cobertura,opencover</Format>
          <Exclude>[*.Tests]*,[*]*.Migrations.*</Exclude>
          <ExcludeByAttribute>Obsolete,GeneratedCodeAttribute,CompilerGeneratedAttribute</ExcludeByAttribute>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

### 3. Running Tests Locally

```bash
# Run all tests
dotnet test

# Run unit tests only
dotnet test tests/BeC.OpenId.Connect.Tests

# Run integration tests only
dotnet test tests/BeC.OpenId.Connect.IntegrationTests

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# Run specific test
dotnet test --filter "FullyQualifiedName~PricingCalculatorServiceTests"

# Run tests by category
dotnet test --filter "Category=Payment"

# Generate coverage report
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage-report -reporttypes:Html
```

---

## Performance Testing

### 1. Load Testing with NBomber

```bash
dotnet add package NBomber
dotnet add package NBomber.Http
```

```csharp
public class LoadTests
{
    [Test]
    public void LoadTest_JobCreation_Handles100ConcurrentRequests()
    {
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("http://localhost:5000");

        var scenario = Scenario.Create("job_creation_load_test", async context =>
        {
            var createDto = new CreateJobDto
            {
                PickupLocation = "123 Main St",
                DeliveryLocation = "456 Oak Ave",
                VehicleType = VehicleType.Sedan
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto),
                Encoding.UTF8,
                "application/json"
            );

            var response = await httpClient.PostAsync("/api/jobs", content);

            return response.IsSuccessStatusCode
                ? Response.Ok()
                : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 100, during: TimeSpan.FromSeconds(30))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // Assert performance metrics
        var jobCreationStats = stats.ScenarioStats[0];
        jobCreationStats.Ok.Request.RPS.Should().BeGreaterThan(90); // At least 90 RPS
        jobCreationStats.Ok.Latency.Percent99.Should().BeLessThan(1000); // P99 < 1s
    }
}
```

### 2. Database Performance Testing

```csharp
[Fact]
public async Task PricingCalculation_With1000Rules_CompletesUnder100ms()
{
    // Arrange
    var stopwatch = Stopwatch.StartNew();
    await SeedPricingRules(1000);

    // Act
    stopwatch.Restart();
    var result = await _pricingService.CalculatePriceAsync(new PriceCalculationRequest());
    stopwatch.Stop();

    // Assert
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
}

[Fact]
public async Task GetDriverJobs_With10000Jobs_UsesEfficientQuery()
{
    // Arrange
    await SeedJobs(driverId: 1, count: 10000);

    // Act
    var stopwatch = Stopwatch.StartNew();
    var jobs = await _jobRepo.GetAllAsync(j => j.DriverId == 1);
    stopwatch.Stop();

    // Assert
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(500);
    jobs.Should().HaveCount(10000);
}
```

---

## Security Testing

### 1. Authorization Testing

```csharp
public class AuthorizationTests
{
    [Fact]
    public async Task Customer_CannotAccessOtherCustomersJobs()
    {
        // Arrange
        var customer1 = await CreateCustomerUser("customer1@test.com");
        var customer2 = await CreateCustomerUser("customer2@test.com");

        var job = await CreateJob(customerId: customer2.Id);

        AuthenticateAs(customer1);

        // Act
        var response = await _client.GetAsync($"/api/jobs/{job.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Driver_CanOnlyUpdateAssignedJobs()
    {
        // Arrange
        var driver1 = await CreateDriver();
        var driver2 = await CreateDriver();

        var job = await CreateJob(driverId: driver2.Id);

        AuthenticateAs(driver1.UserId);

        // Act
        var response = await _client.PutAsync(
            $"/api/jobs/{job.Id}/status",
            new StringContent(JsonSerializer.Serialize(new { status = "in_progress" }))
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminOnly_CanAccessUserManagement()
    {
        // Arrange
        var customer = await CreateCustomerUser("customer@test.com");
        AuthenticateAs(customer);

        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
```

### 2. Input Validation Testing

```csharp
public class InputValidationTests
{
    [Theory]
    [InlineData("")] // Empty
    [InlineData(null)] // Null
    [InlineData("   ")] // Whitespace
    public async Task CreateJob_WithInvalidPickupLocation_ReturnsBadRequest(string invalidLocation)
    {
        // Act
        var response = await CreateJobWithPickupLocation(invalidLocation);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateJob_WithSQLInjection_IsSanitized()
    {
        // Arrange
        var maliciousInput = "'; DROP TABLE Jobs; --";

        // Act
        var response = await CreateJobWithDescription(maliciousInput);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify database wasn't affected
        var jobsTable = await _context.Jobs.ToListAsync();
        jobsTable.Should().NotBeEmpty(); // Table still exists
    }

    [Fact]
    public async Task CreateJob_WithXSSPayload_IsEscaped()
    {
        // Arrange
        var xssPayload = "<script>alert('XSS')</script>";

        // Act
        var response = await CreateJobWithDescription(xssPayload);

        // Assert
        var job = await response.Content.ReadAsAsync<JobDto>();
        job.Description.Should().NotContain("<script>");
        job.Description.Should().Contain("&lt;script&gt;"); // HTML encoded
    }
}
```

### 3. Rate Limiting Tests

```csharp
public class RateLimitingTests
{
    [Fact]
    public async Task API_EnforcesRateLimit_After100RequestsPerMinute()
    {
        // Arrange
        var requests = new List<Task<HttpResponseMessage>>();

        // Act - Send 101 requests rapidly
        for (int i = 0; i < 101; i++)
        {
            requests.Add(_client.GetAsync("/api/jobs"));
        }

        var responses = await Task.WhenAll(requests);

        // Assert
        var tooManyRequests = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        tooManyRequests.Should().BeGreaterThan(0);
    }
}
```

---

## Best Practices & Tips

### 1. Test Naming Conventions

```
MethodName_StateUnderTest_ExpectedBehavior

Examples:
- CalculatePrice_WithBaseFareOnly_ReturnsCorrectAmount
- ProcessPayment_WithInvalidCard_ThrowsException
- GetJobs_ForUnauthenticatedUser_ReturnsUnauthorized
```

### 2. AAA Pattern (Arrange, Act, Assert)

```csharp
[Fact]
public async Task Example_Test()
{
    // Arrange - Set up test data and dependencies
    var mockRepo = new Mock<IRepository<Job>>();
    var service = new JobService(mockRepo.Object);

    // Act - Execute the method being tested
    var result = await service.GetJobAsync(1);

    // Assert - Verify the expected outcome
    result.Should().NotBeNull();
    result.Id.Should().Be(1);
}
```

### 3. Use Test Categories

```csharp
[Trait("Category", "Unit")]
[Trait("Category", "Payment")]
public class PaymentServiceTests { }

[Trait("Category", "Integration")]
[Trait("Category", "Database")]
public class JobRepositoryTests { }

// Run specific categories:
// dotnet test --filter "Category=Unit"
// dotnet test --filter "Category=Payment"
```

### 4. Parallel Test Execution

```csharp
// Enable parallel execution in xUnit
[assembly: CollectionBehavior(DisableTestParallelization = false, MaxParallelThreads = 4)]

// Disable for specific test classes that share state
[Collection("Database")]
public class DatabaseTests { }
```

### 5. Test Data Builders

```csharp
public class JobBuilder
{
    private Job _job = new Job
    {
        PickupLocation = "Default Pickup",
        DeliveryLocation = "Default Delivery",
        TotalPrice = 50.00m,
        Status = JobStatus.Pending
    };

    public JobBuilder WithCustomer(string customerId)
    {
        _job.CustomerId = customerId;
        return this;
    }

    public JobBuilder WithDriver(int driverId)
    {
        _job.DriverId = driverId;
        return this;
    }

    public JobBuilder WithStatus(JobStatus status)
    {
        _job.Status = status;
        return this;
    }

    public Job Build() => _job;
}

// Usage:
var job = new JobBuilder()
    .WithCustomer("customer_123")
    .WithDriver(1)
    .WithStatus(JobStatus.Completed)
    .Build();
```

---

## Next Steps

1. **Start with Unit Tests**: Begin by creating the unit test project and testing core business logic
2. **Add Integration Tests**: Set up the integration test project with WebApplicationFactory
3. **Implement CI/CD**: Configure GitHub Actions to run tests automatically
4. **Measure Coverage**: Aim for 80%+ coverage on business logic
5. **Add E2E Tests**: Implement critical user flows once unit and integration tests are solid
6. **Performance Testing**: Add load tests for critical endpoints
7. **Security Testing**: Ensure proper authorization and input validation

---

## Useful Resources

- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [ASP.NET Core Integration Testing](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [Bogus Documentation](https://github.com/bchavez/Bogus)
- [WireMock.Net Documentation](https://github.com/WireMock-Net/WireMock.Net)
- [NBomber Documentation](https://nbomber.com/)
