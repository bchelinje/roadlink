using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Collections.Immutable;
using System.Security.Claims;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using BeC.OpenId.Connect.Features.Users.Dtos;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace BeC.OpenId.Connect.Features.Authorization.Controllers;

[ApiExplorerSettings(IgnoreApi = true)] // Hide from Swagger - these are OAuth flows, not REST APIs
public class AuthorizationController : ControllerBase
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogService _activityLogService;

    public AuthorizationController(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictScopeManager scopeManager,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IActivityLogService activityLogService)
    {
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
        _scopeManager = scopeManager;
        _signInManager = signInManager;
        _userManager = userManager;
        _activityLogService = activityLogService;
    }

    #region Authorization Code Flow

    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
                     throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // Retrieve the user principal stored in the authentication cookie.
        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);

        // If the user is not authenticated, redirect them to the login page.
        if (!result.Succeeded)
        {
            // 🎯 LOG: Authorization attempt without authentication
            await _activityLogService.LogActivityAsync(
                action: "AUTHORIZATION_ATTEMPTED_UNAUTHENTICATED",
                entityType: "AUTHORIZATION",
                entityId: request.ClientId,
                entityName: request.ClientId,
                description: $"Authorization attempt for client '{request.ClientId}' without authentication",
                severity: "INFO",
                metadata: new Dictionary<string, object>
                {
                    ["ClientId"] = request.ClientId ?? "unknown",
                    ["Scopes"] = string.Join(", ", request.GetScopes()),
                    ["ResponseType"] = request.ResponseType ?? "unknown"
                }
            );

            return Challenge(
                authenticationSchemes: IdentityConstants.ApplicationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = Request.PathBase + Request.Path + QueryString.Create(
                        Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList())
                });
        }

        // Retrieve the profile of the logged in user.
        var user = await _userManager.GetUserAsync(result.Principal) ??
                  throw new InvalidOperationException("The user details cannot be retrieved.");

        // Retrieve the application details from the database.
        var application = await _applicationManager.FindByClientIdAsync(request.ClientId!) ??
                         throw new InvalidOperationException("Details concerning the calling client application cannot be found.");

        // Retrieve the permanent authorizations associated with the user and the calling client application.
        var authorizations = new List<object>();
        await foreach (var auth in _authorizationManager.FindAsync(
            subject: await _userManager.GetUserIdAsync(user),
            client: (await _applicationManager.GetIdAsync(application))!,
            status: Statuses.Valid,
            type: AuthorizationTypes.Permanent,
            scopes: request.GetScopes().ToImmutableArray()))
        {
            authorizations.Add(auth);
        }

        var clientDisplayName = await _applicationManager.GetDisplayNameAsync(application);

        switch (await _applicationManager.GetConsentTypeAsync(application))
        {
            // If the consent is external (e.g. when authorizations are granted by a sysadmin),
            // immediately return an error if no authorization can be found in the database.
            case ConsentTypes.External when !authorizations.Any():
                // 🎯 LOG: Authorization denied - external consent required
                await _activityLogService.LogActivityAsync(
                    action: "AUTHORIZATION_DENIED",
                    entityType: "AUTHORIZATION",
                    entityId: request.ClientId,
                    entityName: clientDisplayName,
                    description: $"User '{user.Email}' denied access to client '{clientDisplayName}' - external consent required",
                    severity: "WARNING",
                    userId: user.Id,
                    userName: user.UserName ?? "Unknown",
                    userEmail: user.Email ?? "unknown@example.com",
                    metadata: new Dictionary<string, object>
                    {
                        ["ClientId"] = request.ClientId ?? "unknown",
                        ["ConsentType"] = ConsentTypes.External,
                        ["Reason"] = "No authorization found"
                    }
                );

                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.ConsentRequired,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The logged in user is not allowed to access this client application."
                    }));

            // If the consent is implicit or if an authorization was found,
            // return an authorization response without displaying the consent form.
            case ConsentTypes.Implicit:
            case ConsentTypes.External when authorizations.Any():
            case ConsentTypes.Explicit when authorizations.Any() && !HasPrompt(request, "consent"):
                var userPrincipal = await CreateUserPrincipalAsync(user, request.GetScopes());

                // Automatically create a permanent authorization to avoid requiring explicit consent
                // for future authorization or token requests.
                var userAuthorization = authorizations.LastOrDefault();
                var isNewAuthorization = false;
                
                if (userAuthorization == null)
                {
                    isNewAuthorization = true;
                    userAuthorization = await _authorizationManager.CreateAsync(
                        principal: userPrincipal,
                        subject: await _userManager.GetUserIdAsync(user),
                        client: (await _applicationManager.GetIdAsync(application))!,
                        type: AuthorizationTypes.Permanent,
                        scopes: userPrincipal.GetScopes());
                }

                userPrincipal.SetAuthorizationId(await _authorizationManager.GetIdAsync(userAuthorization));

                foreach (var claim in userPrincipal.Claims)
                {
                    claim.SetDestinations(GetDestinations(claim, userPrincipal));
                }

                // 🎯 LOG: Successful authorization
                await _activityLogService.LogActivityAsync(
                    action: isNewAuthorization ? "AUTHORIZATION_GRANTED" : "AUTHORIZATION_SUCCESS",
                    entityType: "AUTHORIZATION",
                    entityId: request.ClientId,
                    entityName: clientDisplayName,
                    description: isNewAuthorization 
                        ? $"User '{user.Email}' granted authorization to client '{clientDisplayName}'"
                        : $"User '{user.Email}' authorized with client '{clientDisplayName}' (existing authorization)",
                    severity: "INFO",
                    userId: user.Id,
                    userName: user.UserName ?? "Unknown",
                    userEmail: user.Email ?? "unknown@example.com",
                    metadata: new Dictionary<string, object>
                    {
                        ["ClientId"] = request.ClientId ?? "unknown",
                        ["Scopes"] = string.Join(", ", request.GetScopes()),
                        ["ConsentType"] = await _applicationManager.GetConsentTypeAsync(application) ?? "unknown",
                        ["IsNewAuthorization"] = isNewAuthorization
                    }
                );

                return SignIn(userPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            // At this point, no authorization was found in the database and an error must be returned
            // if the client application specified prompt=none in the authorization request.
            case ConsentTypes.Explicit when HasPrompt(request, "none"):
            case ConsentTypes.Systematic when HasPrompt(request, "none"):
                // 🎯 LOG: Authorization denied - interactive consent required
                await _activityLogService.LogActivityAsync(
                    action: "AUTHORIZATION_DENIED",
                    entityType: "AUTHORIZATION",
                    entityId: request.ClientId,
                    entityName: clientDisplayName,
                    description: $"User '{user.Email}' authorization denied for client '{clientDisplayName}' - interactive consent required",
                    severity: "WARNING",
                    userId: user.Id,
                    userName: user.UserName ?? "Unknown",
                    userEmail: user.Email ?? "unknown@example.com",
                    metadata: new Dictionary<string, object>
                    {
                        ["ClientId"] = request.ClientId ?? "unknown",
                        ["Reason"] = "Interactive consent required, but prompt=none specified"
                    }
                );

                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.ConsentRequired,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "Interactive user consent is required."
                    }));

            // In every other case, render the consent form.
            default:
                // TODO: Implement a proper consent view
                // For now, automatically approve (development only!)
                var defaultPrincipal = await CreateUserPrincipalAsync(user, request.GetScopes());
                
                var defaultAuthorization = await _authorizationManager.CreateAsync(
                    principal: defaultPrincipal,
                    subject: await _userManager.GetUserIdAsync(user),
                    client: (await _applicationManager.GetIdAsync(application))!,
                    type: AuthorizationTypes.Permanent,
                    scopes: defaultPrincipal.GetScopes());

                defaultPrincipal.SetAuthorizationId(await _authorizationManager.GetIdAsync(defaultAuthorization));

                foreach (var claim in defaultPrincipal.Claims)
                {
                    claim.SetDestinations(GetDestinations(claim, defaultPrincipal));
                }

                // 🎯 LOG: Automatic consent granted (development mode)
                await _activityLogService.LogActivityAsync(
                    action: "AUTHORIZATION_GRANTED",
                    entityType: "AUTHORIZATION",
                    entityId: request.ClientId,
                    entityName: clientDisplayName,
                    description: $"User '{user.Email}' automatically granted authorization to client '{clientDisplayName}' (development mode)",
                    severity: "INFO",
                    userId: user.Id,
                    userName: user.UserName ?? "Unknown",
                    userEmail: user.Email ?? "unknown@example.com",
                    metadata: new Dictionary<string, object>
                    {
                        ["ClientId"] = request.ClientId ?? "unknown",
                        ["Scopes"] = string.Join(", ", request.GetScopes()),
                        ["Note"] = "Automatic consent - development mode"
                    }
                );

                return SignIn(defaultPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
    }

    #endregion

    #region Token Endpoint

    [HttpPost("~/connect/token")]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
                     throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsClientCredentialsGrantType())
        {
            return await HandleClientCredentialsFlow(request);
        }

        if (request.IsAuthorizationCodeGrantType())
        {
            return await HandleAuthorizationCodeFlow(request);
        }

        if (request.IsRefreshTokenGrantType())
        {
            return await HandleRefreshTokenFlow(request);
        }

        if (request.IsPasswordGrantType())
        {
            return await HandlePasswordFlow(request);
        }

        // 🎯 LOG: Unsupported grant type
        await _activityLogService.LogActivityAsync(
            action: "TOKEN_REQUEST_FAILED",
            entityType: "AUTHORIZATION",
            entityId: null,
            entityName: request.GrantType,
            description: $"Unsupported grant type: {request.GrantType}",
            severity: "ERROR",
            metadata: new Dictionary<string, object>
            {
                ["GrantType"] = request.GrantType ?? "unknown",
                ["ClientId"] = request.ClientId ?? "unknown"
            }
        );

        throw new InvalidOperationException("The specified grant type is not supported.");
    }

    private async Task<IActionResult> HandleClientCredentialsFlow(OpenIddictRequest request)
    {
        // Note: the client credentials are automatically validated by OpenIddict
        var application = await _applicationManager.FindByClientIdAsync(request.ClientId!) ??
                         throw new InvalidOperationException("The application details cannot be found in the database.");

        // Create a new ClaimsIdentity containing the claims that will be used to create an id_token, a token or a code.
        var identity = new ClaimsIdentity(
            TokenValidationParameters.DefaultAuthenticationType,
            Claims.Name,
            Claims.Role);

        // Use the client_id as the subject identifier.
        identity.SetClaim(Claims.Subject, await _applicationManager.GetClientIdAsync(application));
        identity.SetClaim(Claims.Name, await _applicationManager.GetDisplayNameAsync(application));

        identity.SetDestinations(static claim => claim.Type switch
        {
            Claims.Name or Claims.Subject => [Destinations.AccessToken, Destinations.IdentityToken],
            _ => [Destinations.AccessToken]
        });

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes().ToImmutableArray());

        var clientDisplayName = await _applicationManager.GetDisplayNameAsync(application);

        // 🎯 LOG: Client credentials token issued
        await _activityLogService.LogActivityAsync(
            action: "TOKEN_ISSUED",
            entityType: "AUTHORIZATION",
            entityId: request.ClientId,
            entityName: clientDisplayName,
            description: $"Access token issued for client '{clientDisplayName}' using client credentials flow",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                ["GrantType"] = "client_credentials",
                ["ClientId"] = request.ClientId ?? "unknown",
                ["Scopes"] = string.Join(", ", request.GetScopes())
            }
        );

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult> HandleAuthorizationCodeFlow(OpenIddictRequest request)
    {
        // Retrieve the claims principal stored in the authorization code
        var principal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;

        if (principal == null)
        {
            // 🎯 LOG: Invalid authorization code
            await _activityLogService.LogActivityAsync(
                action: "TOKEN_REQUEST_FAILED",
                entityType: "AUTHORIZATION",
                entityId: request.ClientId,
                entityName: request.ClientId,
                description: $"Token request failed for client '{request.ClientId}' - invalid authorization code",
                severity: "WARNING",
                metadata: new Dictionary<string, object>
                {
                    ["GrantType"] = "authorization_code",
                    ["ClientId"] = request.ClientId ?? "unknown",
                    ["Reason"] = "Invalid authorization code"
                }
            );

            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The token is no longer valid."
                }));
        }

        // Retrieve the user profile corresponding to the authorization code
        var user = await _userManager.GetUserAsync(principal);
        if (user == null)
        {
            // 🎯 LOG: User not found for authorization code
            await _activityLogService.LogActivityAsync(
                action: "TOKEN_REQUEST_FAILED",
                entityType: "AUTHORIZATION",
                entityId: request.ClientId,
                entityName: request.ClientId,
                description: $"Token request failed for client '{request.ClientId}' - user not found",
                severity: "ERROR",
                metadata: new Dictionary<string, object>
                {
                    ["GrantType"] = "authorization_code",
                    ["ClientId"] = request.ClientId ?? "unknown",
                    ["Reason"] = "User not found"
                }
            );

            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The token is no longer valid."
                }));
        }

        // Ensure the user is still allowed to sign in
        if (!await _signInManager.CanSignInAsync(user))
        {
            // 🎯 LOG: User not allowed to sign in
            await _activityLogService.LogActivityAsync(
                action: "TOKEN_REQUEST_FAILED",
                entityType: "AUTHORIZATION",
                entityId: request.ClientId,
                entityName: request.ClientId,
                description: $"Token request failed for user '{user.Email}' - user not allowed to sign in",
                severity: "WARNING",
                userId: user.Id,
                userName: user.UserName ?? "Unknown",
                userEmail: user.Email ?? "unknown@example.com",
                metadata: new Dictionary<string, object>
                {
                    ["GrantType"] = "authorization_code",
                    ["ClientId"] = request.ClientId ?? "unknown",
                    ["Reason"] = "User not allowed to sign in"
                }
            );

            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is no longer allowed to sign in."
                }));
        }

        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, principal));
        }

        // 🎯 LOG: Token issued via authorization code
        await _activityLogService.LogActivityAsync(
            action: "TOKEN_ISSUED",
            entityType: "AUTHORIZATION",
            entityId: request.ClientId,
            entityName: request.ClientId,
            description: $"Access token issued for user '{user.Email}' using authorization code flow",
            severity: "INFO",
            userId: user.Id,
            userName: user.UserName ?? "Unknown",
            userEmail: user.Email ?? "unknown@example.com",
            metadata: new Dictionary<string, object>
            {
                ["GrantType"] = "authorization_code",
                ["ClientId"] = request.ClientId ?? "unknown"
            }
        );

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult> HandleRefreshTokenFlow(OpenIddictRequest request)
    {
        // Retrieve the claims principal stored in the refresh token
        var principal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;

        if (principal == null)
        {
            // 🎯 LOG: Invalid refresh token
            await _activityLogService.LogActivityAsync(
                action: "TOKEN_REFRESH_FAILED",
                entityType: "AUTHORIZATION",
                entityId: request.ClientId,
                entityName: request.ClientId,
                description: $"Token refresh failed for client '{request.ClientId}' - invalid refresh token",
                severity: "WARNING",
                metadata: new Dictionary<string, object>
                {
                    ["GrantType"] = "refresh_token",
                    ["ClientId"] = request.ClientId ?? "unknown",
                    ["Reason"] = "Invalid refresh token"
                }
            );

            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The refresh token is no longer valid."
                }));
        }

        // Retrieve the user profile corresponding to the refresh token
        var user = await _userManager.GetUserAsync(principal);
        if (user == null)
        {
            // 🎯 LOG: User not found for refresh token
            await _activityLogService.LogActivityAsync(
                action: "TOKEN_REFRESH_FAILED",
                entityType: "AUTHORIZATION",
                entityId: request.ClientId,
                entityName: request.ClientId,
                description: $"Token refresh failed for client '{request.ClientId}' - user not found",
                severity: "ERROR",
                metadata: new Dictionary<string, object>
                {
                    ["GrantType"] = "refresh_token",
                    ["ClientId"] = request.ClientId ?? "unknown",
                    ["Reason"] = "User not found"
                }
            );

            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The refresh token is no longer valid."
                }));
        }

        // Ensure the user is still allowed to sign in
        if (!await _signInManager.CanSignInAsync(user))
        {
            // 🎯 LOG: User not allowed to sign in
            await _activityLogService.LogActivityAsync(
                action: "TOKEN_REFRESH_FAILED",
                entityType: "AUTHORIZATION",
                entityId: request.ClientId,
                entityName: request.ClientId,
                description: $"Token refresh failed for user '{user.Email}' - user not allowed to sign in",
                severity: "WARNING",
                userId: user.Id,
                userName: user.UserName ?? "Unknown",
                userEmail: user.Email ?? "unknown@example.com",
                metadata: new Dictionary<string, object>
                {
                    ["GrantType"] = "refresh_token",
                    ["ClientId"] = request.ClientId ?? "unknown",
                    ["Reason"] = "User not allowed to sign in"
                }
            );

            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is no longer allowed to sign in."
                }));
        }

        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, principal));
        }

        // 🎯 LOG: Token refreshed successfully
        await _activityLogService.LogActivityAsync(
            action: "TOKEN_REFRESHED",
            entityType: "AUTHORIZATION",
            entityId: request.ClientId,
            entityName: request.ClientId,
            description: $"Access token refreshed for user '{user.Email}'",
            severity: "INFO",
            userId: user.Id,
            userName: user.UserName ?? "Unknown",
            userEmail: user.Email ?? "unknown@example.com",
            metadata: new Dictionary<string, object>
            {
                ["GrantType"] = "refresh_token",
                ["ClientId"] = request.ClientId ?? "unknown"
            }
        );

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult> HandlePasswordFlow(OpenIddictRequest request)
    {
        // WARNING: The password flow is not recommended for production use.
        // It's included here for testing purposes only.
        // Mobile apps should use Authorization Code Flow with PKCE instead.

        var user = await _userManager.FindByNameAsync(request.Username!);
        if (user == null)
        {
            user = await _userManager.FindByEmailAsync(request.Username!);
        }

        if (user == null)
        {
            // 🎯 LOG: Failed login - user not found (Password flow)
            await _activityLogService.LogActivityAsync(
                action: "LOGIN_FAILED",
                entityType: "AUTHORIZATION",
                entityId: request.ClientId,
                entityName: request.ClientId,
                description: $"Failed login attempt using password flow for username '{request.Username}' - user not found",
                severity: "WARNING",
                metadata: new Dictionary<string, object>
                {
                    ["GrantType"] = "password",
                    ["ClientId"] = request.ClientId ?? "unknown",
                    ["Username"] = request.Username ?? "unknown",
                    ["Reason"] = "User not found"
                }
            );

            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The username/password couple is invalid."
                }));
        }

        // Validate the username/password credentials
        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password!, lockoutOnFailure: true);
        
        if (!result.Succeeded)
        {
            var reason = result.IsLockedOut ? "Account locked out" : 
                        result.IsNotAllowed ? "Not allowed to sign in" :
                        result.RequiresTwoFactor ? "Two-factor required" : 
                        "Invalid credentials";

            // 🎯 LOG: Failed login (Password flow)
            await _activityLogService.LogActivityAsync(
                action: "LOGIN_FAILED",
                entityType: "AUTHORIZATION",
                entityId: request.ClientId,
                entityName: request.ClientId,
                description: $"Failed login attempt for user '{user.Email}' using password flow - {reason}",
                severity: result.IsLockedOut ? "ERROR" : "WARNING",
                userId: user.Id,
                userName: user.UserName ?? "Unknown",
                userEmail: user.Email ?? "unknown@example.com",
                metadata: new Dictionary<string, object>
                {
                    ["GrantType"] = "password",
                    ["ClientId"] = request.ClientId ?? "unknown",
                    ["Reason"] = reason,
                    ["IsLockedOut"] = result.IsLockedOut,
                    ["IsNotAllowed"] = result.IsNotAllowed,
                    ["RequiresTwoFactor"] = result.RequiresTwoFactor
                }
            );

            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = result.IsLockedOut 
                        ? "The user account has been locked out." 
                        : "The username/password couple is invalid."
                }));
        }

        // Create the claims principal
        var passwordPrincipal = await CreateUserPrincipalAsync(user, request.GetScopes());

        // Set claim destinations
        foreach (var claim in passwordPrincipal.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, passwordPrincipal));
        }

        // 🎯 LOG: Successful login via password flow
        await _activityLogService.LogActivityAsync(
            action: "LOGIN_SUCCESS",
            entityType: "AUTHORIZATION",
            entityId: request.ClientId,
            entityName: request.ClientId,
            description: $"User '{user.Email}' logged in successfully using password flow (client: '{request.ClientId}')",
            severity: "INFO",
            userId: user.Id,
            userName: user.UserName ?? "Unknown",
            userEmail: user.Email ?? "unknown@example.com",
            metadata: new Dictionary<string, object>
            {
                ["GrantType"] = "password",
                ["ClientId"] = request.ClientId ?? "unknown",
                ["Scopes"] = string.Join(", ", request.GetScopes()),
                ["Note"] = "Password flow - not recommended for production"
            }
        );

        return SignIn(passwordPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    #endregion

    #region Logout

    [HttpGet("~/connect/logout")]
    public async Task<IActionResult> Logout()
    {
        // Get user info before signing out
        var user = await _userManager.GetUserAsync(User);
        
        if (user != null)
        {
            // 🎯 LOG: User logout
            await _activityLogService.LogActivityAsync(
                action: "LOGOUT",
                entityType: "AUTHORIZATION",
                entityId: user.Id,
                entityName: user.UserName,
                description: $"User '{user.Email}' logged out",
                severity: "INFO",
                userId: user.Id,
                userName: user.UserName ?? "Unknown",
                userEmail: user.Email ?? "unknown@example.com"
            );
        }

        // Ask OpenIddict to sign out the user
        await _signInManager.SignOutAsync();

        return SignOut(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties
            {
                RedirectUri = "/"
            });
    }

    #endregion

    #region Helpers

    private async Task<ClaimsPrincipal> CreateUserPrincipalAsync(ApplicationUser user, IEnumerable<string> scopes)
    {
        var principal = await _signInManager.CreateUserPrincipalAsync(user);
        var identity = (ClaimsIdentity)principal.Identity!;

        // Add custom claims based on scopes
        if (scopes.Contains(Scopes.Profile))
        {
            identity.SetClaim(Claims.Subject, await _userManager.GetUserIdAsync(user))
                   .SetClaim(Claims.Email, await _userManager.GetEmailAsync(user))
                   .SetClaim(Claims.Name, await _userManager.GetUserNameAsync(user))
                   .SetClaim(Claims.PreferredUsername, await _userManager.GetUserNameAsync(user));
        }

        if (scopes.Contains(Scopes.Email))
        {
            identity.SetClaim(Claims.Email, await _userManager.GetEmailAsync(user))
                   .SetClaim(Claims.EmailVerified, (await _userManager.IsEmailConfirmedAsync(user)).ToString().ToLower());
        }

        if (scopes.Contains(Scopes.Phone))
        {
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (!string.IsNullOrEmpty(phoneNumber))
            {
                identity.SetClaim(Claims.PhoneNumber, phoneNumber)
                       .SetClaim(Claims.PhoneNumberVerified, (await _userManager.IsPhoneNumberConfirmedAsync(user)).ToString().ToLower());
            }
        }

        if (scopes.Contains(Scopes.Roles))
        {
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                identity.AddClaim(new Claim(Claims.Role, role));
            }
        }

        principal.SetScopes(scopes.ToImmutableArray());
        
        var resources = new List<string>();
        await foreach (var resource in _scopeManager.ListResourcesAsync(scopes.ToImmutableArray()))
        {
            resources.Add(resource);
        }
        principal.SetResources(resources.ToImmutableArray());

        return principal;
    }

    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
    {
        // Note: by default, claims are NOT automatically included in the access and identity tokens.
        // To allow OpenIddict to serialize them, you must attach them a destination, that specifies
        // whether they should be included in access tokens, in identity tokens or in both.

        switch (claim.Type)
        {
            case Claims.Name or Claims.PreferredUsername:
                yield return Destinations.AccessToken;

                if (principal.HasScope(Scopes.Profile))
                    yield return Destinations.IdentityToken;

                yield break;

            case Claims.Email:
                yield return Destinations.AccessToken;

                if (principal.HasScope(Scopes.Email))
                    yield return Destinations.IdentityToken;

                yield break;

            case Claims.Role:
                yield return Destinations.AccessToken;

                if (principal.HasScope(Scopes.Roles))
                    yield return Destinations.IdentityToken;

                yield break;

            // Never include the security stamp in the access and identity tokens, as it's a secret value.
            case "AspNet.Identity.SecurityStamp":
                yield break;

            default:
                yield return Destinations.AccessToken;
                yield break;
        }
    }

    private static bool HasPrompt(OpenIddictRequest request, string prompt)
    {
        var prompts = request.Prompt?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        return prompts.Contains(prompt, StringComparer.OrdinalIgnoreCase);
    }

    #endregion
}