# Customer Job Booking with Payment - Complete Flow

## Overview

The customer booking endpoint (`POST /api/customers/jobs/book`) integrates job creation with immediate payment capture, providing a seamless booking experience like Uber or other marketplace platforms.

## API Endpoint

```
POST /api/customers/jobs/book
Authorization: Bearer {customer_token}
Content-Type: application/json
```

## Request Body

```json
{
  "jobType": "local_move",
  "vehicleTypeRequired": "van",
  "priority": "normal",
  "scheduledDate": "2025-01-20T10:00:00Z",
  "scheduledTime": "10:00 AM",
  "estimatedDuration": 120,
  "customerPhone": "+1-555-123-4567",
  "customerEmail": "customer@example.com",
  "pickupLocation": "123 Main St, City, ST 12345",
  "pickupLatitude": 40.7128,
  "pickupLongitude": -74.0060,
  "deliveryLocation": "456 Oak Ave, City, ST 12345",
  "deliveryLatitude": 40.7580,
  "deliveryLongitude": -73.9855,
  "distance": 5.2,
  "items": [
    {
      "name": "Furniture",
      "quantity": 5,
      "weight": 200
    }
  ],
  "specialInstructions": "Please call when arriving"
}
```

## Response

```json
{
  "jobId": "550e8400-e29b-41d4-a716-446655440000",
  "jobNumber": "JOB-20250117-0001",
  "amount": 100.00,
  "platformFee": 15.00,
  "driverEarnings": 85.00,
  "currency": "usd",
  "paymentId": "660e8400-e29b-41d4-a716-446655440001",
  "paymentNumber": "PAY-20250117-ABC12345",
  "clientSecret": "pi_3KJ6XY2eZvKYlo2C0xxx_secret_yyy",
  "publishableKey": "pk_test_51Hxxx",
  "job": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "jobNumber": "JOB-20250117-0001",
    "customerId": "user-123",
    "customerName": "John Doe",
    "jobType": "local_move",
    "status": "pending",
    "scheduledDate": "2025-01-20T10:00:00Z",
    "pickupLocation": "123 Main St, City, ST 12345",
    "deliveryLocation": "456 Oak Ave, City, ST 12345",
    "distance": 5.2
  },
  "pricingBreakdown": {
    "baseFare": 25.00,
    "distanceCharge": 52.00,
    "timeCharge": 15.00,
    "vehicleTypeCharge": 8.00,
    "surgeMultiplier": 1.0,
    "totalPrice": 100.00
  }
}
```

## Frontend Integration

### 1. React/TypeScript Example

```typescript
import { loadStripe } from '@stripe/stripe-js';
import { CardElement, useStripe, useElements } from '@stripe/react-stripe-js';

const BookJobForm = () => {
  const stripe = useStripe();
  const elements = useElements();

  const handleBookJob = async (jobDetails) => {
    try {
      // 1. Create job and get payment intent
      const response = await fetch('/api/customers/jobs/book', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${accessToken}`
        },
        body: JSON.stringify(jobDetails)
      });

      if (!response.ok) {
        throw new Error('Booking failed');
      }

      const data = await response.json();

      // 2. Show pricing to customer
      console.log(`Job Amount: $${data.amount}`);
      console.log(`Platform Fee: $${data.platformFee}`);
      console.log(`Driver Earnings: $${data.driverEarnings}`);

      // 3. Confirm payment with Stripe
      const { error, paymentIntent } = await stripe.confirmCardPayment(
        data.clientSecret,
        {
          payment_method: {
            card: elements.getElement(CardElement),
            billing_details: {
              name: 'Customer Name',
              email: jobDetails.customerEmail
            }
          }
        }
      );

      if (error) {
        console.error('Payment failed:', error.message);
        // Handle payment error
        return;
      }

      if (paymentIntent.status === 'succeeded') {
        // 4. Payment successful!
        console.log('Job booked successfully!');
        console.log(`Job Number: ${data.jobNumber}`);

        // Redirect to job confirmation page
        window.location.href = `/jobs/${data.jobId}`;
      }
    } catch (error) {
      console.error('Booking error:', error);
    }
  };

  return (
    <form onSubmit={(e) => {
      e.preventDefault();
      handleBookJob(formData);
    }}>
      {/* Job details form fields */}
      <CardElement />
      <button type="submit">Book Job & Pay ${amount}</button>
    </form>
  );
};
```

### 2. Vanilla JavaScript Example

```javascript
// Load Stripe
const stripe = Stripe('pk_test_51Hxxx'); // Use publishableKey from response

