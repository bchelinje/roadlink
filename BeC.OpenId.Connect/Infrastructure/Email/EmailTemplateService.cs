using System;
using System.Text;

namespace BeC.OpenId.Connect.Infrastructure.Email
{
    /// <summary>
    /// Interface for email template rendering
    /// </summary>
    public interface IEmailTemplateService
    {
        string RenderEmailConfirmation(string userName, string confirmationLink);
        string RenderPasswordReset(string userName, string resetLink);
        string RenderWelcome(string userName);
        string RenderPasswordChanged(string userName);
    }

    /// <summary>
    /// Email template service with modern, responsive HTML templates
    /// </summary>
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly string _brandColor = "#4F46E5"; // Indigo-600
        private readonly string _brandName = "BeC OpenId Connect";
        private readonly string _supportEmail = "support@bec.com";

        public string RenderEmailConfirmation(string userName, string confirmationLink)
        {
            var template = GetBaseTemplate();
            var content = $@"
                <h1 style='color: #1F2937; font-size: 24px; font-weight: bold; margin: 0 0 16px 0;'>
                    Confirm Your Email Address
                </h1>
                <p style='color: #4B5563; font-size: 16px; line-height: 24px; margin: 0 0 24px 0;'>
                    Hi {EscapeHtml(userName)},
                </p>
                <p style='color: #4B5563; font-size: 16px; line-height: 24px; margin: 0 0 24px 0;'>
                    Thank you for registering with {_brandName}! Please confirm your email address by clicking the button below:
                </p>
                <table cellpadding='0' cellspacing='0' style='margin: 0 0 24px 0;'>
                    <tr>
                        <td style='border-radius: 8px; background-color: {_brandColor};'>
                            <a href='{EscapeHtml(confirmationLink)}' 
                               style='display: inline-block; padding: 12px 32px; color: #FFFFFF; text-decoration: none; font-size: 16px; font-weight: 600;'>
                                Confirm Email Address
                            </a>
                        </td>
                    </tr>
                </table>
                <p style='color: #6B7280; font-size: 14px; line-height: 20px; margin: 0 0 16px 0;'>
                    Or copy and paste this link into your browser:
                </p>
                <p style='color: #6B7280; font-size: 14px; line-height: 20px; word-break: break-all; margin: 0 0 24px 0;'>
                    {EscapeHtml(confirmationLink)}
                </p>
                <p style='color: #6B7280; font-size: 14px; line-height: 20px; margin: 0;'>
                    This link will expire in 24 hours. If you didn't create an account, you can safely ignore this email.
                </p>
            ";

            return template.Replace("{{CONTENT}}", content);
        }

        public string RenderPasswordReset(string userName, string resetLink)
        {
            var template = GetBaseTemplate();
            var content = $@"
                <h1 style='color: #1F2937; font-size: 24px; font-weight: bold; margin: 0 0 16px 0;'>
                    Reset Your Password
                </h1>
                <p style='color: #4B5563; font-size: 16px; line-height: 24px; margin: 0 0 24px 0;'>
                    Hi {EscapeHtml(userName)},
                </p>
                <p style='color: #4B5563; font-size: 16px; line-height: 24px; margin: 0 0 24px 0;'>
                    We received a request to reset your password. Click the button below to choose a new password:
                </p>
                <table cellpadding='0' cellspacing='0' style='margin: 0 0 24px 0;'>
                    <tr>
                        <td style='border-radius: 8px; background-color: {_brandColor};'>
                            <a href='{EscapeHtml(resetLink)}' 
                               style='display: inline-block; padding: 12px 32px; color: #FFFFFF; text-decoration: none; font-size: 16px; font-weight: 600;'>
                                Reset Password
                            </a>
                        </td>
                    </tr>
                </table>
                <p style='color: #6B7280; font-size: 14px; line-height: 20px; margin: 0 0 16px 0;'>
                    Or copy and paste this link into your browser:
                </p>
                <p style='color: #6B7280; font-size: 14px; line-height: 20px; word-break: break-all; margin: 0 0 24px 0;'>
                    {EscapeHtml(resetLink)}
                </p>
                <div style='background-color: #FEF3C7; border-left: 4px solid #F59E0B; padding: 16px; margin: 0 0 24px 0; border-radius: 4px;'>
                    <p style='color: #92400E; font-size: 14px; line-height: 20px; margin: 0;'>
                        <strong>Security Notice:</strong> This link will expire in 1 hour. If you didn't request a password reset, please ignore this email or contact support if you have concerns.
                    </p>
                </div>
            ";

            return template.Replace("{{CONTENT}}", content);
        }

