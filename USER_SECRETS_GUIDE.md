# User Secrets Guide

This guide explains how to securely manage sensitive configuration data for local development using ASP.NET User Secrets.

## What are User Secrets?

User Secrets is a secure way to store sensitive data during local development. Secrets are stored outside your project directory (in your user profile folder) and never committed to source control.

## Quick Setup

### Option 1: Using the Setup Script (Recommended)

**macOS/Linux:**
```bash
./setup-secrets.sh
```

**Windows (PowerShell):**
```powershell
.\setup-secrets.ps1
```

The script will guide you through configuring all necessary secrets interactively.

### Option 2: Manual Setup

Set secrets manually using the .NET CLI:

```bash
cd BeC.OpenId.Connect

# Database connection
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"

# Google Maps API
dotnet user-secrets set "GoogleMaps:ApiKey" "your-google-maps-api-key"

# Stripe configuration
dotnet user-secrets set "Stripe:SecretKey" "your-stripe-secret-key"
dotnet user-secrets set "Stripe:PublishableKey" "your-stripe-publishable-key"
dotnet user-secrets set "Stripe:WebhookSecret" "your-webhook-secret"

# Email configuration (optional)
dotnet user-secrets set "EmailSettings:Username" "your-email@gmail.com"
dotnet user-secrets set "EmailSettings:Password" "your-app-password"
```

## Required Secrets

### 1. Database Connection String
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=localhost,1433;Database=dev-bec-openid-db;User Id=sa;Password=YourPassword;TrustServerCertificate=True;MultipleActiveResultSets=True"
```

### 2. Google Maps API Key
Get your API key from: https://console.cloud.google.com/apis/credentials

```bash
dotnet user-secrets set "GoogleMaps:ApiKey" "AIzaSy..."
```

**Important:** Restrict your API key to:
- HTTP referrers (for web apps)
- Specific APIs only (Distance Matrix API, Places API)
- IP addresses (for backend services)

### 3. Stripe Keys
Get your keys from: https://dashboard.stripe.com/test/apikeys

```bash
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_..."
```

For webhooks (using Stripe CLI):
```bash
stripe listen --forward-to localhost:5000/api/stripe/webhook
# Copy the webhook secret that's displayed
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..."
```

### 4. Email Settings (Optional)
For Gmail, you'll need an App Password: https://myaccount.google.com/apppasswords

```bash
dotnet user-secrets set "EmailSettings:Username" "your-email@gmail.com"
dotnet user-secrets set "EmailSettings:Password" "your-app-password"
```

## Managing Secrets

### List all secrets
```bash
dotnet user-secrets list --project BeC.OpenId.Connect
```

### Remove a specific secret
```bash
dotnet user-secrets remove "GoogleMaps:ApiKey" --project BeC.OpenId.Connect
```

### Clear all secrets
```bash
dotnet user-secrets clear --project BeC.OpenId.Connect
```

## Where are secrets stored?

User secrets are stored in a JSON file outside your project:

- **Windows:** `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
- **macOS/Linux:** `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

Your project's `user_secrets_id` is: `aspnet-BeC.OpenId.Connect-d9698535-3d69-4f3e-b40a-53e488f1b436`

## Configuration Priority

ASP.NET Core uses the following configuration priority (highest to lowest):

1. Command-line arguments
2. Environment variables
3. **User Secrets** (Development environment only)
4. appsettings.{Environment}.json
5. appsettings.json

This means User Secrets will override values from appsettings.json during local development.

## Production Deployment

User Secrets are **only for local development**. For production, use:

- **Railway/Heroku:** Environment Variables in dashboard
- **Azure:** App Service Configuration / Key Vault
- **AWS:** Systems Manager Parameter Store / Secrets Manager
- **Docker:** Environment variables or secrets management

See `DEPLOYMENT_GUIDE.md` for production configuration instructions.

## Security Best Practices

1. ✅ **Never commit secrets to git**
   - The `.gitignore` is configured to exclude `appsettings.json`
   - Always use User Secrets or environment variables

2. ✅ **Restrict API keys**
   - Google Maps: Use API restrictions and HTTP referrer restrictions
   - Stripe: Use test keys for development, restrict by IP for production

3. ✅ **Rotate keys regularly**
   - If a key is exposed, revoke it immediately and create a new one

4. ✅ **Use different keys per environment**
   - Development, staging, and production should each have separate keys

5. ✅ **Enable 2FA**
   - Enable two-factor authentication on all service accounts (Google Cloud, Stripe, etc.)

## Troubleshooting

### "User secret not found"
Make sure you're in the correct directory:
```bash
cd BeC.OpenId.Connect
dotnet user-secrets list
```

### "UserSecretsId is not set"
The project file should contain:
```xml
<UserSecretsId>aspnet-BeC.OpenId.Connect-d9698535-3d69-4f3e-b40a-53e488f1b436</UserSecretsId>
```

This is already configured in `BeC.OpenId.Connect.csproj`.

### Configuration not being loaded
Ensure you're running in Development environment:
```bash
export ASPNETCORE_ENVIRONMENT=Development  # macOS/Linux
$env:ASPNETCORE_ENVIRONMENT="Development"  # Windows PowerShell
```

### Need to share secrets with team?
**DO NOT share the secrets.json file.** Instead:
1. Share the setup scripts (`setup-secrets.sh` or `setup-secrets.ps1`)
2. Each developer should obtain their own API keys
3. For shared development resources, use a development environment or document how to obtain keys

## Additional Resources

- [ASP.NET Core User Secrets Documentation](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Google Cloud API Key Best Practices](https://cloud.google.com/docs/authentication/api-keys)
- [Stripe API Keys Guide](https://stripe.com/docs/keys)
