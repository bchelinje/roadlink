using Hangfire.Dashboard;

namespace BeC.OpenId.Connect.Infrastructure.BackgroundJobs;

/// <summary>
/// Authorization filter for Hangfire dashboard
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Allow in development
        if (httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            return true;
        }

        // In production, require authentication and admin role
        return httpContext.User.Identity?.IsAuthenticated == true &&
               (httpContext.User.IsInRole("Admin") || httpContext.User.IsInRole("SuperAdmin"));
    }
}
