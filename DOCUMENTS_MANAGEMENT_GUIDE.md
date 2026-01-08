# 📄 Document Management System Guide

## Overview

The document management system allows drivers to upload required documents (license, insurance, etc.) and enables admins to verify, approve, or reject them. The system tracks expiration dates and sends alerts for expiring documents.

---

## ✅ Fully Implemented Features

### **For Drivers**
- ✅ Upload documents (Driver's License, Insurance, Registration, etc.)
- ✅ View all uploaded documents
- ✅ Delete documents (if not yet verified)
- ✅ Track document status (Pending/Verified/Rejected)
- ✅ See expiry dates
- ✅ Visual status indicators

### **For Admins**
- ✅ View all pending documents for verification
- ✅ Verify/approve documents
- ✅ Reject documents with reason
- ✅ Track expiring documents
- ✅ Statistics dashboard
- ✅ Filter by expiry period (7, 14, 30, 60, 90 days)

---

## 📍 Access Points

### **Driver Document Upload**
**URL**: `/driver/documents`

**What Drivers Can Do**:
- Upload new documents
- View uploaded documents
- Check verification status
- Delete unverified documents

### **Admin Document Management**
**URL**: `/admin/documents`

**What Admins Can Do**:
- Review pending documents
- Verify or reject documents
- Monitor expiring documents
- View statistics

---

## 🚀 How to Use - Driver Side

### **1. Upload a Document**

1. Navigate to `/driver/documents`
2. Click **"Choose File"** in the upload area
3. Select your document (Image or PDF)
4. Fill in the form:
   - **Document Type** (Required): Driver's License, Insurance, Registration, etc.
   - **Expiry Date** (Optional): If the document expires
5. Click **"Upload Document"**
6. Wait for confirmation

### **2. View Your Documents**

All your uploaded documents appear as cards showing:
- Document type
- Upload date
- Expiry date (if applicable)
- **Status Badge**:
  - 🟡 **Pending** - Awaiting admin review
  - 🟢 **Verified** - Approved by admin
  - 🔴 **Rejected** - Not accepted (check rejection reason)

### **3. View a Document**

Click **"View"** on any document card to open it in a new tab.

### **4. Delete a Document**

Click **"Delete"** on the document card.

**Note**: You can only delete documents that haven't been verified yet.

---

## 🔧 How to Use - Admin Side

### **1. Access Document Management**

Navigate to `/admin/documents`

You'll see a statistics dashboard:
- **Total Documents**
- **Pending Verification** (orange)
- **Verified** (green)
- **Rejected** (red)
- **Expired** (gray)
- **Expiring Soon** (yellow)

### **2. Review Pending Documents**

Click on the **"Pending Verification"** tab to see all documents awaiting review.

For each document, you'll see:
- Document type
- Driver name
- File name
- Upload date
- Expiry date (if applicable)

**Actions Available**:
- **View Document** - Opens document in new tab for review
- **Verify** - Approve the document
- **Reject** - Reject with reason

### **3. Verify a Document**

1. Click **"View Document"** to review it
2. If acceptable, click **"Verify"**
3. Document status changes to "Verified"
4. Driver is notified (if notifications are enabled)

### **4. Reject a Document**

1. Click **"Reject"**
2. Modal appears asking for rejection reason
3. Enter detailed reason (required)
4. Click **"Reject Document"**
5. Driver will see the rejection reason

### **5. Monitor Expiring Documents**

Click on the **"Expiring Soon"** tab to see documents nearing expiration.

**Filter Options**:
- 7 days
- 14 days
- 30 days (default)
- 60 days
- 90 days

Each expiring document shows:
- Days until expiry
- Driver contact information
- Color-coded urgency:
  - 🔴 **< 7 days** - Critical
  - 🟠 **7-14 days** - Warning
  - 🟡 **15-30 days** - Notice

**Proactive Actions**:
- Contact driver to request renewal
- Send reminder notifications
- Disable driver if critical documents expire

---

## 📋 Document Types Supported

### **Standard Types**:
1. **Driver's License** - Required for all drivers
2. **Insurance** - Vehicle insurance certificate
3. **Vehicle Registration** - Vehicle registration documents
4. **Vehicle Inspection** - Safety inspection certificate
5. **Other** - Any other required documents

### **Accepted File Formats**:
- Images: JPG, JPEG, PNG, GIF
- Documents: PDF

### **File Size Limits**:
Check backend configuration (typically 5-10 MB)

---

## 🔔 Notification Integration

The document system integrates with the notification system:

### **Automatic Notifications** (if configured):

**For Drivers**:
- Document verified
- Document rejected (with reason)
- Document expiring soon (30 days, 7 days, 1 day)
- Document expired

**For Admins**:
- New document uploaded (pending review)
- Multiple documents expiring soon

---

## 🔐 Security & Permissions

### **Driver Permissions**:
- ✅ Upload own documents
- ✅ View own documents
- ✅ Delete own unverified documents
- ❌ Cannot view other drivers' documents
- ❌ Cannot verify/reject documents
- ❌ Cannot delete verified documents

### **Admin Permissions**:
- ✅ View all documents
- ✅ Verify/reject any document
- ✅ View statistics
- ✅ Monitor expiring documents
- ✅ Access all driver documents

---

## 📊 Backend API Endpoints

### **Driver Endpoints**:
```
GET    /api/drivers/me/documents          - Get my documents
POST   /api/drivers/me/documents          - Upload document
DELETE /api/drivers/me/documents/{id}     - Delete my document
```

### **Admin Endpoints**:
```
GET  /api/documents/pending               - Get pending documents
GET  /api/documents/expiring              - Get expiring documents
GET  /api/documents/statistics            - Get statistics
POST /api/documents/{id}/verify           - Verify document
POST /api/documents/{id}/reject           - Reject document
GET  /api/documents/{id}                  - Get document details
```

---

## 🗂️ Document Storage

### **File Storage Location**:
Documents are stored on the server at:
```
/uploads/documents/{driverId}/{documentId}.{extension}
```

### **Database Records**:
- Document metadata (type, upload date, expiry, status)
- File path/URL
- Driver association
- Verification details (who verified, when, reason if rejected)

### **Backup Recommendations**:
- Regular backups of `/uploads` directory
- Database backup includes document metadata
- Consider cloud storage (AWS S3, Azure Blob, etc.) for production

---

## ⚙️ Configuration

### **Backend Configuration** (appsettings.json):

```json
{
  "FileUpload": {
    "MaxFileSizeInMB": 10,
    "AllowedExtensions": [".jpg", ".jpeg", ".png", ".pdf"],
    "UploadPath": "uploads/documents"
  },
  "DocumentExpiry": {
    "WarningDays": 30,
    "CriticalDays": 7,
    "SendExpiryNotifications": true
  }
}
```

### **Frontend Routes**:
- Driver: `/driver/documents`
- Admin: `/admin/documents`

---

## 🐛 Troubleshooting

### **Upload Fails**

**Possible Causes**:
1. File too large → Check file size limits
2. Invalid file type → Only images and PDFs allowed
3. Not logged in → Ensure authenticated
4. Not a driver → Must have Driver role

**Solutions**:
- Compress large files
- Convert to PDF if needed
- Re-login to refresh token
- Check user roles

### **Can't Delete Document**

**Reason**: Documents can only be deleted if:
- Status is "Pending" (not yet verified)
- You are the owner
- You have Driver role

**Verified documents cannot be deleted** to maintain audit trail.

### **Document Not Showing**

**Check**:
1. Refresh the page
2. Check browser console for errors
3. Verify backend is running
4. Check API response in Network tab

### **Admin Can't See Pending Documents**

**Check**:
1. User has Admin or SuperAdmin role
2. Documents exist with "Pending" status
3. Backend API is accessible
4. Check browser console for errors

---

## 📈 Best Practices

### **For Drivers**:
1. ✅ Upload clear, readable documents
2. ✅ Set correct expiry dates
3. ✅ Upload documents before they expire
4. ✅ Check verification status regularly
5. ✅ Re-upload if rejected (with corrections)

### **For Admins**:
1. ✅ Review documents within 24-48 hours
2. ✅ Provide clear rejection reasons
3. ✅ Monitor expiring documents weekly
4. ✅ Contact drivers proactively for renewals
5. ✅ Keep statistics dashboard visible

---

## 🔄 Workflow Example

### **Standard Document Verification Flow**:

1. **Driver uploads document**
   - Selects file and type
   - Sets expiry date
   - Clicks upload

2. **System stores document**
   - Saves file to storage
   - Creates database record
   - Sets status: "Pending"
   - Notifies admin (optional)

3. **Admin reviews document**
   - Opens document management
   - Views pending documents
   - Opens document for review

4. **Admin decides**:

   **Option A: Verify**
   - Clicks "Verify"
   - Status → "Verified"
   - Driver notified (optional)

   **Option B: Reject**
   - Clicks "Reject"
   - Enters reason
   - Status → "Rejected"
   - Driver notified with reason

5. **Driver sees result**
   - Status badge updates
   - Can re-upload if rejected
   - Document now in system if verified

---

## 📅 Expiry Management

### **How Expiry Tracking Works**:

1. **Driver sets expiry date** during upload (optional)
2. **System monitors** all documents with expiry dates
3. **Admin can filter** by days until expiry
4. **Notifications sent** (if configured):
   - 30 days before expiry
   - 7 days before expiry
   - 1 day before expiry
   - On expiry day

### **Expiry Color Codes**:
- 🔴 **Red**: Expired or < 7 days
- 🟠 **Orange**: 7-14 days
- 🟡 **Yellow**: 15-30 days
- ⚪ **White**: > 30 days

---

## 🎓 Common Scenarios

### **Scenario 1: New Driver Onboarding**

Driver needs to upload:
1. Driver's License (expires in 2 years)
2. Insurance (expires in 6 months)
3. Vehicle Registration (expires in 1 year)

**Steps**:
- Upload each document separately
- Set correct expiry dates
- Wait for admin verification
- Check status regularly

### **Scenario 2: Document About to Expire**

Driver's insurance expires in 15 days:
- System shows in "Expiring Soon"
- Admin contacts driver
- Driver renews insurance
- Uploads new document
- Old document remains (audit trail)
- New document gets verified

### **Scenario 3: Rejected Document**

Driver uploads blurry license photo:
- Admin rejects with reason: "Image too blurry, please upload clearer photo"
- Driver sees rejection
- Deletes rejected document
- Uploads new clear photo
- Admin verifies new upload

---

## 📦 Implementation Details

### **Frontend Components**:

**Driver Component**: `my-documents.component.ts`
- Location: `frontend/src/app/features/driver/documents/`
- Features: Upload, view, delete

**Admin Component**: `document-management.component.ts`
- Location: `frontend/src/app/features/admin/documents/`
- Features: Verify, reject, monitor expiring

### **Backend Services**:

**Document Service**: `DocumentService.cs`
- Location: `BeC.OpenId.Connect/Features/Documents/Services/`
- Methods: Upload, verify, reject, get pending, get expiring

**Controller**: `DocumentsController.cs`
- Location: `BeC.OpenId.Connect/Features/Documents/Controllers/`
- Endpoints: All document-related API endpoints

---

## 🚀 Future Enhancements (Optional)

### **Potential Improvements**:
- ✨ Bulk document upload
- ✨ Document templates/requirements by driver type
- ✨ OCR for automatic data extraction
- ✨ E-signature integration
- ✨ Document version history
- ✨ Automated expiry reminders
- ✨ Integration with DMV/insurance APIs
- ✨ Mobile app support

---

## 📞 Support

### **For Drivers**:
- Issues uploading? Contact support
- Document rejected? Check rejection reason
- Questions? Refer to this guide

### **For Admins**:
- Need help verifying? See review guidelines
- System issues? Check backend logs
- Questions? Contact development team

---

## ✅ Quick Checklist

### **Driver Setup**:
- [ ] Access `/driver/documents`
- [ ] Upload Driver's License with expiry
- [ ] Upload Insurance with expiry
- [ ] Upload Vehicle Registration with expiry
- [ ] Check verification status
- [ ] Set up document expiry notifications

### **Admin Setup**:
- [ ] Access `/admin/documents`
- [ ] Review statistics dashboard
- [ ] Check pending documents
- [ ] Set up expiring document monitoring
- [ ] Establish verification SLA (24-48 hours)

---

**Status**: ✅ **Fully Implemented and Production-Ready**

**Last Updated**: December 2025
