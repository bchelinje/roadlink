namespace BeC.OpenId.Connect.Infrastructure.Authorization;

/// <summary>
/// Defines authorization policy names used throughout the application
/// </summary>
public static class Policies
{
    public const string RequireSuperAdminRole = "RequireSuperAdminRole";
    public const string RequireAdminRole = "RequireAdminRole";
    public const string RequireDriverRole = "RequireDriverRole";
    public const string RequireCustomerRole = "RequireCustomerRole";
    
    // Composite policies
    public const string RequireAdminOrSuperAdmin = "RequireAdminOrSuperAdmin";
    public const string RequireDriverOrAdmin = "RequireDriverOrAdmin";
}

