#!/bin/bash

# Setup script for ASP.NET User Secrets
# This script helps you configure secrets for local development

echo "🔐 BeC OpenId Connect - User Secrets Setup"
echo "=========================================="
echo ""

PROJECT_PATH="BeC.OpenId.Connect/BeC.OpenId.Connect.csproj"

# Check if dotnet CLI is available
if ! command -v dotnet &> /dev/null; then
    echo "❌ Error: .NET SDK not found. Please install .NET SDK first."
    exit 1
fi

echo "This script will help you configure secrets for local development."
echo ""

# Database Connection String
echo "📊 Database Configuration"
echo "------------------------"
read -p "Enter your database connection string (press Enter for default localhost): " DB_CONNECTION
if [ -z "$DB_CONNECTION" ]; then
    DB_CONNECTION="Server=localhost,1433;Database=dev-bec-openid-db;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=True"
fi
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "$DB_CONNECTION" --project "$PROJECT_PATH"
echo "✅ Database connection configured"
echo ""

# Google Maps API Key
echo "🗺️  Google Maps Configuration"
echo "----------------------------"
read -p "Enter your Google Maps API Key: " GOOGLE_MAPS_KEY
if [ ! -z "$GOOGLE_MAPS_KEY" ]; then
    dotnet user-secrets set "GoogleMaps:ApiKey" "$GOOGLE_MAPS_KEY" --project "$PROJECT_PATH"
    echo "✅ Google Maps API key configured"
else
    echo "⚠️  Skipped Google Maps API key"
fi
echo ""

# Stripe Configuration
echo "💳 Stripe Configuration"
echo "---------------------"
read -p "Enter your Stripe Secret Key (sk_test_...): " STRIPE_SECRET
if [ ! -z "$STRIPE_SECRET" ]; then
    dotnet user-secrets set "Stripe:SecretKey" "$STRIPE_SECRET" --project "$PROJECT_PATH"
    echo "✅ Stripe Secret Key configured"
else
    echo "⚠️  Skipped Stripe Secret Key"
fi

read -p "Enter your Stripe Publishable Key (pk_test_...): " STRIPE_PUBLISHABLE
if [ ! -z "$STRIPE_PUBLISHABLE" ]; then
    dotnet user-secrets set "Stripe:PublishableKey" "$STRIPE_PUBLISHABLE" --project "$PROJECT_PATH"
    echo "✅ Stripe Publishable Key configured"
else
    echo "⚠️  Skipped Stripe Publishable Key"
fi

read -p "Enter your Stripe Webhook Secret (whsec_...): " STRIPE_WEBHOOK
if [ ! -z "$STRIPE_WEBHOOK" ]; then
    dotnet user-secrets set "Stripe:WebhookSecret" "$STRIPE_WEBHOOK" --project "$PROJECT_PATH"
    echo "✅ Stripe Webhook Secret configured"
else
    echo "⚠️  Skipped Stripe Webhook Secret"
fi
echo ""

# Email Configuration
echo "📧 Email Configuration (Optional)"
echo "--------------------------------"
read -p "Enter your email address (or press Enter to skip): " EMAIL_USERNAME
if [ ! -z "$EMAIL_USERNAME" ]; then
    dotnet user-secrets set "EmailSettings:Username" "$EMAIL_USERNAME" --project "$PROJECT_PATH"
    read -p "Enter your email app password: " EMAIL_PASSWORD
    dotnet user-secrets set "EmailSettings:Password" "$EMAIL_PASSWORD" --project "$PROJECT_PATH"
    echo "✅ Email settings configured"
else
    echo "⚠️  Skipped email configuration"
fi
echo ""

echo "=========================================="
echo "✅ User Secrets setup complete!"
echo ""
echo "To view your secrets, run:"
echo "  dotnet user-secrets list --project $PROJECT_PATH"
echo ""
echo "To remove a secret, run:"
echo "  dotnet user-secrets remove \"SecretKey\" --project $PROJECT_PATH"
echo ""
echo "To clear all secrets, run:"
echo "  dotnet user-secrets clear --project $PROJECT_PATH"
echo ""