        public string RenderWelcome(string userName)
        {
            var template = GetBaseTemplate();
            var content = $@"
                <h1 style='color: #1F2937; font-size: 24px; font-weight: bold; margin: 0 0 16px 0;'>
                    Welcome to {_brandName}!
                </h1>
                <p style='color: #4B5563; font-size: 16px; line-height: 24px; margin: 0 0 24px 0;'>
                    Hi {EscapeHtml(userName)},
                </p>
                <p style='color: #4B5563; font-size: 16px; line-height: 24px; margin: 0 0 24px 0;'>
                    Thank you for confirming your email address! Your account is now fully activated and ready to use.
                </p>
                <div style='background-color: #F3F4F6; border-radius: 8px; padding: 24px; margin: 0 0 24px 0;'>
                    <h2 style='color: #1F2937; font-size: 18px; font-weight: 600; margin: 0 0 16px 0;'>
                        Getting Started
                    </h2>
                    <ul style='color: #4B5563; font-size: 14px; line-height: 20px; margin: 0; padding-left: 20px;'>
                        <li style='margin-bottom: 8px;'>Complete your profile to personalize your experience</li>
                        <li style='margin-bottom: 8px;'>Explore our features and services</li>
                        <li style='margin-bottom: 8px;'>Set up two-factor authentication for enhanced security</li>
                        <li>Connect with our support team if you need any help</li>
                    </ul>
                </div>
                <p style='color: #4B5563; font-size: 16px; line-height: 24px; margin: 0 0 24px 0;'>
                    We're excited to have you on board!
                </p>
            ";

            return template.Replace("{{CONTENT}}", content);
        }

        public string RenderPasswordChanged(string userName)
        {
            var template = GetBaseTemplate();
            var content = $@"
                <h1 style='color: #1F2937; font-size: 24px; font-weight: bold; margin: 0 0 16px 0;'>
                    Password Changed Successfully
                </h1>
                <p style='color: #4B5563; font-size: 16px; line-height: 24px; margin: 0 0 24px 0;'>
                    Hi {EscapeHtml(userName)},
                </p>
                <p style='color: #4B5563; font-size: 16px; line-height: 24px; margin: 0 0 24px 0;'>
                    This is a confirmation that your password was successfully changed.
                </p>
                <div style='background-color: #ECFDF5; border-left: 4px solid #10B981; padding: 16px; margin: 0 0 24px 0; border-radius: 4px;'>
                    <p style='color: #065F46; font-size: 14px; line-height: 20px; margin: 0;'>
                        <strong>✓ Password Updated:</strong> {DateTime.UtcNow:MMMM dd, yyyy 'at' HH:mm} UTC
                    </p>
                </div>
                <div style='background-color: #FEF3C7; border-left: 4px solid #F59E0B; padding: 16px; margin: 0 0 24px 0; border-radius: 4px;'>
                    <p style='color: #92400E; font-size: 14px; line-height: 20px; margin: 0 0 12px 0;'>
                        <strong>Didn't make this change?</strong>
                    </p>
                    <p style='color: #92400E; font-size: 14px; line-height: 20px; margin: 0;'>
                        If you didn't change your password, please contact our security team immediately at {_supportEmail}
                    </p>
                </div>
            ";

            return template.Replace("{{CONTENT}}", content);
        }

        private string GetBaseTemplate()
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <meta http-equiv='X-UA-Compatible' content='IE=edge'>
    <title>{_brandName}</title>
    <!--[if mso]>
    <style type='text/css'>
        body, table, td {{font-family: Arial, Helvetica, sans-serif !important;}}
    </style>
    <![endif]-->
</head>
<body style='margin: 0; padding: 0; background-color: #F9FAFB; font-family: -apple-system, BlinkMacSystemFont, &quot;Segoe UI&quot;, Roboto, &quot;Helvetica Neue&quot;, Arial, sans-serif;'>
    <table cellpadding='0' cellspacing='0' border='0' width='100%' style='background-color: #F9FAFB; padding: 40px 0;'>
        <tr>
            <td align='center'>
                <table cellpadding='0' cellspacing='0' border='0' width='600' style='max-width: 600px; background-color: #FFFFFF; border-radius: 12px; box-shadow: 0 1px 3px rgba(0,0,0,0.1);'>
                    <!-- Header -->
                    <tr>
                        <td style='padding: 32px 40px; border-bottom: 1px solid #E5E7EB;'>
                            <table cellpadding='0' cellspacing='0' border='0' width='100%'>
                                <tr>
                                    <td>
                                        <div style='display: flex; align-items: center;'>
                                            <div style='width: 40px; height: 40px; background-color: {_brandColor}; border-radius: 8px; display: flex; align-items: center; justify-content: center; margin-right: 12px;'>
                                                <span style='color: #FFFFFF; font-size: 20px; font-weight: bold;'>BeC</span>
                                            </div>
                                            <span style='color: #1F2937; font-size: 20px; font-weight: 600;'>{_brandName}</span>
                                        </div>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    
                    <!-- Content -->
                    <tr>
                        <td style='padding: 40px;'>
                            {{{{CONTENT}}}}
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style='padding: 32px 40px; border-top: 1px solid #E5E7EB; background-color: #F9FAFB;'>
                            <p style='color: #6B7280; font-size: 12px; line-height: 18px; margin: 0 0 8px 0;'>
                                © {DateTime.UtcNow.Year} {_brandName}. All rights reserved.
                            </p>
                            <p style='color: #9CA3AF; font-size: 12px; line-height: 18px; margin: 0;'>
                                This is an automated message, please do not reply. For support, contact us at {_supportEmail}
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>
            ";
        }

        private string EscapeHtml(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }
    }
}