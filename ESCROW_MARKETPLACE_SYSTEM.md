# Escrow Marketplace Payment System

## Overview

This document describes the complete escrow marketplace payment system implemented for the delivery/moving service platform. The system works exactly like Uber, Upwork, and Fiverr - capturing payments upfront, holding funds in escrow, and automatically splitting commissions when jobs complete.

## How It Works

### Payment Flow

```
1. Customer books job → Payment captured (card charged immediately)
2. Job assigned to driver → Funds held in escrow
3. Job starts → Status: "processing" (escrow hold confirmed)
4. Job completes → Funds released with automatic split:
   - 15% to platform (commission)
   - 85% to driver (earnings)
5. Automated weekly payouts → Drivers receive their accumulated earnings

Alternative flows:
- Job cancelled → Full refund to customer
- Partial cancellation → Partial refund
```

### Commission Split

- **Platform Fee**: 15% of job amount
- **Driver Earnings**: 85% of job amount + 100% of tips

Example:
- Job amount: $100
- Platform fee: $15
- Driver earnings: $85
- Customer tip: $10 (100% goes to driver)
- Driver total: $95

## Architecture

### Services

1. **StripePaymentService** (`Infrastructure/Payments/StripePaymentService.cs`)
   - Creates Stripe payment intents
   - Captures funds to escrow
   - Releases funds with commission splits
   - Processes refunds
   - Handles webhook events

2. **JobPaymentAutomationService** (`Infrastructure/Payments/JobPaymentAutomationService.cs`)
   - Automates payment operations based on job status
   - Triggers escrow hold when job starts
   - Releases funds when job completes
   - Processes refunds when job cancelled

3. **PayoutSchedulerService** (`Infrastructure/Payments/PayoutSchedulerService.cs`)
   - Background service running on schedule (weekly/daily/monthly)
   - Batches all pending driver earnings
   - Creates payouts automatically
   - Can be configured in `appsettings.json`

### Controllers

1. **StripeWebhookController** (`Features/Payments/Controllers/StripeWebhookController.cs`)
   - Receives Stripe webhook events
   - Verifies webhook signatures for security
   - Updates payment status based on events

2. **PaymentsController** (existing, enhanced)
   - Payment CRUD operations
   - Customer payment history
   - Admin payment statistics
   - Refund processing

3. **JobsController** (enhanced with payment automation)
   - Job status updates trigger payment actions:
     - `in_progress` → Hold in escrow
     - `completed` → Release funds
     - `cancelled` → Refund customer

### Database Models

**Payment** (existing, uses Stripe fields):
- `StripePaymentIntentId` - Stripe payment intent
- `StripeChargeId` - Stripe charge reference
- `StripeRefundId` - Refund reference
- `PlatformFee` - 15% commission
- `DriverEarnings` - 85% to driver
- `Status` - pending, processing, completed, refunded

**Earning** (updated with payment tracking):
- `PaymentId` - Links to payment
- `PayoutId` - Links to payout batch
- `PaidAt` - When driver was paid
- `EarnedAt` - When earning was recorded
- `PaymentStatus` - pending, paid

**Payout** (existing):
- `StripePayoutId` - Stripe payout reference
- `DriverId` - Driver receiving payout
- `Amount` - Total payout amount
- `PaymentIds` - JSON array of payment IDs
- `PeriodStart/End` - Payout period

## Configuration

### appsettings.json

```json
{
  "Stripe": {
    "SecretKey": "sk_test_YOUR_STRIPE_SECRET_KEY_HERE",
    "PublishableKey": "pk_test_YOUR_STRIPE_PUBLISHABLE_KEY_HERE",
    "WebhookSecret": "whsec_YOUR_WEBHOOK_SECRET_HERE",
    "PlatformFeePercent": 15,
    "Currency": "usd",
    "PaymentDescription": "Delivery Service Payment",
    "EscrowHoldDays": 1,
    "AutoPayoutSchedule": "weekly"
  }
}
```

### Environment Setup

1. **Get Stripe API Keys**:
   - Sign up at https://dashboard.stripe.com
   - Copy Secret Key from Developers → API Keys
   - Copy Publishable Key
   - Update `appsettings.json`

