# Real-Time Features Documentation

This document explains how to use the new real-time features added to the BeC Moving Services platform.

## Table of Contents
1. [SignalR Real-Time Notifications](#signalr-real-time-notifications)
2. [Background Jobs with Hangfire](#background-jobs-with-hangfire)
3. [Email Service](#email-service)
4. [Configuration](#configuration)

---

## SignalR Real-Time Notifications

SignalR enables real-time, bi-directional communication between clients and the server.

### Hub Endpoint
```
/hubs/notifications
```

### Client Connection (JavaScript/TypeScript)

#### Installation
```bash
npm install @microsoft/signalr
```

#### Basic Connection
```typescript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("https://your-api.com/hubs/notifications", {
    accessTokenFactory: () => yourAccessToken
  })
  .withAutomaticReconnect()
  .configureLogging(signalR.LogLevel.Information)
  .build();

// Start connection
connection.start()
  .then(() => console.log("Connected to notification hub"))
  .catch(err => console.error("Connection error:", err));

// Listen for notifications
connection.on("ReceiveNotification", (notification) => {
  console.log("Notification received:", notification);
  // notification structure:
  // {
  //   type: "job_created" | "job_updated" | "payment_completed" | etc.,
  //   data: { /* type-specific data */ },
  //   timestamp: "2025-01-15T10:30:00Z"
  // }
});
```

#### Advanced Usage - Join Job Group
```typescript
// Customer or driver joins a job-specific channel
async function joinJobChannel(jobId: string) {
  await connection.invoke("JoinJobGroup", jobId);
  console.log(`Joined job group: ${jobId}`);
}

// Leave job group when done
async function leaveJobChannel(jobId: string) {
  await connection.invoke("LeaveJobGroup", jobId);
}
```

#### Mark Notification as Read
```typescript
async function markAsRead(notificationId: string) {
  await connection.invoke("MarkAsRead", notificationId);
}
```

### Notification Types

| Type | Description | Sent To | Data Fields |
|------|-------------|---------|-------------|
| `job_created` | New job created | Driver (role-based) | jobId, jobNumber, customer |
| `job_assigned` | Job assigned to driver | Driver (specific user) | jobId, jobNumber, scheduledDate |
| `job_started` | Driver started job | Customer (specific user) | jobId, driverName, eta |
| `job_completed` | Job completed | Customer (specific user) | jobId, totalAmount |
| `payment_completed` | Payment successful | Driver & Customer | paymentId, amount, jobId |
| `document_verified` | Document approved | Driver (specific user) | documentId, documentType |
| `document_rejected` | Document rejected | Driver (specific user) | documentId, reason |
| `payout_completed` | Payout processed | Driver (specific user) | payoutId, amount |
| `document_expiry_warning` | Document expiring soon | Driver (specific user) | documentId, daysUntilExpiry |

### Angular Example Component

```typescript
import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private hubConnection: signalR.HubConnection;
  public notifications$ = new Subject<any>();

  constructor(private authService: AuthService) {}

  async startConnection() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('https://your-api.com/hubs/notifications', {
        accessTokenFactory: () => this.authService.getToken()
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveNotification', (notification) => {
      this.notifications$.next(notification);
      this.showToast(notification);
    });

    await this.hubConnection.start();
  }

  private showToast(notification: any) {
    // Show toast/snackbar based on notification type
    switch (notification.type) {
      case 'job_assigned':
        this.toastService.success(`New job assigned: ${notification.data.jobNumber}`);
        break;
      case 'payment_completed':
        this.toastService.success(`Payment received: $${notification.data.amount}`);
        break;
      // ... other cases
    }
  }

  async joinJobGroup(jobId: string) {
    await this.hubConnection.invoke('JoinJobGroup', jobId);
  }

  async stopConnection() {
    await this.hubConnection.stop();
  }
}
```

### React Example Hook

```typescript
import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';

export function useNotifications(accessToken: string) {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [notifications, setNotifications] = useState<any[]>([]);

  useEffect(() => {
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl('https://your-api.com/hubs/notifications', {
        accessTokenFactory: () => accessToken
      })
      .withAutomaticReconnect()
      .build();

    newConnection.on('ReceiveNotification', (notification) => {
      setNotifications(prev => [notification, ...prev]);
      // Show toast notification
      toast.info(notification.data.message);
    });

    newConnection.start().then(() => {
      console.log('Connected to notifications');
      setConnection(newConnection);
    });

    return () => {
      newConnection.stop();
    };
  }, [accessToken]);

  const joinJobGroup = async (jobId: string) => {
    if (connection) {
      await connection.invoke('JoinJobGroup', jobId);
    }
  };

  return { notifications, joinJobGroup };
}
```

---

## Background Jobs with Hangfire

Hangfire handles scheduled and recurring tasks automatically.

### Dashboard Access
```
https://your-api.com/hangfire
```
- **Development**: Open to all
- **Production**: Requires Admin or SuperAdmin role

### Configured Recurring Jobs

#### 1. Document Expiry Reminders
- **Schedule**: Daily at 9:00 AM UTC
- **Job ID**: `document-expiry-reminder`
- **Purpose**: Sends email and push notifications to drivers 30 days before document expiry
- **Actions**:
  - Checks all verified documents expiring within 30 days
  - Sends email reminder
  - Sends real-time notification

#### 2. Notification Cleanup
- **Schedule**: Every hour
- **Job ID**: `notification-cleanup`
- **Purpose**: Removes old notifications to keep database clean
- **Actions**:
  - Deletes notifications older than 30 days
  - Deletes expired notifications (based on ExpiresAt field)

#### 3. Weekly Driver Payouts
- **Schedule**: Weekly on Monday at 8:00 AM UTC
- **Job ID**: `weekly-driver-payout`
- **Purpose**: Processes driver payouts for completed jobs
- **Actions**:
  - Finds all pending payouts from the past week
  - Processes each payout (integrates with payment provider)
  - Sends email confirmation
  - Sends real-time notification

#### 4. Daily Reports
- **Schedule**: Daily at midnight UTC
- **Job ID**: `daily-report`
- **Purpose**: Generates platform metrics for the previous day
- **Metrics**:
  - Jobs created
  - Jobs completed
  - New drivers registered
  - Total revenue

### Manual Job Execution

```csharp
// In a controller or service
using Hangfire;

// Queue a one-time job
BackgroundJob.Enqueue<DocumentExpiryReminderJob>(job => job.SendExpiryRemindersAsync());

// Schedule a delayed job
BackgroundJob.Schedule<WeeklyDriverPayoutJob>(
    job => job.ProcessWeeklyPayoutsAsync(),
    TimeSpan.FromHours(2)
);

// Add/update recurring job
RecurringJob.AddOrUpdate<DailyReportJob>(
    "custom-report",
    job => job.GenerateDailyReportAsync(),
    "0 0 * * *"
);
```

### Creating Custom Background Jobs

```csharp
public class CustomNotificationJob
{
    private readonly ApplicationDbContext _context;
    private readonly IRealtimeNotificationService _realtimeService;

    public CustomNotificationJob(
        ApplicationDbContext context,
        IRealtimeNotificationService realtimeService)
    {
        _context = context;
        _realtimeService = realtimeService;
    }

    public async Task SendCustomNotificationsAsync()
    {
        // Your logic here
        var users = await _context.Users.Where(u => u.IsActive).ToListAsync();

        foreach (var user in users)
        {
            await _realtimeService.SendToUserAsync(
                user.Id,
                "custom_notification",
                new { message = "Hello!" }
            );
        }
    }
}

// Register in RecurringJobs.cs
RecurringJob.AddOrUpdate<CustomNotificationJob>(
    "custom-notification",
    job => job.SendCustomNotificationsAsync(),
    "0 12 * * *" // Daily at noon
);
```

---

## Email Service

The email service provides professional HTML email templates for common platform events.

### Using Email Service in Controllers

```csharp
using BeC.OpenId.Connect.Infrastructure.Email;

public class JobsController : ControllerBase
{
    private readonly IEmailService _emailService;

    public JobsController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateJob(CreateJobRequest request)
    {
        // ... create job logic

        // Send confirmation email to customer
        await _emailService.SendJobConfirmationEmailAsync(
            to: customerEmail,
            customerName: customer.FullName,
            jobNumber: job.JobNumber,
            scheduledDate: job.ScheduledDate,
            pickupAddress: job.PickupAddress,
            deliveryAddress: job.DeliveryAddress
        );

        return Ok(job);
    }
}
```

### Available Email Templates

#### 1. Job Confirmation
```csharp
await _emailService.SendJobConfirmationEmailAsync(
    "customer@example.com",
    "John Doe",
    "JOB-2025-001",
    DateTime.Now.AddDays(3),
    "123 Pickup St, City",
    "456 Delivery Ave, City"
);
```

#### 2. Job Assignment (to Driver)
```csharp
await _emailService.SendJobAssignmentEmailAsync(
    "driver@example.com",
    "Mike Driver",
    "JOB-2025-001",
    DateTime.Now.AddDays(3),
    "123 Pickup St, City",
    "456 Delivery Ave, City"
);
```

#### 3. Job Completion
```csharp
await _emailService.SendJobCompletionEmailAsync(
    "customer@example.com",
    "John Doe",
    "JOB-2025-001",
    150.00m
);
```

#### 4. Payment Receipt
```csharp
await _emailService.SendPaymentReceiptEmailAsync(
    "customer@example.com",
    "John Doe",
    "JOB-2025-001",
    150.00m,
    "Credit Card",
    "ch_1234567890"
);
```

#### 5. Document Status
```csharp
// Verified
await _emailService.SendDocumentStatusEmailAsync(
    "driver@example.com",
    "Mike Driver",
    "drivers_license",
    "verified"
);

// Rejected
await _emailService.SendDocumentStatusEmailAsync(
    "driver@example.com",
    "Mike Driver",
    "insurance",
    "rejected",
    "Document image is unclear. Please upload a clearer photo."
);
```

#### 6. Payout Notification
```csharp
await _emailService.SendPayoutNotificationEmailAsync(
    "driver@example.com",
    "Mike Driver",
    450.00m,
    "Jan 1 - Jan 7, 2025"
);
```

#### 7. Welcome Email
```csharp
await _emailService.SendWelcomeEmailAsync(
    "newuser@example.com",
    "Jane Smith",
    "Driver"
);
```

#### 8. Password Reset
```csharp
await _emailService.SendPasswordResetEmailAsync(
    "user@example.com",
    "John Doe",
    "https://your-app.com/reset-password?token=abc123"
);
```

### Sending Custom Emails
```csharp
await _emailService.SendEmailAsync(
    to: "recipient@example.com",
    subject: "Custom Subject",
    body: "<html><body><h1>Custom HTML Content</h1></body></html>",
    isHtml: true
);
```

---

## Configuration

### appsettings.json

Add the following configuration sections:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=...;..."
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "UseSsl": "true",
    "FromAddress": "noreply@becmoving.com",
    "FromName": "BeC Moving Services",
    "Username": "your-email@gmail.com",
    "Password": "your-app-password"
  }
}
```

### Email Configuration for Development

For local development, you can use:
- **smtp4dev**: Local SMTP server for testing
  ```bash
  docker run -p 3000:80 -p 2525:25 rnwood/smtp4dev
  ```
  Then set `SmtpHost: "localhost"` and `SmtpPort: 2525`

- **Ethereal Email**: Free email testing service
  - Visit https://ethereal.email to get test credentials

### Production Email Providers

Recommended providers:
- **SendGrid**: Up to 100 emails/day free
- **AWS SES**: Very low cost
- **Mailgun**: Good deliverability
- **Gmail SMTP**: Use app-specific password

---

## Integration Examples

### Complete Workflow Example

When a customer creates a job:

```csharp
[HttpPost("jobs")]
public async Task<IActionResult> CreateJob(CreateJobRequest request)
{
    // 1. Create job in database
    var job = new Job { /* ... */ };
    await _context.Jobs.AddAsync(job);
    await _context.SaveChangesAsync();

    // 2. Send email confirmation
    await _emailService.SendJobConfirmationEmailAsync(
        customer.Email,
        customer.FullName,
        job.JobNumber,
        job.ScheduledDate,
        job.PickupAddress,
        job.DeliveryAddress
    );

    // 3. Send real-time notification to available drivers
    await _realtimeNotificationService.SendToRoleAsync(
        "Driver",
        "job_created",
        new {
            jobId = job.Id,
            jobNumber = job.JobNumber,
            pickupAddress = job.PickupAddress,
            scheduledDate = job.ScheduledDate
        }
    );

    // 4. Schedule reminder 24 hours before job
    BackgroundJob.Schedule(
        () => SendJobReminderAsync(job.Id),
        job.ScheduledDate.AddHours(-24)
    );

    return Ok(job);
}
```

When a driver accepts a job:

```csharp
[HttpPost("jobs/{jobId}/accept")]
public async Task<IActionResult> AcceptJob(Guid jobId)
{
    var job = await _context.Jobs
        .Include(j => j.Customer)
        .FirstOrDefaultAsync(j => j.Id == jobId);

    job.AssignedDriverId = currentDriverId;
    job.Status = "assigned";
    await _context.SaveChangesAsync();

    // Send email to customer
    await _emailService.SendEmailAsync(
        job.Customer.Email,
        "Driver Assigned to Your Job",
        $"Good news! A driver has been assigned to job {job.JobNumber}"
    );

    // Send real-time notification to customer
    await _realtimeNotificationService.SendToUserAsync(
        job.CustomerId,
        "driver_assigned",
        new {
            jobId = job.Id,
            driverName = currentDriver.FullName,
            driverPhoto = currentDriver.PhotoUrl
        }
    );

    return Ok();
}
```

---

## Testing

### Test SignalR Connection
```bash
# Install signalr-client-cli
npm install -g @microsoft/signalr-client

# Connect to hub
signalr-client connect https://your-api.com/hubs/notifications --access-token YOUR_TOKEN
```

### Test Email Sending
```csharp
// In a test controller
[HttpGet("test-email")]
public async Task<IActionResult> TestEmail()
{
    await _emailService.SendWelcomeEmailAsync(
        "test@example.com",
        "Test User",
        "Customer"
    );
    return Ok("Email sent!");
}
```

### Monitor Background Jobs
Visit `/hangfire` dashboard to:
- View job execution history
- See failed jobs and retry them
- Monitor job performance
- Manually trigger jobs

---

## Next Steps

Recommended features to add next:
1. **SMS Notifications** (Twilio integration)
2. **Push Notifications** (Firebase Cloud Messaging)
3. **Google Maps Integration** for real-time driver tracking
4. **Chat System** using SignalR for customer-driver communication
5. **Rate Limiting** to prevent API abuse

---

## Support

For questions or issues, contact: bchelinje@gmail.com