async function bookJob(jobDetails) {
  try {
    // 1. Call booking API
    const response = await fetch('/api/customers/jobs/book', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ' + token
      },
      body: JSON.stringify(jobDetails)
    });

    const data = await response.json();

    // 2. Display pricing breakdown
    document.getElementById('amount').textContent = `$${data.amount}`;
    document.getElementById('platform-fee').textContent = `$${data.platformFee}`;
    document.getElementById('driver-earnings').textContent = `$${data.driverEarnings}`;

    // 3. Confirm payment with saved card or new card
    const result = await stripe.confirmCardPayment(data.clientSecret, {
      payment_method: 'pm_card_visa' // Or create new payment method
    });

    if (result.error) {
      alert('Payment failed: ' + result.error.message);
      return;
    }

    // 4. Success!
    alert(`Job booked! Job Number: ${data.jobNumber}`);
    window.location.href = '/my-jobs';

  } catch (error) {
    console.error('Booking failed:', error);
    alert('Failed to book job');
  }
}
```

## Complete Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    Customer Books Job                       │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  POST /api/customers/jobs/book                              │
│  - Job details (pickup, delivery, date, items)              │
│  - Customer info (phone, email)                             │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  Backend: Calculate Pricing                                 │
│  - Base fare: $25                                           │
│  - Distance (5.2 mi × $10/mi): $52                          │
│  - Time (120 min × $0.125/min): $15                         │
│  - Vehicle type: $8                                         │
│  = Total: $100                                              │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  Backend: Create Job Record                                 │
│  - Status: "pending"                                        │
│  - Job Number: "JOB-20250117-0001"                          │
│  - Stored in database                                       │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  Backend: Create Stripe Payment Intent                      │
│  - Amount: $100 (captured immediately)                      │
│  - Metadata: { jobId, platformFee: 15% }                    │
│  - Returns: clientSecret                                    │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  Backend: Create Payment Record                             │
│  - Amount: $100                                             │
│  - Platform Fee: $15 (15%)                                  │
│  - Driver Earnings: $85 (85%)                               │
│  - Status: "pending"                                        │
│  - Stripe Payment Intent ID: "pi_xxx"                       │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  Response to Frontend                                       │
│  - clientSecret (for Stripe.js)                             │
│  - Job details                                              │
│  - Pricing breakdown                                        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  Frontend: Confirm Payment with Stripe.js                   │
│  - Customer enters card details                             │
│  - stripe.confirmCardPayment(clientSecret)                  │
│  - Stripe processes payment                                 │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  Stripe Webhook: payment_intent.succeeded                   │
│  - Updates payment status to "completed"                    │
│  - Sends confirmation email                                 │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  Job Ready for Assignment!                                  │
│  - Admin assigns driver                                     │
│  - Job status → "assigned"                                  │
│  - Payment already captured and in escrow                   │
└─────────────────────────────────────────────────────────────┘
```

## Other Customer Endpoints

### Get My Booked Jobs

```
GET /api/customers/jobs?status=pending&page=1&pageSize=20
Authorization: Bearer {customer_token}
```

**Response:**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "jobNumber": "JOB-20250117-0001",
    "status": "pending",
    "scheduledDate": "2025-01-20T10:00:00Z",
    "pickupLocation": "123 Main St",
    "deliveryLocation": "456 Oak Ave",
    "driverName": null
  }
]
```

### Get Job Details

```
GET /api/customers/jobs/{jobId}
Authorization: Bearer {customer_token}
```

### Cancel Job (Automatic Refund)

```
POST /api/customers/jobs/{jobId}/cancel
Authorization: Bearer {customer_token}
Content-Type: application/json

{
  "reason": "Changed my mind"
}
```

**Response:**
```json
{
  "message": "Job cancelled successfully. Refund will be processed.",
  "jobNumber": "JOB-20250117-0001"
}
```

**Backend Action:**
- Updates job status to "cancelled"
- Processes full Stripe refund automatically
- Updates payment status to "refunded"
- Customer receives refund in 5-10 business days

## Payment States

| Job Status | Payment Status | Description |
|-----------|---------------|-------------|
| pending | pending | Job created, payment intent created |
| pending | completed | Payment confirmed by Stripe webhook |
| assigned | completed | Driver assigned, payment still in escrow |
| in_progress | processing | Job started, funds held in escrow |
| completed | completed | Job done, funds released (15%/85% split) |
| cancelled | refunded | Job cancelled, full refund processed |

## Error Handling

### Payment Declined

```json
{
  "error": "Failed to book job with payment",
  "details": "Your card was declined. Please try a different payment method."
}
```

### Insufficient Funds

```json
{
  "error": "Failed to book job with payment",
  "details": "Insufficient funds. Payment requires $100.00."
}
```

### Authentication Required

```json
{
  "error": "Failed to book job with payment",
  "details": "Your bank requires authentication. Please complete 3D Secure."
}
```

**Frontend should handle 3D Secure:**
```javascript
const { error, paymentIntent } = await stripe.confirmCardPayment(clientSecret);

if (error?.type === 'authentication_required') {
  // Redirect to 3D Secure page
  window.location.href = error.payment_intent.next_action.redirect_to_url.url;
}
```

## Security Features

1. **Customer Authorization Required**
   - Must have valid Bearer token
   - Must have "Customer" role

2. **Payment Captured Immediately**
   - No delayed charges
   - Funds secured on booking

3. **Automatic Refunds**
   - Cancelled jobs trigger immediate refund
   - No manual intervention needed

4. **Data Privacy**
   - Customers only see their own jobs
   - Card details never stored (handled by Stripe)

## Testing

### Test Cards (Stripe Test Mode)

```
Success: 4242 4242 4242 4242
Decline: 4000 0000 0000 0002
Insufficient Funds: 4000 0000 0000 9995
3D Secure Required: 4000 0025 0000 3155
```

### Test Flow

1. Create customer account
2. Login as customer
3. POST to `/api/customers/jobs/book` with test data
4. Use test card in Stripe.js
5. Verify payment captured
6. Check job created
7. Test cancellation and refund

## Production Checklist

- [ ] Configure production Stripe keys
- [ ] Update `publishableKey` in response (from settings)
- [ ] Set up Stripe Connect for driver payouts
- [ ] Enable 3D Secure authentication
- [ ] Configure webhook endpoint
- [ ] Test complete booking flow
- [ ] Test cancellation and refunds
- [ ] Set up monitoring and alerts
- [ ] Update terms of service
- [ ] Configure customer email confirmations

## Summary

The customer booking endpoint provides:

✅ **One-click booking with payment**
✅ **Automatic pricing calculation**
✅ **Immediate payment capture**
✅ **15%/85% commission split**
✅ **Easy cancellation with refunds**
✅ **Stripe.js integration ready**
✅ **Production-ready security**

Customers can now book jobs with integrated payment just like Uber, Lyft, or any modern marketplace platform!
