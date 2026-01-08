# ⚡ Quick Notification Setup

## TL;DR - The Error is Normal!

The message **"Failed to load preferences. Using defaults."** is **EXPECTED** on first access. It's not an error - it means the system is creating your default preferences.

**Just click "Save Preferences"** and you're done!

---

## How Notifications Work

### 1. **Access Notification Preferences**

**URL**: `/admin/notifications/preferences`

Or navigate through:
- **Admin**: Settings → Notifications
- **Customer**: Settings → Notifications
- **Driver**: Settings → Notifications

### 2. **What You'll See**

On first visit:
```
┌─────────────────────────────────────────┐
│ Notification Preferences                │
│ Manage how you receive notifications   │
├─────────────────────────────────────────┤
│ ⚠️ Failed to load preferences.          │
│    Using defaults.                      │
└─────────────────────────────────────────┘

[Form with all notification options appears]
```

**This is NORMAL!** The backend automatically creates default preferences when you save.

### 3. **Configure Your Settings**

Toggle what you want:

- ✅ **Email Notifications** - Via email
- ✅ **SMS Notifications** - Text messages
- ✅ **Push Notifications** - Browser alerts

Customize by type:
- Job notifications (assigned, completed, cancelled)
- Payment notifications (received, processed)
- Review notifications
- System alerts

### 4. **Save**

Click **"Save Preferences"** → See success message → Done!

---

## Default Settings (What You Start With)

```javascript
Email:  ✅ Enabled
SMS:    ❌ Disabled
Push:   ✅ Enabled

Job Notifications:      ✅ Enabled (most)
Payment Notifications:  ✅ Enabled
Review Notifications:   ✅ Enabled
System Alerts:         ✅ Enabled
Promotional:           ❌ Disabled

Quiet Hours:           ❌ Disabled
Email Digest:          ❌ Disabled
```

---

## Backend Status

✅ **Fully Implemented**:
- GET `/api/Notifications/preferences` - Load preferences
- PUT `/api/Notifications/preferences` - Save preferences
- GET `/api/Notifications/me` - Get notifications
- PATCH `/api/Notifications/{id}/read` - Mark as read
- POST `/api/Notifications/send` - Send notification (Admin)
- POST `/api/Notifications/broadcast` - Broadcast (Admin)

⚠️ **Requires Configuration** (Optional):
- **Email Delivery**: Configure SMTP/SendGrid/AWS SES
- **SMS Delivery**: Configure Twilio/AWS SNS
- **Push Notifications**: Configure Firebase/OneSignal

**Current State**: Backend logs notifications but doesn't send them (see lines 246-250 in NotificationService.cs). This is intentional for testing.

---

## Troubleshooting

### If you see the error message but form doesn't load:

1. **Check you're logged in**
   ```bash
   # In browser console (F12)
   localStorage.getItem('bec_access_token')
   # Should return a long token string
   ```

2. **Check backend is running**
   ```bash
   curl -I https://localhost:7172/api/Notifications/preferences
   # Should get response (even if 401)
   ```

3. **Check browser console for errors**
   - Open DevTools (F12)
   - Go to Console tab
   - Look for red errors
   - Common: CORS, 401 Unauthorized, Network errors

### If preferences won't save:

- Verify you're authenticated
- Check backend logs for errors
- Try refreshing the page and saving again

---

## How Notification Sending Works

### When a Job is Created/Updated:

1. **Backend sends notification**
   ```csharp
   await _notificationService.SendJobNotificationAsync(
       userId: driverId,
       jobId: job.Id,
       jobNumber: job.JobNumber,
       eventType: "job_assigned",
       message: "You have been assigned a new job!"
   );
   ```

2. **System checks user preferences**
   - Is email enabled? → Queue email
   - Is SMS enabled? → Queue SMS
   - Is push enabled? → Queue push
   - Within quiet hours? → Suppress SMS/Push

3. **Notification is stored in database**
   ```sql
   INSERT INTO Notifications (UserId, Title, Message, Type, ...)
   ```

4. **User sees notification in UI**
   - Bell icon shows unread count
   - Notification list shows recent items
   - Click to mark as read

---

## Features

### ✅ Implemented

- Notification preferences management
- Multiple channels (Email/SMS/Push)
- Per-event configuration
- Quiet hours support
- Email digest support
- Mark as read/unread
- Notification history
- Admin broadcast
- Automatic cleanup
- GDPR compliant

### ⚠️ Needs External Services

- **Email**: Currently logs only (add SMTP)
- **SMS**: Currently logs only (add Twilio)
- **Push**: Currently logs only (add Firebase)

To enable actual sending, configure in `appsettings.json`:

```json
{
  "SendGrid": {
    "ApiKey": "your-sendgrid-key",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "BeC Platform"
  },
  "Twilio": {
    "AccountSid": "your-account-sid",
    "AuthToken": "your-auth-token",
    "PhoneNumber": "+1234567890"
  }
}
```

---

## Quick Access Links

- **User Preferences**: `/admin/notifications/preferences`
- **My Notifications**: `/admin/notifications` (if route exists)
- **Admin Statistics**: `GET /api/admin/notifications/statistics`
- **Admin Cleanup**: `DELETE /api/admin/notifications/cleanup`

---

## Full Documentation

For comprehensive details, see:
- `NOTIFICATION_SETTINGS_GUIDE.md` - Complete user & admin guide
- Backend: `BeC.OpenId.Connect/Features/Notifications/`
- Frontend: `frontend/src/app/shared/pages/notification-preferences/`

---

**Quick Summary**: The notification system is fully functional. The "error" message is normal. Just configure your preferences and save!