2. **Configure Webhook**:
   - Go to Developers → Webhooks
   - Add endpoint: `https://your-domain.com/api/webhooks/stripe`
   - Select events: `payment_intent.succeeded`, `payment_intent.payment_failed`, `charge.refunded`, `payout.paid`, `payout.failed`
   - Copy webhook secret to `appsettings.json`

3. **Run Database Migration** (for new Earning fields):
   ```bash
   dotnet ef migrations add AddPaymentTrackingToEarnings
   dotnet ef database update
   ```

## API Endpoints

### Payment Endpoints

```
POST   /api/payments                    - Create payment (internal use)
GET    /api/payments/{id}                - Get payment details
GET    /api/payments/jobs/{jobId}        - Get job payments
POST   /api/payments/{id}/refund         - Process refund (Admin/Customer)
GET    /api/customers/me/payments        - My payment history (Customer)
GET    /api/drivers/me/payouts           - My payout history (Driver)
GET    /api/payments/statistics          - Payment stats (Admin)
```

### Webhook Endpoint

```
POST   /api/webhooks/stripe              - Stripe webhook (public, signature verified)
GET    /api/webhooks/stripe/health       - Webhook health check
```

### Job Status Updates (with payment automation)

```
PATCH  /api/jobs/{id}/status
```

**Status changes trigger payments**:
- `in_progress` → Holds funds in escrow
- `completed` → Releases funds (15%/85% split)
- `cancelled` → Refunds customer

## Usage Examples

### 1. Customer Books Job

**Frontend Flow**:
1. Customer creates job (not yet charged)
2. Job assigned to driver
3. Customer provides payment method
4. Payment captured immediately

**Backend (automatic)**:
```csharp
// In JobPaymentAutomationService
var payment = await CreatePaymentForJobAsync(
    jobId: job.Id,
    customerId: customer.Id,
    amount: 100.00m
);

// Creates Stripe payment intent
// Captures $100 from customer's card
// Calculates: Platform $15, Driver $85
```

### 2. Job Starts

**Driver Updates Status** → `in_progress`

**Backend (automatic)**:
```csharp
// In JobsController.UpdateJobStatus()
await _paymentAutomationService.HandleJobStartedAsync(jobId);

// Updates payment status to "processing"
// Funds now held in escrow
```

### 3. Job Completes

**Driver Updates Status** → `completed`

**Backend (automatic)**:
```csharp
// In JobsController.UpdateJobStatus()
await _paymentAutomationService.HandleJobCompletedAsync(jobId);

// Releases from escrow
// Creates driver earning record:
//   - BaseAmount: $100
//   - NetAmount: $85 (after 15% fee)
//   - PaymentStatus: "pending"
```

### 4. Weekly Payouts

**Background Service** (runs automatically every week)

```csharp
// PayoutSchedulerService
// Finds all drivers with pending earnings
// Creates batch payouts:
//   - Driver A: $425 (5 jobs @ $85 each)
//   - Driver B: $680 (8 jobs @ $85 each)
// Marks earnings as "paid"
```

### 5. Job Cancelled

**Admin/Customer Cancels** → `cancelled`

**Backend (automatic)**:
```csharp
// In JobsController.UpdateJobStatus()
await _paymentAutomationService.HandleJobCancelledAsync(
    jobId,
    reason: "Customer request"
);

// Processes full Stripe refund
// Updates payment status to "refunded"
// Customer receives $100 back
```

## Security Features

1. **Webhook Signature Verification**
   - All Stripe webhooks verified using secret
   - Prevents unauthorized payment updates

2. **Role-Based Access**
   - Customers: View own payments only
   - Drivers: View own payouts only
   - Admins: Full payment management access

3. **Payment Status Tracking**
   - Complete audit trail in database
   - Activity logs for all payment actions
   - Status history for compliance

4. **Idempotency**
   - Duplicate webhook events handled gracefully
   - Payment intent IDs prevent double-charging

## Production Checklist

### Before Going Live

