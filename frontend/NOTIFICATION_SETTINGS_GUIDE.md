# 🔔 Notification Preferences & Settings Guide

## Overview

The notification system allows users to customize how they receive notifications for various events in the application. This includes email, SMS, and push notifications for jobs, payments, reviews, and more.

---

## 📍 Accessing Notification Preferences

### For All Users

Notification preferences are available in your **Settings** page:

- **Customers**: `/customer/settings` → Notifications tab
- **Drivers**: `/driver/settings` → Notifications tab
- **Admins**: `/admin/settings` → Notifications tab

Or directly at: `/notification-preferences` (if route is configured)

---

## 🎯 How It Works

### First-Time Access

When you first access the notification preferences:

1. The system checks if you have existing preferences
2. If **none exist**, it automatically creates default preferences for you
3. Default settings are:
   - **Email**: ✅ Enabled
   - **SMS**: ❌ Disabled
   - **Push**: ✅ Enabled
   - **Most notifications**: ✅ Enabled

### The "Failed to load preferences. Using defaults." Message

This message appears when:

✅ **Normal Scenarios** (Not an Error):
- First time accessing the page
- No preferences have been saved yet
- System is creating your default preferences

❌ **Actual Errors**:
- Not logged in (authentication required)
- Backend API is not running
- Network connectivity issues
- CORS configuration problems

**What to do**: If you see this message but can still use the form, just **adjust your preferences and click "Save"**. The system will create your preferences on save.

---

## ⚙️ Available Settings

### 1. **Notification Channels**

Control which methods you want to receive notifications through:

#### Email Notifications
- ✅ Receive notifications via email
- Sent to your registered email address

#### SMS Notifications
- 📱 Receive text messages for important updates
- Sent to your registered phone number
- **Note**: SMS may incur carrier charges

#### Push Notifications
- 🔔 Browser/mobile app push notifications
- Instant alerts even when app is closed

---

### 2. **Job Notifications**

Fine-grained control over job-related notifications:

| Event | Email | SMS | Push | Description |
|-------|-------|-----|------|-------------|
| **Job Assigned** | ✅ | ❌ | ✅ | When a new job is assigned to you |
| **Job Completed** | ✅ | ❌ | ✅ | When a job is marked as complete |
| **Job Cancelled** | ✅ | ✅ | ✅ | When a job is cancelled |
| **Job Rescheduled** | ✅ | ❌ | ❌ | When a job is rescheduled |

**Use Cases**:
- **Drivers**: Get notified when jobs are assigned or cancelled
- **Customers**: Track job status and completion
- **Disable non-urgent**: Turn off "Completed" notifications if you don't need instant updates

---

### 3. **Payment Notifications**

Stay informed about financial transactions:

| Event | Email | SMS | Push | Description |
|-------|-------|-----|------|-------------|
| **Payment Received** | ✅ | ❌ | ✅ | When payment is received from customer |
| **Payout Processed** | ✅ | ❌ | ✅ | When earnings are paid out to driver |
| **Refund Processed** | ✅ | ❌ | ❌ | When a refund is issued |

**Recommended Settings**:
- Keep email enabled for records
- Enable SMS for high-value transactions
- Use push for real-time updates

---

### 4. **Review Notifications**

Manage review and feedback notifications:

| Event | Email | SMS | Push | Description |
|-------|-------|-----|------|-------------|
| **Review Received** | ✅ | ❌ | ✅ | When someone leaves you a review |
| **Review Response** | ✅ | ❌ | ❌ | When someone responds to your review |

---

### 5. **System Notifications**

Critical system alerts and updates:

| Event | Email | SMS | Push | Description |
|-------|-------|-----|------|-------------|
| **System Alerts** | ✅ | ✅ | ✅ | Important system announcements |
| **Account Updates** | ✅ | ❌ | ✅ | Account security and changes |
| **Promotional** | ❌ | ❌ | ❌ | Marketing and promotional content |

**Note**: System alerts often cannot be fully disabled for security reasons.

---

### 6. **Email Digest**

Combine multiple notifications into a single email:

- ✅ **Enable Email Digest**: Get summary emails instead of individual ones
- **Frequency Options**:
  - Daily (once per day)
  - Weekly (once per week)
  - Monthly (once per month)
  - Never (disabled)

**When to Use**:
- Reduce email clutter
- Get overview of all activities
- Perfect for non-urgent updates

---

### 7. **Quiet Hours**

Set times when you don't want to be disturbed:

- ✅ **Enable Quiet Hours**: Prevent non-urgent notifications
- **Start Time**: When quiet hours begin (e.g., 22:00)
- **End Time**: When quiet hours end (e.g., 08:00)

**How it Works**:
- **During quiet hours**:
  - ❌ SMS notifications are suppressed
  - ❌ Push notifications are suppressed
  - ✅ Emails are still sent (you can check later)
  - ✅ Urgent/critical alerts may still come through

**Example Setup**:
```
Start Time: 10:00 PM (22:00)
End Time: 8:00 AM (08:00)
```
Perfect for uninterrupted sleep!

---

## 💡 Recommended Settings by Role

### For Customers

**Active Users** (Track everything):
```
Email: ✅ All enabled
SMS: ✅ Job Assigned, Job Cancelled, Payment Received
Push: ✅ All enabled
Quiet Hours: 22:00 - 08:00
```

**Casual Users** (Less notifications):
```
Email: ✅ Enabled (with Daily Digest)
SMS: ✅ Only Job Cancelled
Push: ❌ Disabled
```

### For Drivers

