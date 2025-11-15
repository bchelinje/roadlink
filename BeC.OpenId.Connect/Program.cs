using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.ActivityLogs.Services;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using BeC.OpenId.Connect.Features.Users.Dtos;
using BeC.OpenId.Connect.Infrastructure.Authorization;
using BeC.OpenId.Connect.Infrastructure.Email;
using BeC.OpenId.Connect.Infrastructure.Hosting;
using BeC.OpenId.Connect.Infrastructure.Maps;
using BeC.OpenId.Connect.Features.Pricing.Services;
using BeC.OpenId.Connect.Features.Pricing.Services.Interfaces;
using BeC.OpenId.Connect.Shared.Interfaces;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.OpenApi.Models;
using BeC.Common.Data.Repositories;
using BeC.Common.Data.Repositories.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddAutoMapper(typeof(Program).Assembly);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddHttpContextAccessor();

builder.Services.Scan(s => s.FromAssemblyOf<ITransient>()
    .AddClasses(c => c.AssignableTo<ITransient>())
    .AsImplementedInterfaces()
    .WithTransientLifetime());
builder.Services.Scan(s => s.FromAssemblyOf<ISingleton>()
    .AddClasses(c => c.AssignableTo<ISingleton>())
    .AsImplementedInterfaces()
    .WithSingletonLifetime());
builder.Services.Scan(s => s.FromAssemblyOf<IScoped>()
    .AddClasses(c => c.AssignableTo<IScoped>())
    .AsImplementedInterfaces()
    .WithScopedLifetime());

builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<IGoogleMapsService, GoogleMapsService>();
builder.Services.AddScoped<IPricingCalculatorService, PricingCalculatorService>();

// Register BeC.Common.Data Repository
builder.Services.AddScoped<IRepository, Repository>();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
});

// Configure Identity with roles
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;

        // Password settings
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;

        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // User settings
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>() // Add role support
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Configure cookie authentication to not redirect API requests
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        // For API requests, return 401 instead of redirecting to login page
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        // For API requests, return 403 instead of redirecting to access denied page
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = 403;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

// Add Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.RequireSuperAdminRole, 
        policy => policy.RequireRole(Roles.SuperAdmin));
    
    options.AddPolicy(Policies.RequireAdminRole, 
        policy => policy.RequireRole(Roles.Admin));
    
    options.AddPolicy(Policies.RequireDriverRole, 
        policy => policy.RequireRole(Roles.Driver));
    
    options.AddPolicy(Policies.RequireCustomerRole, 
        policy => policy.RequireRole(Roles.Customer));
    
    options.AddPolicy(Policies.RequireAdminOrSuperAdmin, 
        policy => policy.RequireRole(Roles.SuperAdmin, Roles.Admin));
    
    options.AddPolicy(Policies.RequireDriverOrAdmin, 
        policy => policy.RequireRole(Roles.Driver, Roles.Admin, Roles.SuperAdmin));
});


builder.Services.AddRazorPages();
builder.Services.AddControllers();

// Add hosted services
builder.Services.AddHostedService<OpenIddictSeedWorker>();
builder.Services.AddHostedService<RoleSeedWorker>();
builder.Services.AddEmailServices(builder.Configuration);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    options.UseOpenIddict();
});

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<ApplicationDbContext>();
    })
    .AddServer(options =>
    {
        // Configure endpoints
        options.SetAuthorizationEndpointUris("connect/authorize")
            .SetTokenEndpointUris("connect/token")
            .SetUserInfoEndpointUris("connect/userinfo");  // ← Fixed: Remove leading slash
        
        // Enable flows
        options.AllowAuthorizationCodeFlow()
            .AllowClientCredentialsFlow()
            .AllowRefreshTokenFlow()
            .AllowPasswordFlow(); // ⚠️ For testing only - remove in production!
        
        // Register scopes
        options.RegisterScopes("openid", "profile", "email", "roles", "phone", "offline_access");

        // ✅ ADD: Disable token encryption for development
        options.DisableAccessTokenEncryption();

        options.AddDevelopmentEncryptionCertificate()
            .AddDevelopmentSigningCertificate();

        // ✅ ADD: Enable userinfo endpoint passthrough
        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableTokenEndpointPassthrough()
            .EnableUserInfoEndpointPassthrough();  // ← ADD THIS!
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BeC API",
        Version = "v1",
        Description = "Authentication and Authorization API using OpenIddict",
        Contact = new OpenApiContact
        {
            Name = "BeC Support",
            Email = "bchelinje@gmail.com"
        }
    });

    // Add OAuth2 security definition for Bearer tokens
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            ClientCredentials = new OpenApiOAuthFlow
            {
                TokenUrl = new Uri("/connect/token", UriKind.Relative),
                Scopes = new Dictionary<string, string>
                {
                    { "profile", "Access to user profile information" }
                }
            }
        },
        Description = "OAuth2 Client Credentials Flow"
    });

    // Add Bearer token authentication
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Use XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// ✅ CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",    // Angular HTTP
                "https://localhost:4200"    // Angular HTTPS
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    
    // Enable Swagger in development
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "BeC Identity Server API v1");
        options.RoutePrefix = "swagger"; // Access at: /swagger
        options.DocumentTitle = "BeC Identity Server API";
        options.DisplayRequestDuration();
        
        // OAuth2 configuration for Swagger UI
        options.OAuthClientId("swagger-ui");
        options.OAuthClientSecret("swagger-secret");
        options.OAuthAppName("Swagger UI");
    });
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    // ✅ Only redirect to HTTPS in production
    app.UseHttpsRedirection();
}

// ✅ CORS must come early in the pipeline
app.UseCors("AllowAngularApp");

app.UseRouting();
app.UseStaticFiles();

app.UseAuthentication(); 
app.UseAuthorization(); 

app.MapStaticAssets();
app.MapControllers();
app.MapRazorPages()
    .WithStaticAssets();
app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});

app.Run();