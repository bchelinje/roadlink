using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using BeC.OpenId.Connect.Features.Users.Dtos;

namespace BeC.OpenId.Connect.Infrastructure.Hosting;

/// <summary>
/// Seeds default roles and admin user on application startup
/// </summary>
public sealed class RoleSeedWorker : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public RoleSeedWorker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await SeedRolesAsync(roleManager);
        await SeedAdminUserAsync(userManager);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        var roles = new[]
        {
            new { Name = "SuperAdmin", Description = "Full system access with all permissions" },
            new { Name = "Admin", Description = "Administrative access to manage users and content" },
            new { Name = "Driver", Description = "Van driver with access to job management" },
            new { Name = "Customer", Description = "Customer with access to booking and tracking" }
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role.Name))
            {
                var identityRole = new IdentityRole(role.Name)
                {
                    NormalizedName = role.Name.ToUpper()
                };

                var result = await roleManager.CreateAsync(identityRole);

                if (result.Succeeded)
                {
                    // Add role claims for additional metadata
                    await roleManager.AddClaimAsync(identityRole, 
                        new Claim("description", role.Description));
                    
                    Console.WriteLine($"✓ Role '{role.Name}' created successfully");
                }
                else
                {
                    Console.WriteLine($"✗ Failed to create role '{role.Name}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
    {
        const string adminEmail = "admin@becvanmoving.com";
        const string adminPassword = "Admin@123!"; // Change this in production!

        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true, // Auto-confirm admin email
                PhoneNumber = "+447700900000",
                PhoneNumberConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);

            if (result.Succeeded)
            {
                // Assign SuperAdmin role
                await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
                
                // Add custom claims for the admin user
                await userManager.AddClaimsAsync(adminUser, new[]
                {
                    new Claim("full_name", "System Administrator"),
                    new Claim("department", "IT"),
                    new Claim("employee_id", "EMP001")
                });

                Console.WriteLine($"✓ Admin user created: {adminEmail}");
                Console.WriteLine($"  Password: {adminPassword}");
                Console.WriteLine("  ⚠️  IMPORTANT: Change this password in production!");
            }
            else
            {
                Console.WriteLine($"✗ Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            // Ensure admin has SuperAdmin role
            if (!await userManager.IsInRoleAsync(adminUser, "SuperAdmin"))
            {
                await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
                Console.WriteLine($"✓ Added SuperAdmin role to existing user: {adminEmail}");
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}