# RoadLink

A modern delivery and logistics platform connecting customers with drivers. Features include real-time job management, distance-based pricing, Stripe payments, and driver approval workflows.

## Features

- **Customer Portal**: Create and manage delivery jobs with real-time tracking
- **Driver Dashboard**: Accept jobs, manage deliveries, and track earnings
- **Admin Panel**: Approve drivers, monitor platform activity, and manage users
- **Distance-Based Pricing**: Automatic fare calculation using Google Maps Distance Matrix API
- **Secure Payments**: Integrated Stripe payment processing with escrow support
- **Authentication**: OpenID Connect implementation with role-based access control
- **Real-time Updates**: Live job status updates and notifications

## Technology Stack

### Backend
- **Framework**: ASP.NET Core 9.0
- **Authentication**: OpenIddict (OpenID Connect)
- **Database**: SQL Server with Entity Framework Core
- **Background Jobs**: Hangfire
- **Payment Processing**: Stripe.net
- **Maps Integration**: Google Maps API (Distance Matrix, Geocoding)

### Frontend
- **Framework**: Angular 19
- **UI Components**: Angular Material
- **State Management**: RxJS
- **Styling**: SCSS

## Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) or [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- [Google Maps API Key](https://console.cloud.google.com/apis/credentials)
- [Stripe Account](https://stripe.com/) (test mode)

### Backend Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/bchelinje/roadlink.git
   cd roadlink
   ```

2. **Configure User Secrets**

   Run the setup script to configure your local development secrets:
   ```bash
   ./setup-secrets.sh  # macOS/Linux
   # or
   .\setup-secrets.ps1  # Windows
   ```

   See [USER_SECRETS_GUIDE.md](USER_SECRETS_GUIDE.md) for detailed instructions.

3. **Run Database Migrations**
   ```bash
   cd BeC.OpenId.Connect
   dotnet ef database update
   ```

4. **Start the Backend**
   ```bash
   dotnet run
   ```

   The API will be available at `https://localhost:5001`

### Frontend Setup

1. **Install Dependencies**
   ```bash
   cd frontend
   npm install
   ```

2. **Configure API URL**

   Update `frontend/src/environments/environment.ts`:
   ```typescript
   export const environment = {
     production: false,
     apiUrl: 'https://localhost:5001'
   };
   ```

3. **Start the Frontend**
   ```bash
   npm start
   ```

   The app will be available at `http://localhost:4200`

## Project Structure

```
roadlink/
├── BeC.OpenId.Connect/          # Backend API
│   ├── Features/                # Feature-based organization
│   │   ├── Jobs/               # Job management
│   │   ├── Drivers/            # Driver management
│   │   ├── Users/              # User management
│   │   └── Maps/               # Maps integration
│   ├── Infrastructure/          # Cross-cutting concerns
│   └── appsettings.json.example # Configuration template
├── frontend/                    # Angular frontend
│   ├── src/
│   │   ├── app/
│   │   │   ├── customer/       # Customer features
│   │   │   ├── driver/         # Driver features
│   │   │   └── admin/          # Admin features
│   │   └── environments/       # Environment configs
└── setup-secrets.sh            # Local development setup
```

## Configuration

### Required Secrets

The application requires the following secrets for local development:

- **Database Connection**: SQL Server connection string
- **Google Maps API Key**: For distance calculations and geocoding
- **Stripe Keys**: Secret key, publishable key, and webhook secret
- **Email Settings**: SMTP configuration (optional)

Never commit secrets to git. Use User Secrets for local development and environment variables for production.

See [USER_SECRETS_GUIDE.md](USER_SECRETS_GUIDE.md) for complete setup instructions.

## Deployment

### Railway (Backend)

1. Create a new Railway project
2. Add PostgreSQL or SQL Server database
3. Configure environment variables (see [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md))
4. Deploy from GitHub

### Vercel (Frontend)

1. Import the `frontend` directory
2. Configure build settings:
   - Build Command: `npm run build`
   - Output Directory: `dist/bec-admin-dashboard`
3. Add environment variables:
   - `API_BASE_URL`: Your Railway backend URL

See [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md) for detailed deployment instructions.

## Documentation

- [User Secrets Guide](USER_SECRETS_GUIDE.md) - Secure local development setup
- [Deployment Guide](DEPLOYMENT_GUIDE.md) - Production deployment instructions
- [Distance & Payment Implementation](DISTANCE_PAYMENT_IMPLEMENTATION.md) - Distance-based pricing details
- [Testing Guide](TESTING_GUIDE.md) - Testing instructions

## Security

- All sensitive configuration is managed via User Secrets (local) or environment variables (production)
- API keys are restricted by domain/IP
- Passwords are hashed using ASP.NET Core Identity
- HTTPS enforced in production
- CORS configured for specific origins
- SQL injection protection via EF Core parameterized queries

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is proprietary software. All rights reserved.

## Author

**Blessing Chelinje**
GitHub: [@bchelinje](https://github.com/bchelinje)

## Support

For issues and questions, please open an issue on GitHub.
