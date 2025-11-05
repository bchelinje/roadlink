namespace BeC.OpenId.Connect.Infrastructure.Authorization;

/// <summary>
/// Defines role names used in the application
/// </summary>
public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Driver = "Driver";
    public const string Customer = "Customer";

    public static readonly string[] All = 
    { 
        SuperAdmin, 
        Admin, 
        Driver, 
        Customer 
    };

    public static readonly string[] AdminRoles = 
    { 
        SuperAdmin, 
        Admin 
    };
}