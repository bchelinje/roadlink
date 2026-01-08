# Setup script for ASP.NET User Secrets (PowerShell)
# This script helps you configure secrets for local development

Write-Host "🔐 BeC OpenId Connect - User Secrets Setup" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

$ProjectPath = "BeC.OpenId.Connect/BeC.OpenId.Connect.csproj"

# Check if dotnet CLI is available
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "❌ Error: .NET SDK not found. Please install .NET SDK first." -ForegroundColor Red
    exit 1
}

Write-Host "This script will help you configure secrets for local development."
Write-Host ""

# Database Connection String
Write-Host "📊 Database Configuration" -ForegroundColor Yellow
Write-Host "------------------------"
$DbConnection = Read-Host "Enter your database connection string (press Enter for default localhost)"
if ([string]::IsNullOrWhiteSpace($DbConnection)) {
    $DbConnection = "Server=localhost,1433;Database=dev-bec-openid-db;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=True"
}
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "$DbConnection" --project $ProjectPath
Write-Host "✅ Database connection configured" -ForegroundColor Green
Write-Host ""

# Google Maps API Key
Write-Host "🗺️  Google Maps Configuration" -ForegroundColor Yellow
Write-Host "----------------------------"
$GoogleMapsKey = Read-Host "Enter your Google Maps API Key"
if (-not [string]::IsNullOrWhiteSpace($GoogleMapsKey)) {
    dotnet user-secrets set "GoogleMaps:ApiKey" "$GoogleMapsKey" --project $ProjectPath
    Write-Host "✅ Google Maps API key configured" -ForegroundColor Green
} else {
    Write-Host "⚠️  Skipped Google Maps API key" -ForegroundColor Yellow
}
Write-Host ""

# Stripe Configuration
Write-Host "💳 Stripe Configuration" -ForegroundColor Yellow
Write-Host "---------------------"
$StripeSecret = Read-Host "Enter your Stripe Secret Key (sk_test_...)"
if (-not [string]::IsNullOrWhiteSpace($StripeSecret)) {
    dotnet user-secrets set "Stripe:SecretKey" "$StripeSecret" --project $ProjectPath
    Write-Host "✅ Stripe Secret Key configured" -ForegroundColor Green
} else {
    Write-Host "⚠️  Skipped Stripe Secret Key" -ForegroundColor Yellow
}

$StripePublishable = Read-Host "Enter your Stripe Publishable Key (pk_test_...)"
if (-not [string]::IsNullOrWhiteSpace($StripePublishable)) {
    dotnet user-secrets set "Stripe:PublishableKey" "$StripePublishable" --project $ProjectPath
    Write-Host "✅ Stripe Publishable Key configured" -ForegroundColor Green
} else {
    Write-Host "⚠️  Skipped Stripe Publishable Key" -ForegroundColor Yellow
}

$StripeWebhook = Read-Host "Enter your Stripe Webhook Secret (whsec_...)"
if (-not [string]::IsNullOrWhiteSpace($StripeWebhook)) {
    dotnet user-secrets set "Stripe:WebhookSecret" "$StripeWebhook" --project $ProjectPath
    Write-Host "✅ Stripe Webhook Secret configured" -ForegroundColor Green
} else {
    Write-Host "⚠️  Skipped Stripe Webhook Secret" -ForegroundColor Yellow
}
Write-Host ""

# Email Configuration
Write-Host "📧 Email Configuration (Optional)" -ForegroundColor Yellow
Write-Host "--------------------------------"
$EmailUsername = Read-Host "Enter your email address (or press Enter to skip)"
if (-not [string]::IsNullOrWhiteSpace($EmailUsername)) {
    dotnet user-secrets set "EmailSettings:Username" "$EmailUsername" --project $ProjectPath
    $EmailPassword = Read-Host "Enter your email app password" -AsSecureString
    $EmailPasswordPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($EmailPassword))
    dotnet user-secrets set "EmailSettings:Password" "$EmailPasswordPlain" --project $ProjectPath
    Write-Host "✅ Email settings configured" -ForegroundColor Green
} else {
    Write-Host "⚠️  Skipped email configuration" -ForegroundColor Yellow
}
Write-Host ""

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "✅ User Secrets setup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "To view your secrets, run:"
Write-Host "  dotnet user-secrets list --project $ProjectPath" -ForegroundColor Gray
Write-Host ""
Write-Host "To remove a secret, run:"
Write-Host "  dotnet user-secrets remove `"SecretKey`" --project $ProjectPath" -ForegroundColor Gray
Write-Host ""
Write-Host "To clear all secrets, run:"
Write-Host "  dotnet user-secrets clear --project $ProjectPath" -ForegroundColor Gray
Write-Host ""
