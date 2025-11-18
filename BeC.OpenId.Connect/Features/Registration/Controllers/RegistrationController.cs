using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Users.Dtos;
using BeC.OpenId.Connect.Features.Drivers.Dtos;
using BeC.OpenId.Connect.Features.Customers.Dtos;
using BeC.OpenId.Connect.Features.Registration.Dtos;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using BeC.Common.Data.Repositories.Interfaces;
using AuthRoles = BeC.OpenId.Connect.Infrastructure.Authorization.Roles;

namespace BeC.OpenId.Connect.Features.Registration.Controllers;

/// <summary>
/// Self-registration endpoints for customers and drivers
/// Supports UK-based vetting workflow with approval process
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class RegistrationController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IRepository _repository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<RegistrationController> _logger;

    public RegistrationController(
        ApplicationDbContext context,
        IRepository repository,
        UserManager<ApplicationUser> userManager,
        IActivityLogService activityLogService,
        ILogger<RegistrationController> logger)
    {
        _context = context;
        _repository = repository;
        _userManager = userManager;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    #region Customer Registration

    /// <summary>
    /// Customer self-registration endpoint
    /// Creates account in pending approval status
    /// </summary>
    [HttpPost("customer")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegistrationResponse>> RegisterCustomer([FromBody] CustomerRegistrationRequest request)
    {
        // Validate terms acceptance
        if (!request.AcceptedTerms || !request.AcceptedPrivacyPolicy)
        {
            return BadRequest(new { message = "You must accept the terms and conditions and privacy policy" });
        }

        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return BadRequest(new { message = "A user with this email already exists" });
        }

        // Create ApplicationUser
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = $"{request.FirstName} {request.LastName}",
            PhoneNumber = request.Phone,
            EmailConfirmed = false
        };

        var createUserResult = await _userManager.CreateAsync(user, request.Password);
        if (!createUserResult.Succeeded)
        {
            return BadRequest(new {
                message = "Failed to create user account",
                errors = createUserResult.Errors.Select(e => e.Description)
            });
        }

        // Assign Customer role
        await _userManager.AddToRoleAsync(user, AuthRoles.Customer);

        // Create Customer profile
        var customer = new Customer
        {
            UserId = user.Id,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            CustomerType = request.CustomerType,
            CompanyName = request.CompanyName,
            PrimaryAddress = request.PrimaryAddress != null ? JsonSerializer.Serialize(request.PrimaryAddress) : null,
            ApprovalStatus = "pending",
            Status = "pending",
            EmailVerified = false,
            PhoneVerified = false
        };

        await _repository.InsertEntity(customer);

        await _activityLogService.LogActivityAsync(
            user.Id,
            "customer_registered",
            "Customer",
            customer.Id.ToString(),
            request.Email,
            $"Customer {request.FirstName} {request.LastName} registered - pending approval"
        );

        _logger.LogInformation("New customer registration: {Email}", request.Email);

        // TODO: Send email verification
        // TODO: Notify admins of new customer registration

        return CreatedAtAction(nameof(GetRegistrationStatus), new { userId = user.Id }, new RegistrationResponse
        {
            UserId = user.Id,
            Email = user.Email!,
            CustomerId = customer.Id.ToString(),
            ApprovalStatus = "pending",
            Message = "Registration successful! Your account is pending approval. You'll receive an email once approved.",
            EmailVerificationRequired = true
        });
    }

    #endregion

    #region Driver Registration

    /// <summary>
    /// Driver self-registration endpoint with KYC
    /// Creates account in pending approval status
    /// Requires document upload via separate endpoint
    /// </summary>
    [HttpPost("driver")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegistrationResponse>> RegisterDriver([FromBody] DriverRegistrationRequest request)
    {
        // Validate terms and consent
        if (!request.AcceptedTerms || !request.AcceptedPrivacyPolicy)
        {
            return BadRequest(new { message = "You must accept the terms and conditions and privacy policy" });
        }

        if (!request.ConsentBackgroundCheck)
        {
            return BadRequest(new { message = "Background check consent is required for driver registration" });
        }

        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return BadRequest(new { message = "A user with this email already exists" });
        }

        // Check if license already registered
        var existingDriver = await _repository.GetEntity<Driver>(d => d.LicenseNumber == request.LicenseNumber);
        if (existingDriver != null)
        {
            return BadRequest(new { message = "This driving license is already registered" });
        }

        // Validate license expiry
        if (request.LicenseExpiry <= DateTime.UtcNow.AddMonths(3))
        {
            return BadRequest(new { message = "Driving license must be valid for at least 3 months" });
        }

        // Create ApplicationUser
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = $"{request.FirstName} {request.LastName}",
            PhoneNumber = request.Phone,
            EmailConfirmed = false
        };

        var createUserResult = await _userManager.CreateAsync(user, request.Password);
        if (!createUserResult.Succeeded)
        {
            return BadRequest(new {
                message = "Failed to create user account",
                errors = createUserResult.Errors.Select(e => e.Description)
            });
        }

        // Assign Driver role
        await _userManager.AddToRoleAsync(user, AuthRoles.Driver);

        // Create Driver profile
        var driver = new Driver
        {
            UserId = user.Id,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            LicenseNumber = request.LicenseNumber,
            LicenseExpiry = request.LicenseExpiry,
            DrivingLicenseType = request.DrivingLicenseType,
            NationalInsuranceNumber = request.NationalInsuranceNumber,
            RightToWorkExpiry = request.RightToWorkExpiry,
            Address = JsonSerializer.Serialize(request.Address),
            EmergencyContact = request.EmergencyContact != null ? JsonSerializer.Serialize(request.EmergencyContact) : null,
            ApprovalStatus = "pending",
            Status = "inactive",
            BackgroundCheckCompleted = false,
            RightToWorkVerified = false,
            ProofOfAddressVerified = false
        };

        await _repository.InsertEntity(driver);

        await _activityLogService.LogActivityAsync(
            user.Id,
            "driver_registered",
            "Driver",
            driver.Id.ToString(),
            request.Email,
            $"Driver {request.FirstName} {request.LastName} registered - pending vetting and approval"
        );

        _logger.LogInformation("New driver registration: {Email}, License: {LicenseNumber}",
            request.Email, request.LicenseNumber);

        // TODO: Send email verification
        // TODO: Send document upload instructions
        // TODO: Notify admins of new driver registration for vetting

        return CreatedAtAction(nameof(GetRegistrationStatus), new { userId = user.Id }, new RegistrationResponse
        {
            UserId = user.Id,
            Email = user.Email!,
            DriverId = driver.Id.ToString(),
            ApprovalStatus = "pending",
            Message = "Registration successful! Please upload your documents for verification. Your account will be reviewed within 24-48 hours.",
            EmailVerificationRequired = true
        });
    }

    #endregion

    #region Vehicle Registration

    /// <summary>
    /// Register a vehicle for an approved driver
    /// Vehicle will be pending approval until documents are verified
    /// </summary>
    [HttpPost("driver/vehicle")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Authorize(Roles = AuthRoles.Driver)]
    [ProducesResponseType(typeof(Vehicle), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Vehicle>> RegisterVehicle([FromBody] VehicleRegistrationRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Get driver profile
        var driver = await _repository.GetEntity<Driver>(d => d.UserId == userId);
        if (driver == null)
        {
            return NotFound(new { message = "Driver profile not found" });
        }

        // Check if driver is approved (optional - you may allow vehicle registration before driver approval)
        // Uncomment if you want to enforce driver approval before vehicle registration
        // if (driver.ApprovalStatus != "approved")
        // {
        //     return BadRequest(new { message = "Your driver account must be approved before registering a vehicle" });
        // }

        // Check if vehicle registration already exists
        var existingVehicle = await _repository.GetEntity<Vehicle>(
            v => v.RegistrationNumber == request.RegistrationNumber
        );
        if (existingVehicle != null)
        {
            return BadRequest(new { message = "This vehicle registration number is already in the system" });
        }

        // Validate UK requirements
        if (request.MOTExpiryDate <= DateTime.UtcNow)
        {
            return BadRequest(new { message = "Vehicle MOT has expired" });
        }

        if (request.InsuranceExpiry <= DateTime.UtcNow)
        {
            return BadRequest(new { message = "Vehicle insurance has expired" });
        }

        if (request.RoadTaxExpiryDate <= DateTime.UtcNow)
        {
            return BadRequest(new { message = "Vehicle road tax has expired" });
        }

        // For commercial use, Hire & Reward insurance is required
        if (!request.HireAndRewardInsurance)
        {
            _logger.LogWarning("Vehicle registered without Hire & Reward insurance: {Reg}", request.RegistrationNumber);
        }

        // Create vehicle
        var vehicle = new Vehicle
        {
            DriverId = driver.Id,
            Type = request.Type,
            Make = request.Make,
            Model = request.Model,
            Year = request.Year,
            RegistrationNumber = request.RegistrationNumber.ToUpper().Replace(" ", ""),
            VinNumber = request.VinNumber,
            CargoCapacity = request.CargoCapacity,
            MaxPayloadWeight = request.MaxPayloadWeight,
            CargoLength = request.CargoLength,
            CargoWidth = request.CargoWidth,
            CargoHeight = request.CargoHeight,
            Features = request.Features != null ? JsonSerializer.Serialize(request.Features) : null,

            // UK Requirements
            HasValidMOT = true,
            MOTExpiryDate = request.MOTExpiryDate,
            MOTCertificateNumber = request.MOTCertificateNumber,
            HasInsurance = true,
            InsuranceExpiry = request.InsuranceExpiry,
            InsuranceProvider = request.InsuranceProvider,
            InsurancePolicyNumber = request.InsurancePolicyNumber,
            InsuranceType = request.InsuranceType,
            HasValidRoadTax = true,
            RoadTaxExpiryDate = request.RoadTaxExpiryDate,
            HireAndRewardInsurance = request.HireAndRewardInsurance,
            GoodsInTransitInsurance = request.GoodsInTransitInsurance,
            PublicLiabilityInsurance = request.PublicLiabilityInsurance,
            PublicLiabilityExpiry = request.PublicLiabilityExpiry,

            ApprovalStatus = "pending",
            Status = "pending_approval",
            IsActive = false
        };

        await _repository.InsertEntity(vehicle);

        await _activityLogService.LogActivityAsync(
            userId,
            "vehicle_registered",
            "Vehicle",
            vehicle.Id.ToString(),
            request.RegistrationNumber,
            $"Driver {driver.FirstName} {driver.LastName} registered vehicle {request.Make} {request.Model} ({request.RegistrationNumber}) - pending approval"
        );

        _logger.LogInformation("Vehicle registered: {Reg} by driver {DriverId}",
            request.RegistrationNumber, driver.Id);

        // TODO: Send notification to driver about document upload requirements
        // TODO: Notify admins of new vehicle registration for vetting

        return CreatedAtAction(nameof(GetVehicle), new { id = vehicle.Id }, vehicle);
    }

    /// <summary>
    /// Get vehicle details
    /// </summary>
    [HttpGet("driver/vehicle/{id}")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Authorize(Roles = AuthRoles.Driver)]
    [ProducesResponseType(typeof(Vehicle), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Vehicle>> GetVehicle(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var driver = await _repository.GetEntity<Driver>(d => d.UserId == userId);
        if (driver == null)
            return NotFound(new { message = "Driver profile not found" });

        var vehicle = await _repository.GetEntity<Vehicle>(v => v.Id == id && v.DriverId == driver.Id);
        if (vehicle == null)
            return NotFound(new { message = "Vehicle not found" });

        return Ok(vehicle);
    }

    #endregion

    #region Document Upload

    /// <summary>
    /// Upload driver or vehicle document
    /// Documents are stored in cloud storage (e.g., Azure Blob, AWS S3)
    /// This endpoint records the document metadata after upload
    /// </summary>
    [HttpPost("driver/documents")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Authorize(Roles = AuthRoles.Driver)]
    [ProducesResponseType(typeof(DriverDocument), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DriverDocument>> UploadDocument([FromBody] DocumentUploadRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var driver = await _repository.GetEntity<Driver>(d => d.UserId == userId);
        if (driver == null)
        {
            return NotFound(new { message = "Driver profile not found" });
        }

        // Validate document type
        var validDocumentTypes = new[]
        {
            "driving_license", "driving_license_counterpart", "proof_of_address",
            "national_insurance", "right_to_work", "dbs_check", "profile_photo",
            "vehicle_mot_certificate", "vehicle_insurance_certificate",
            "vehicle_road_tax_certificate", "vehicle_registration_v5c",
            "vehicle_hire_reward_insurance", "vehicle_goods_in_transit_insurance",
            "vehicle_public_liability_insurance", "vehicle_photo_front",
            "vehicle_photo_back", "vehicle_photo_side", "vehicle_photo_interior"
        };

        if (!validDocumentTypes.Contains(request.Type))
        {
            return BadRequest(new { message = $"Invalid document type: {request.Type}" });
        }

        // If vehicle document, validate vehicle exists and belongs to driver
        if (request.Type.StartsWith("vehicle_") && request.VehicleId.HasValue)
        {
            var vehicle = await _repository.GetEntity<Vehicle>(
                v => v.Id == request.VehicleId.Value && v.DriverId == driver.Id
            );
            if (vehicle == null)
            {
                return BadRequest(new { message = "Vehicle not found or does not belong to you" });
            }
        }

        // Create document record
        var document = new DriverDocument
        {
            DriverId = driver.Id,
            Type = request.Type,
            FileName = request.FileName,
            FileUrl = request.FileUrl,
            FileSize = request.FileSize,
            MimeType = request.MimeType,
            ExpiryDate = request.ExpiryDate,
            DocumentNumber = request.DocumentNumber,
            IssueDate = request.IssueDate,
            IssuingAuthority = request.IssuingAuthority,
            VehicleId = request.VehicleId,
            Status = "pending"
        };

        await _repository.InsertEntity(document);

        await _activityLogService.LogActivityAsync(
            userId,
            "document_uploaded",
            "DriverDocument",
            document.Id.ToString(),
            request.Type,
            $"Driver {driver.FirstName} {driver.LastName} uploaded {request.Type} document - pending verification"
        );

        _logger.LogInformation("Document uploaded: {Type} by driver {DriverId}",
            request.Type, driver.Id);

        // TODO: Notify admins of new document for verification
        // TODO: If all required documents uploaded, notify driver

        return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, document);
    }

    /// <summary>
    /// Get driver's documents
    /// </summary>
    [HttpGet("driver/documents")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Authorize(Roles = AuthRoles.Driver)]
    [ProducesResponseType(typeof(List<DriverDocument>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DriverDocument>>> GetMyDocuments()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var driver = await _repository.GetEntity<Driver>(d => d.UserId == userId);
        if (driver == null)
            return NotFound(new { message = "Driver profile not found" });

        var documents = await _repository.GetEntities<DriverDocument, DateTime>(
            d => d.DriverId == driver.Id,
            d => d.CreatedAt,
            isDescending: true
        );

        return Ok(documents);
    }

    /// <summary>
    /// Get specific document
    /// </summary>
    [HttpGet("driver/documents/{id}")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Authorize(Roles = AuthRoles.Driver)]
    [ProducesResponseType(typeof(DriverDocument), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DriverDocument>> GetDocument(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var driver = await _repository.GetEntity<Driver>(d => d.UserId == userId);
        if (driver == null)
            return NotFound(new { message = "Driver profile not found" });

        var document = await _repository.GetEntity<DriverDocument>(
            d => d.Id == id && d.DriverId == driver.Id
        );

        if (document == null)
            return NotFound(new { message = "Document not found" });

        return Ok(document);
    }

    #endregion

    #region Status Check

    /// <summary>
    /// Get registration status for a user
    /// </summary>
    [HttpGet("status/{userId}")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetRegistrationStatus(string userId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Users can only check their own status unless they're an admin
        if (currentUserId != userId && !User.IsInRole(AuthRoles.Admin) && !User.IsInRole(AuthRoles.SuperAdmin))
        {
            return Forbid();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var roles = await _userManager.GetRolesAsync(user);

        var response = new
        {
            UserId = user.Id,
            Email = user.Email,
            Roles = roles,
            EmailConfirmed = user.EmailConfirmed,
            Customer = (object?)null,
            Driver = (object?)null
        };

        if (roles.Contains(AuthRoles.Customer))
        {
            var customer = await _repository.GetEntity<Customer>(c => c.UserId == userId);
            response = response with { Customer = customer };
        }

        if (roles.Contains(AuthRoles.Driver))
        {
            var driver = await _context.Drivers
                .Include(d => d.Vehicles)
                .Include(d => d.Documents)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            response = response with { Driver = driver };
        }

        return Ok(response);
    }

    #endregion
}