**Full-time Drivers**:
```
Email: ✅ All job and payment notifications
SMS: ✅ Job Assigned, Job Cancelled
Push: ✅ All job notifications
Quiet Hours: Don't enable (need to respond quickly)
```

**Part-time Drivers**:
```
Email: ✅ All enabled
SMS: ✅ Job Assigned only
Push: ✅ Job Assigned, Payment Received
Quiet Hours: Enable during work hours at day job
```

### For Admins

**System Administrators**:
```
Email: ✅ All enabled
SMS: ✅ System Alerts only
Push: ✅ System Alerts, Important issues
Quiet Hours: Personalize based on support hours
```

---

## 🔧 How to Configure

### Step 1: Access Settings
Navigate to your settings page based on your role.

### Step 2: Adjust Preferences
- Toggle notification channels (Email/SMS/Push)
- Enable/disable specific notification types
- Configure quiet hours if needed
- Set up email digest if desired

### Step 3: Save Changes
Click **"Save Preferences"** button at the bottom.

### Step 4: Verify
You should see: ✅ **"Preferences saved successfully!"**

---

## 🚨 Troubleshooting

### "Failed to load preferences. Using defaults."

**Solutions**:

1. **Make sure you're logged in**
   - Check if you see your name in the navigation
   - Try logging out and back in

2. **Save your preferences anyway**
   - The form still works with defaults
   - Adjust settings and click "Save Preferences"
   - Your preferences will be created

3. **Check browser console** (F12 → Console tab)
   - Look for red error messages
   - Common issues:
     - `401 Unauthorized` → Not logged in
     - `CORS error` → Backend configuration issue
     - `Network error` → Backend not running

4. **Verify backend is running**
   ```bash
   # Should respond with status
   curl https://localhost:7172/api/Notifications/preferences \
     -H "Authorization: Bearer YOUR_TOKEN"
   ```

### Notifications Not Being Sent

**Check These**:

1. **Preferences Are Enabled**
   - Verify the channel (Email/SMS/Push) is ON
   - Check the specific notification type is enabled

2. **Contact Information Is Set**
   - Email: Must have verified email in profile
   - SMS: Must have phone number in profile
   - Push: Must allow browser notifications

3. **Backend Implementation**
   - Email/SMS/Push require additional setup
   - Check backend logs for "would be sent" messages
   - Configure SMTP for emails, Twilio for SMS

4. **Quiet Hours**
   - Check if current time is within quiet hours
   - Quiet hours block SMS and Push (not Email)

### Can't Save Preferences

**Common Causes**:

1. **Not Authenticated**
   - Log out and log back in
   - Check token expiration

2. **Validation Errors**
   - Check browser console
   - Ensure all required fields are valid

3. **Backend Issues**
   - Check backend is running
   - Verify API endpoint `/api/Notifications/preferences` (PUT) works

---

## 🔐 Privacy & Data

### What We Store
- Your notification preference selections
- Channel enablement status
- Quiet hours configuration
- Email digest frequency

### What We DON'T Store
- Notification content (deleted after delivery)
- Email/SMS provider responses
- Push notification tokens (managed by browser)

### Your Rights (GDPR)
- ✅ View all your notification data
- ✅ Export your preferences
- ✅ Delete your notification history
- ✅ Opt-out of any notification type

---

## 📊 Backend Implementation

### For Developers

The notification system is implemented with:

**Frontend**:
- Component: `notification-preferences.component.ts`
- Service: `NotificationsService` (auto-generated from API)
- Model: `NotificationPreferences` interface

**Backend**:
- Controller: `NotificationsController.cs`
- Service: `NotificationService.cs`
- Endpoints:
  - `GET /api/Notifications/preferences` - Get user preferences
  - `PUT /api/Notifications/preferences` - Update preferences
  - `GET /api/Notifications/me` - Get user notifications
  - `PATCH /api/Notifications/{id}/read` - Mark as read

**Database**:
- Table: `NotificationPreferences`
- Auto-creates default preferences on first access
- Stores user-specific settings

**Integrations** (Require Setup):
- **Email**: SendGrid, AWS SES, SMTP
- **SMS**: Twilio, AWS SNS
- **Push**: Firebase Cloud Messaging, OneSignal

---

## 🎓 Best Practices

### For Users

1. **Start with Defaults**
   - Use the system defaults initially
   - Adjust based on your actual needs

2. **Don't Over-Notify**
   - Too many notifications = notification fatigue
   - Disable non-critical alerts

3. **Use Quiet Hours**
   - Maintain work-life balance
   - Sleep without interruptions

4. **Try Email Digest**
   - Reduces inbox clutter
   - Good for non-urgent updates

### For Admins

1. **Monitor Notification Volume**
   - Check `/api/admin/notifications/statistics`
   - Watch for unusual spikes

2. **Respect User Preferences**
   - System honors user settings
   - Only override for critical alerts

3. **Clean Up Expired Notifications**
   - Use `/api/admin/notifications/cleanup`
   - Run periodically (weekly/monthly)

4. **Configure Delivery Services**
   - Set up SMTP for production emails
   - Configure Twilio for SMS
   - Enable push notifications

---

## 📞 Support

### Need Help?

1. Check this guide first
2. View browser console for errors
3. Contact your system administrator
4. Check backend logs for delivery issues

### Reporting Issues

Include:
- Your role (Customer/Driver/Admin)
- What you were trying to do
- Error message (screenshot helpful)
- Browser console errors (F12 → Console)

---

**Last Updated**: December 2025
**Version**: 1.0