1. **Switch to Live Stripe Keys**:
   - Get production keys from Stripe Dashboard
   - Update `appsettings.Production.json`
   - Remove test mode indicators

2. **Configure Stripe Connect** (for direct driver payouts):
   - Enable Stripe Connect in Dashboard
   - Update `StripePaymentService.CreateDriverPayoutAsync()` to use Connect transfers
   - Onboard drivers to Connect accounts

3. **Set Up Monitoring**:
   - Monitor webhook endpoint health
   - Alert on payment failures
   - Track payout success rates

4. **Compliance**:
   - Update terms of service (marketplace model)
   - Ensure PCI compliance (Stripe handles this)
   - Configure tax reporting (1099s for drivers)

5. **Testing**:
   - Test full flow in Stripe test mode
   - Verify refunds work correctly
   - Test webhook failure scenarios
   - Verify payout calculations

### Stripe Connect Setup (Recommended for Production)

For production, use **Stripe Connect** to pay drivers directly to their bank accounts:

1. **Enable Connect**:
   ```
   Dashboard → Connect → Get Started
   Choose "Express" or "Standard" accounts
   ```

2. **Update Payout Service**:
   ```csharp
   // In CreateDriverPayoutAsync
   var transferOptions = new TransferCreateOptions
   {
       Amount = ConvertToStripeAmount(totalAmount),
       Currency = "usd",
       Destination = driver.StripeAccountId, // Driver's Connect account
       Description = $"Payout for {pendingEarnings.Count} completed jobs"
   };

   var transferService = new TransferService();
   var transfer = await transferService.CreateAsync(transferOptions);
   ```

3. **Driver Onboarding**:
   - Add Stripe Connect onboarding flow
   - Collect bank account details
   - Store `StripeAccountId` on Driver model

## Troubleshooting

### Payment Not Captured

**Issue**: Job created but no payment

**Solution**: Payment creation happens when job is booked with payment method. Ensure frontend calls payment intent creation.

### Funds Not Released

**Issue**: Job completed but driver not paid

**Solution**: Check that job status changed to "completed". Verify `HandleJobCompletedAsync` was called. Check activity logs for payment automation errors.

### Webhook Not Working

**Issue**: Stripe events not updating payment status

**Solution**:
1. Verify webhook URL is publicly accessible
2. Check webhook signature secret matches
3. Review webhook logs in Stripe Dashboard
4. Test with Stripe CLI: `stripe listen --forward-to localhost:5000/api/webhooks/stripe`

### Payout Not Created

**Issue**: Weekly payouts not happening

**Solution**:
1. Check `PayoutSchedulerService` is running (registered as HostedService)
2. Verify earnings have `PaymentStatus = "pending"`
3. Check scheduler logs for errors
4. Verify `AutoPayoutSchedule` configuration

## Testing

### Local Testing with Stripe CLI

1. **Install Stripe CLI**:
   ```bash
   brew install stripe/stripe-cli/stripe
   stripe login
   ```

2. **Forward Webhooks**:
   ```bash
   stripe listen --forward-to http://localhost:5000/api/webhooks/stripe
   ```

3. **Trigger Test Events**:
   ```bash
   stripe trigger payment_intent.succeeded
   stripe trigger charge.refunded
   ```

### Test Cards

```
Success: 4242 4242 4242 4242
Decline: 4000 0000 0000 0002
Authentication Required: 4000 0025 0000 3155
```

## Summary

This escrow marketplace system provides:

✅ **Automatic payment capture** on job booking
✅ **Funds held in escrow** until job completion
✅ **15%/85% commission split** automatically
✅ **Automated weekly driver payouts**
✅ **Full refunds** on cancellation
✅ **Webhook integration** for real-time updates
✅ **Complete audit trail** for compliance
✅ **Production-ready** with Stripe integration

The platform owner never handles money directly - everything flows through Stripe, with automatic commission deduction and driver payouts.

## Support

For questions or issues:
- Review Stripe Dashboard for payment details
- Check activity logs for automation errors
- Review webhook events in Stripe Dashboard
- Contact Stripe Support for payment gateway issues
