using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BeC.OpenId.Connect.Infrastructure.Email
{
    /// <summary>
    /// Extension methods for registering email services
    /// </summary>
    public static class EmailServiceExtensions
    {
        /// <summary>
        /// Adds email services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddEmailServices(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            // Register email settings
            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

            // Register template service
            services.AddSingleton<IEmailTemplateService, EmailTemplateService>();

            // Register email service based on configuration
            var emailSettings = configuration.GetSection("EmailSettings").Get<EmailSettings>();
            
            if (emailSettings?.UseMockService == true)
            {
                services.AddSingleton<IEmailService, MockEmailService>();
            }
            else
            {
                services.AddScoped<IEmailService, EmailService>();
            }

            return services;
        }

        /// <summary>
        /// Adds email services with custom settings
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configureOptions">Action to configure email settings</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddEmailServices(
            this IServiceCollection services,
            Action<EmailSettings> configureOptions)
        {
            services.Configure(configureOptions);
            services.AddSingleton<IEmailTemplateService, EmailTemplateService>();
            
            var settings = new EmailSettings();
            configureOptions(settings);
            
            if (settings.UseMockService)
            {
                services.AddSingleton<IEmailService, MockEmailService>();
            }
            else
            {
                services.AddScoped<IEmailService, EmailService>();
            }

            return services;
        }

        /// <summary>
        /// Adds mock email service for testing
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddMockEmailService(this IServiceCollection services)
        {
            services.AddSingleton<IEmailTemplateService, EmailTemplateService>();
            services.AddSingleton<IEmailService, MockEmailService>();
            return services;
        }
    }
}