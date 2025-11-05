using BeC.OpenId.Connect.Data;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace BeC.OpenId.Connect.Infrastructure.Hosting;

public sealed class OpenIddictSeedWorker(IServiceProvider sp) : IHostedService
{
    private readonly IServiceProvider _sp = sp;
    
    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync(ct);

        await SeedScopesAsync(scope.ServiceProvider, ct);
        await SeedApplicationsAsync(scope.ServiceProvider, ct);
    }

    private async Task SeedScopesAsync(IServiceProvider serviceProvider, CancellationToken ct)
    {
        var scopeManager = serviceProvider.GetRequiredService<IOpenIddictScopeManager>();
        
        var scopes = new[]
        {
            new OpenIddictScopeDescriptor
            {
                Name = Scopes.Profile,
                DisplayName = "Profile scope",
                Description = "Access to user profile information",
                Resources = { "resource_server" }
            },
            new OpenIddictScopeDescriptor
            {
                Name = Scopes.Email,
                DisplayName = "Email scope", 
                Description = "Access to user email address",
                Resources = { "resource_server" }
            },
            new OpenIddictScopeDescriptor
            {
                Name = Scopes.Phone,
                DisplayName = "Phone scope",
                Description = "Access to user phone number", 
                Resources = { "resource_server" }
            },
            new OpenIddictScopeDescriptor
            {
                Name = Scopes.Roles,
                DisplayName = "Roles scope",
                Description = "Access to user roles",
                Resources = { "resource_server" }
            }
        };

        foreach (var scopeDescriptor in scopes)
        {
            if (await scopeManager.FindByNameAsync(scopeDescriptor.Name!, ct) is null)
            {
                await scopeManager.CreateAsync(scopeDescriptor, ct);
            }
        }
    }

    private async Task SeedApplicationsAsync(IServiceProvider serviceProvider, CancellationToken ct)
    {
        var appManager = serviceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        // Service Worker Client (for backend services)
        await CreateOrUpdateClientAsync(appManager, new OpenIddictApplicationDescriptor
        {
            ClientId = "service-worker",
            ClientSecret = "388D45FA-B36B-4988-BA59-B187D329C207",
            DisplayName = "Service Worker",
            Permissions =
            {
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.ClientCredentials,
                Permissions.Prefixes.Scope + Scopes.Profile,
                Permissions.Prefixes.Scope + Scopes.Email,
                Permissions.Prefixes.Scope + Scopes.Roles
            }
        }, ct);

        // Swagger UI Client (for testing)
        await CreateOrUpdateClientAsync(appManager, new OpenIddictApplicationDescriptor
        {
            ClientId = "swagger-ui",
            ClientSecret = "swagger-secret",
            DisplayName = "Swagger UI",
            RedirectUris =
            {
                new Uri("https://localhost:7172/swagger/oauth2-redirect.html"),
                new Uri("https://localhost:5001/swagger/oauth2-redirect.html")
            },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.ClientCredentials,
                Permissions.ResponseTypes.Code,
                Permissions.Prefixes.Scope + Scopes.Profile,
                Permissions.Prefixes.Scope + Scopes.Email,
                Permissions.Prefixes.Scope + Scopes.Roles
            }
        }, ct);

        // Mobile App Client (iOS/Android) - Uses Authorization Code + PKCE
        await CreateOrUpdateClientAsync(appManager, new OpenIddictApplicationDescriptor
        {
            ClientId = "mobile-app",
            DisplayName = "BeC Van Moving Mobile App",
            ClientType = ClientTypes.Public, // Important: Public client for mobile apps
            RedirectUris =
            {
                new Uri("com.bec.vanmoving://callback"),  // Custom URL scheme for mobile
                new Uri("https://localhost:7172/signin-oidc"), // For testing
                new Uri("http://localhost:3000/callback") // For local testing
            },
            PostLogoutRedirectUris =
            {
                new Uri("com.bec.vanmoving://signout-callback"),
                new Uri("https://localhost:7172/signout-oidc")
            },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.EndSession,
                Permissions.Endpoints.Introspection, // For token validation
                Permissions.Endpoints.Revocation,    // For token revocation
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.GrantTypes.Password,     // ⚠️ For testing only!
                Permissions.ResponseTypes.Code,
                Permissions.Prefixes.Scope + Scopes.OpenId,    // Required for OIDC
                Permissions.Prefixes.Scope + Scopes.Email,
                Permissions.Prefixes.Scope + Scopes.Profile,
                Permissions.Prefixes.Scope + Scopes.Roles,
                Permissions.Prefixes.Scope + Scopes.Phone,
                Permissions.Prefixes.Scope + Scopes.OfflineAccess // For refresh tokens
            },
            Requirements =
            {
                Requirements.Features.ProofKeyForCodeExchange // PKCE required for security
            }
        }, ct);

        // Web App Client (for web applications)
        await CreateOrUpdateClientAsync(appManager, new OpenIddictApplicationDescriptor
        {
            ClientId = "web-app",
            ClientSecret = "web-app-secret-key-2024!",
            DisplayName = "BeC Van Moving Web App",
            ClientType = ClientTypes.Confidential, // Confidential client for web apps
            RedirectUris =
            {
                new Uri("https://localhost:7172/signin-oidc"),
                new Uri("https://yourdomain.com/signin-oidc")
            },
            PostLogoutRedirectUris =
            {
                new Uri("https://localhost:7172/signout-oidc"),
                new Uri("https://yourdomain.com/signout-oidc")
            },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.EndSession,
                Permissions.Endpoints.Introspection,
                Permissions.Endpoints.Revocation,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                Permissions.Prefixes.Scope + Scopes.OpenId,
                Permissions.Prefixes.Scope + Scopes.Email,
                Permissions.Prefixes.Scope + Scopes.Profile,
                Permissions.Prefixes.Scope + Scopes.Roles,
                Permissions.Prefixes.Scope + Scopes.Phone,
                Permissions.Prefixes.Scope + Scopes.OfflineAccess
            }
        }, ct);

        // ✅ Angular Admin Dashboard Client (NEW!)
        await CreateOrUpdateClientAsync(appManager, new OpenIddictApplicationDescriptor
        {
            ClientId = "angular-admin-app",
            DisplayName = "Angular Admin Dashboard",
            ClientType = ClientTypes.Public, // Public client (no secret for SPAs)
            RedirectUris =
            {
                new Uri("http://localhost:4200/auth/callback"),
                new Uri("https://localhost:4200/auth/callback")
            },
            PostLogoutRedirectUris =
            {
                new Uri("http://localhost:4200"),
                new Uri("https://localhost:4200")
            },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.EndSession,
                Permissions.Endpoints.Introspection,
                Permissions.Endpoints.Revocation,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.GrantTypes.Password,     // ⚠️ For testing only - remove in production!
                Permissions.ResponseTypes.Code,
                Permissions.Prefixes.Scope + Scopes.OpenId,
                Permissions.Prefixes.Scope + Scopes.Email,
                Permissions.Prefixes.Scope + Scopes.Profile,
                Permissions.Prefixes.Scope + Scopes.Roles,
                Permissions.Prefixes.Scope + Scopes.Phone,
                Permissions.Prefixes.Scope + Scopes.OfflineAccess // For refresh tokens
            },
            Requirements =
            {
                Requirements.Features.ProofKeyForCodeExchange // PKCE for security
            }
        }, ct);
    }

    private async Task CreateOrUpdateClientAsync(
        IOpenIddictApplicationManager appManager, 
        OpenIddictApplicationDescriptor descriptor, 
        CancellationToken ct)
    {
        var existingApp = await appManager.FindByClientIdAsync(descriptor.ClientId!, ct);
        if (existingApp is not null)
        {
            await appManager.DeleteAsync(existingApp, ct);
        }

        await appManager.CreateAsync(descriptor, ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}