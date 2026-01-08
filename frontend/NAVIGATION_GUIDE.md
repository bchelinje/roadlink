# Navigation Guide - BeC Admin Dashboard

This document shows you how to access every page in the application across all three portals.

## 📋 Table of Contents
- [Admin Portal](#admin-portal)
- [Customer Portal](#customer-portal)
- [Driver Portal](#driver-portal)
- [Public Pages](#public-pages)

---

## 🔧 Admin Portal

**Access:** Login as Admin or SuperAdmin user

### Navigation Menu Structure

#### Dashboard
- **Path:** `/dashboard`
- **Access:** Click "Dashboard" in sidebar
- **Description:** Main admin dashboard with system overview

#### User Management (Expandable Menu)
- **All Users**
  - Path: `/users`
  - Access: Click "User Management" → "All Users"
  - Description: List and manage all users

- **Create User**
  - Path: `/users/create`
  - Access: Click "User Management" → "Create User"
  - Description: Add new user to the system

- **User Details** (from list)
  - Path: `/users/:id`
  - Access: Click on any user in the users list

- **Edit User** (from details)
  - Path: `/users/:id/edit`
  - Access: Click "Edit" button on user details page

#### Drivers (Expandable Menu)
- **All Drivers**
  - Path: `/drivers`
  - Access: Click "Drivers" → "All Drivers"
  - Description: List and manage all drivers

- **Create Driver**
  - Path: `/drivers/create`
  - Access: Click "Drivers" → "Create Driver"
  - Description: Register new driver

- **Active Drivers** ✨ *NEW*
  - Path: `/active-drivers`
  - Access: Click "Drivers" → "Active Drivers"
  - Description: Real-time map of active drivers with location tracking

- **Driver Details** (from list)
  - Path: `/drivers/:id`
  - Access: Click on any driver in the drivers list

- **Edit Driver** (from details)
  - Path: `/drivers/:id/edit`
  - Access: Click "Edit" button on driver details page

#### Job Management (Expandable Menu)
- **All Jobs**
  - Path: `/jobs`
  - Access: Click "Job Management" → "All Jobs"
  - Description: List and manage all jobs

- **Create Job** ✨ *NEW*
  - Path: `/jobs/create`
  - Access: Click "Job Management" → "Create Job"
  - Description: Create single new job

- **Bulk Create** ✨ *NEW*
  - Path: `/jobs/bulk-create`
  - Access: Click "Job Management" → "Bulk Create"
  - Description: Create multiple jobs at once

- **Job Details** (from list)
  - Path: `/jobs/:id`
  - Access: Click on any job in the jobs list

- **Edit Job** (from details)
  - Path: `/jobs/:id/edit`
  - Access: Click "Edit" button on job details page

- **Job Stops** (from details)
  - Path: `/jobs/:id/stops`
  - Access: Click "Manage Stops" on job details page

#### Documents ✨ *NEW*
- **Path:** `/documents`
- **Access:** Click "Documents" in sidebar
- **Description:** Manage and verify driver documents

#### Pricing Rules ✨ *NEW*
- **Path:** `/pricing-rules`
- **Access:** Click "Pricing Rules" in sidebar
- **Description:** Configure pricing rules and rates

#### Roles & Permissions
- **Path:** `/roles`
- **Access:** Click "Roles & Permissions" in sidebar
- **Description:** Manage user roles and permissions
- **Sub-pages:**
  - `/roles/create` - Create new role
  - `/roles/:id` - View role details
  - `/roles/:id/edit` - Edit role

#### Activity Logs (Expandable Menu) ✨ *UPDATED*
- **Standard Logs**
  - Path: `/activity-logs`
  - Access: Click "Activity Logs" → "Standard Logs"
  - Description: View basic activity logs

- **Advanced Analytics** ✨ *NEW*
  - Path: `/activity-logs-advanced`
  - Access: Click "Activity Logs" → "Advanced Analytics"
  - Description: Advanced log analytics and insights

#### My Profile
- **Path:** `/profile`
- **Access:** Click "My Profile" in sidebar
- **Description:** Manage admin profile

#### Notifications ✨ *NEW*
- **Path:** `/notifications/preferences`
- **Access:** Click "Notifications" in sidebar
- **Description:** Configure notification preferences

#### Settings
- **Path:** `/settings`
- **Access:** Click "Settings" in sidebar
- **Description:** Application settings

#### Vehicle Maintenance (Contextual)
- **Path:** `/vehicles/:id/maintenance`
- **Access:** Navigate from vehicle details page
- **Description:** Log and view vehicle maintenance records

---

## 👥 Customer Portal

**Access:** Login as Customer user

### Navigation Menu (Top Bar)

#### Dashboard
- **Path:** `/customer/dashboard`
- **Access:** Click "Dashboard" icon
- **Description:** Customer dashboard with job statistics

#### My Jobs
- **Path:** `/customer/my-jobs`
- **Access:** Click "My Jobs" icon
- **Description:** View all your jobs with status filters
- **Sub-pages:**
  - `/customer/jobs/:id` - View job details
  - `/customer/jobs/:jobId/review` - Submit review after job completion
  - `/customer/track-driver/:jobId` - Real-time driver tracking

#### Request Service
- **Path:** `/customer/request-job`
- **Access:** Click "Request Service" icon
- **Description:** Create a new job request

#### Book & Pay ✨ *NEW*
- **Path:** `/customer/book-job`
- **Access:** Click "Book & Pay" in menu
- **Description:** Book job with instant Stripe payment

#### Job Templates ✨ *NEW*
- **Path:** `/customer/job-templates`
- **Access:** Click "Job Templates" in menu
- **Description:** Save and reuse job configurations
- **Sub-pages:**
  - `/customer/job-templates/create` - Create new template
  - `/customer/job-templates/edit/:id` - Edit existing template

#### Recurring Jobs ✨ *NEW*
- **Path:** `/customer/recurring-jobs`
- **Access:** Click "Recurring Jobs" in menu
- **Description:** Schedule recurring job deliveries
- **Sub-pages:**
  - `/customer/recurring-jobs/create` - Create recurring schedule
  - `/customer/recurring-jobs/edit/:id` - Edit recurring job

#### My Addresses ✨ *NEW*
- **Path:** `/customer/addresses`
- **Access:** Click "My Addresses" in menu
- **Description:** Manage saved delivery addresses
- **Sub-pages:**
  - `/customer/addresses/create` - Add new address

#### Favorite Drivers ✨ *NEW*
- **Path:** `/customer/favorites`
- **Access:** Click "Favorite Drivers" in menu
- **Description:** View and manage favorite drivers

#### Payment History ✨ *NEW*
- **Path:** `/customer/payments`
- **Access:** Click "Payment History" in menu
- **Description:** View all payments and receipts

#### My Reviews ✨ *NEW*
- **Path:** `/customer/my-reviews`
- **Access:** Click "My Reviews" in menu
- **Description:** View reviews you've given to drivers

#### Price Calculator ✨ *NEW*
- **Path:** `/customer/price-calculator`
- **Access:** Click "Price Calculator" in menu
- **Description:** Calculate estimated job pricing

#### Profile
- **Path:** `/customer/profile`
- **Access:** Click "Profile" icon
- **Description:** Manage customer profile

---

## 🚛 Driver Portal

**Access:** Login as Driver user

### Navigation Menu (Left Sidebar)

#### Dashboard
- **Path:** `/driver/dashboard`
- **Access:** Click "Dashboard" in sidebar
- **Description:** Driver dashboard with earnings and stats

#### Marketplace
- **Path:** `/driver/marketplace`
- **Access:** Click "Marketplace" in sidebar
- **Description:** Browse and claim available jobs

#### My Jobs
- **Path:** `/driver/jobs`
- **Access:** Click "My Jobs" in sidebar
- **Description:** View assigned and active jobs
- **Sub-pages:**
  - `/driver/jobs/:id` - View job details

#### Schedule
- **Path:** `/driver/schedule`
- **Access:** Click "Schedule" in sidebar
- **Description:** Manage availability schedule

#### My Vehicles
- **Path:** `/driver/vehicles`
- **Access:** Click "My Vehicles" in sidebar
- **Description:** Manage driver vehicles

#### My Earnings
- **Path:** `/driver/earnings`
- **Access:** Click "My Earnings" in sidebar
- **Description:** View earnings and payout history

#### My Documents ✨ *NEW*
- **Path:** `/driver/documents`
- **Access:** Click "My Documents" in sidebar
- **Description:** Upload and manage required documents (license, insurance, etc.)

#### My Reviews ✨ *NEW*
- **Path:** `/driver/reviews`
- **Access:** Click "My Reviews" in sidebar
- **Description:** View reviews from customers

#### My Profile
- **Path:** `/driver/profile`
- **Access:** Click "My Profile" in sidebar
- **Description:** Manage driver profile

---

## 🌐 Public Pages

**Access:** No authentication required

### Landing Page
- **Path:** `/`
- **Description:** Public landing page

### Public Job Booking
- **Path:** `/book`
- **Description:** Public job booking form

### Authentication Pages

#### Login
- **Path:** `/login`
- **Description:** User login page

#### Forgot Password
- **Path:** `/forgot-password`
- **Description:** Request password reset

#### Reset Password
- **Path:** `/reset-password`
- **Description:** Complete password reset with token

#### Email Verification
- **Path:** `/verify-email`
- **Description:** Confirm email address

#### Unauthorized
- **Path:** `/unauthorized`
- **Description:** Error page for unauthorized access

---

## 📊 Navigation Summary

### Admin Portal: 30+ Pages
- ✅ All pages now accessible via navigation menu
- ✨ 7 new menu items added
- 📍 Real-time driver tracking map
- 📄 Document management
- 💰 Pricing rules configuration
- 📊 Advanced analytics

### Customer Portal: 19+ Pages
- ✅ All pages now accessible via navigation menu
- ✨ 8 new menu items added
- 💳 Stripe payment integration
- 📋 Job templates
- 🔄 Recurring jobs
- ⭐ Favorite drivers

### Driver Portal: 10+ Pages
- ✅ All pages now accessible via navigation menu
- ✨ 2 new menu items added
- 📄 Document uploads
- ⭐ Customer reviews

---

## 🎯 Quick Access Tips

### For Admins:
1. **Monitor Active Drivers:** Drivers → Active Drivers
2. **Bulk Job Creation:** Job Management → Bulk Create
3. **View Analytics:** Activity Logs → Advanced Analytics
4. **Verify Documents:** Click "Documents" in sidebar

### For Customers:
1. **Quick Booking:** Click "Book & Pay" for instant job with payment
2. **Save Templates:** Use "Job Templates" to save frequent job configurations
3. **Set Recurring:** Use "Recurring Jobs" for weekly/monthly deliveries
4. **Track Driver:** Click on any active job to track in real-time

### For Drivers:
1. **Find Jobs:** Use "Marketplace" to browse available jobs
2. **Upload Docs:** Keep documents current in "My Documents"
3. **Check Reviews:** Monitor your ratings in "My Reviews"
4. **Track Earnings:** View detailed breakdown in "My Earnings"

---

## ✅ What Changed?

All previously "hidden" pages now have direct navigation links:

### Admin Portal Added:
- Create Driver link
- Active Drivers (real-time map)
- Create Job link
- Bulk Create Jobs link
- Documents management
- Pricing Rules
- Advanced Activity Logs
- Notifications preferences

### Customer Portal Added:
- Book & Pay (Stripe payment)
- Job Templates
- Recurring Jobs
- My Addresses
- Favorite Drivers
- Payment History
- My Reviews
- Price Calculator

### Driver Portal Added:
- My Documents
- My Reviews

**All 50+ application pages are now easily accessible! 🎉**
