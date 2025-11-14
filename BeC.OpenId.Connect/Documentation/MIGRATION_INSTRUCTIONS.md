# Database Migration Instructions

## New Features Added

I've added 6 new controllers with comprehensive endpoints:

1. **CustomersController** - Customer job management
2. **ReviewsController** - Rating and review system
3. **VehiclesController** - Vehicle CRUD operations
4. **DocumentsController** - Driver document verification
5. **NotificationsController** - User notifications
6. **PaymentsController** - Payment and payout processing

## New Database Tables

The following entities have been added to `ApplicationDbContext`:

- `Reviews` - Customer and driver reviews
- `Notifications` - User notifications
- `Payments` - Payment transactions
- `Payouts` - Driver payouts

## Steps to Apply Migrations

### 1. Create the Migration

```bash
cd /home/user/BeC.OpenId.Connect/BeC.OpenId.Connect
dotnet ef migrations add AddReviewsNotificationsPayments
```

### 2. Review the Migration

Check the generated migration file in `Migrations/` folder to ensure it looks correct.

### 3. Apply the Migration

```bash
dotnet ef database update
```

Or if you're using a specific connection string:

```bash
dotnet ef database update --connection "YourConnectionString"
```

### 4. Verify Database Schema

After migration, verify that the following tables exist:

- Reviews
- Notifications
- Payments
- Payouts

## Testing the New Endpoints

### Test Customer Endpoints

```bash
# Create a job (as Customer)
POST /api/customers/me/jobs

# Get my jobs
GET /api/customers/me/jobs

# Cancel a job
PATCH /api/customers/me/jobs/{id}/cancel

# Review a driver
POST /api/customers/me/jobs/{jobId}/review

# Get my stats
GET /api/customers/me/stats
```

### Test Review Endpoints

```bash
# Get driver reviews
GET /api/reviews/drivers/{id}

# Create review
POST /api/reviews

# Update review
PUT /api/reviews/{id}

# Report review
POST /api/reviews/{id}/report

# Respond to review
POST /api/reviews/{id}/response
```

### Test Vehicle Endpoints

```bash
# Get my vehicles (Driver)
GET /api/drivers/me/vehicles

# Add vehicle
POST /api/drivers/me/vehicles

# Update vehicle
PUT /api/vehicles/{id}

# Update vehicle status
PATCH /api/vehicles/{id}/status

# Log maintenance
POST /api/vehicles/{id}/maintenance
```

### Test Document Endpoints

```bash
# Get my documents (Driver)
GET /api/drivers/me/documents

# Upload document
POST /api/drivers/me/documents

# Get pending documents (Admin)
GET /api/documents/pending

# Verify document (Admin)
POST /api/documents/{id}/verify

# Reject document (Admin)
POST /api/documents/{id}/reject

# Get expiring documents (Admin)
GET /api/documents/expiring
```

### Test Notification Endpoints

```bash
# Get my notifications
GET /api/notifications/me

# Get unread count
GET /api/notifications/me/unread-count

# Mark as read
PATCH /api/notifications/{id}/read

# Mark all as read
PATCH /api/notifications/read-all

# Send notification (Admin)
POST /api/notifications/send

# Broadcast notification (Admin)
POST /api/notifications/broadcast
```

### Test Payment Endpoints

```bash
# Create payment
POST /api/payments

# Get payment
GET /api/payments/{id}

# Get my payments (Customer)
GET /api/customers/me/payments

# Get my earnings (Driver)
GET /api/drivers/me/earnings

# Get my payouts (Driver)
GET /api/drivers/me/payouts

# Process refund (Admin)
POST /api/payments/{id}/refund

# Get payment statistics (Admin)
GET /api/payments/statistics

# Create payout (Admin)
POST /api/payments/payouts
```

## Build and Run

```bash
# Build the project
dotnet build

# Run the project
dotnet run

# The API will be available at:
# https://localhost:5001 (or your configured port)
# Swagger UI: https://localhost:5001/swagger
```

## Troubleshooting

### Migration Errors

If you get migration errors:

1. Check that all required packages are installed
2. Verify connection string in `appsettings.json`
3. Ensure SQL Server is running
4. Check for any pending migrations: `dotnet ef migrations list`

### Build Errors

If you get build errors:

1. Restore packages: `dotnet restore`
2. Clean solution: `dotnet clean`
3. Rebuild: `dotnet build`

### Runtime Errors

If you get runtime errors:

1. Check that database is updated
2. Verify all services are registered in `Program.cs`
3. Check application logs
4. Ensure proper authentication/authorization setup

## Notes

- All new endpoints are documented with Swagger/OpenAPI
- Proper authorization is implemented (roles: Customer, Driver, Admin, SuperAdmin)
- Activity logging is integrated for all major actions
- All monetary values use decimal with precision (10,2)
- JSON serialization is used for complex data (addresses, items, photos, etc.)

## Next Steps

1. Apply migrations
2. Test endpoints using Swagger or Postman
3. Implement Stripe integration for real payments (currently placeholder)
4. Add email/push notification delivery (currently placeholder)
5. Implement favorites feature for customers
6. Add more comprehensive analytics and reporting
