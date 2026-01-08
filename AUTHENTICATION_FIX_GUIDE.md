# 🔒 Authentication Fix Guide - 401/404 Errors

## 🎯 Problem Summary

You're seeing these errors:
- `401 Unauthorized` - User not authenticated or wrong role
- `404 Not Found` - Endpoints don't exist (but they do!)

**Root Cause**: Authentication token issues or missing user roles.

---

## ✅ Quick Diagnosis

### **Step 1: Open Browser DevTools**
Press **F12** → Go to **Console** tab

### **Step 2: Check if You're Logged In**
```javascript
// Paste this in the console:
localStorage.getItem('bec_access_token')
```

**Results**:
- ✅ **Returns a long string** (like `eyJhbGciOiJSUzI1...`) = You have a token
- ❌ **Returns `null`** = Not logged in! Log in first.

### **Step 3: Check Your Role**
```javascript
// Paste this in the console:
const token = localStorage.getItem('bec_access_token');
if (token) {
  const payload = JSON.parse(atob(token.split('.')[1]));
  console.log('Your roles:', payload.role || payload.roles);
  console.log('User ID:', payload.sub);
  console.log('Token expires:', new Date(payload.exp * 1000));
} else {
  console.log('No token found - you are not logged in');
}
```

**Expected Output**:
```
Your roles: ["Driver"]  // or ["Customer"] or ["Admin"]
User ID: "some-guid-here"
Token expires: Tue Dec 17 2025 10:30:00
```

**If you see**:
- ❌ No token = Log in again
- ❌ Token expired = Log in again
- ❌ Wrong role = You logged in as wrong user type

---

## 🔧 Fixes

### **Fix 1: Login Again**

1. **Logout**:
   - Click your profile/logout button
   - Or run: `localStorage.clear()` in console

2. **Clear Cache**:
   - Press `Ctrl/Cmd + Shift + Delete`
   - Clear "Cached images and files" and "Cookies"

3. **Login as Correct User Type**:
   - **Driver Dashboard** → Login with Driver account
   - **Customer Dashboard** → Login with Customer account

### **Fix 2: Check User Has Correct Role**

Your user might not have the Driver/Customer role assigned.

#### **Option A: Check in Database**

```sql
-- Check user's roles
SELECT u.Email, u.UserName, r.Name as Role
FROM AspNetUsers u
JOIN AspNetUserRoles ur ON u.Id = ur.UserId
JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.Email = 'your-email@example.com';
```

**Expected**: You should see role "Driver" or "Customer"

#### **Option B: Assign Role via Admin**

If you have admin access:
1. Go to `/admin/users`
2. Find your user
3. Assign "Driver" or "Customer" role

#### **Option C: Assign Role Manually (Database)**

```sql
-- 1. Get User ID
SELECT Id, Email FROM AspNetUsers WHERE Email = 'driver@example.com';

-- 2. Get Role ID for Driver
SELECT Id, Name FROM AspNetRoles WHERE Name = 'Driver';

-- 3. Assign Role (replace GUIDs with actual values)
INSERT INTO AspNetUserRoles (UserId, RoleId)
VALUES ('USER_GUID_HERE', 'DRIVER_ROLE_GUID_HERE');
```

### **Fix 3: CORS Configuration**

If you see CORS errors in console, update backend `Program.cs`:

```csharp
// In Program.cs, find CORS configuration and ensure:
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200") // Angular default
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

// Make sure this is BEFORE app.UseAuthorization():
app.UseCors("AllowFrontend");
```

---

## 🧪 Test Your Fix

### **Test 1: Check Token is Sent**

1. Open DevTools → **Network** tab
2. Refresh the page
3. Click on any failed request (like `/api/Drivers/me`)
4. Look at **Request Headers** section

**Should see**:
```
Authorization: Bearer eyJhbGciOiJSUzI1NiIsIn...
```

**If missing**: Frontend isn't sending the token correctly.

### **Test 2: Manual API Test**

```bash
# Get your token from localStorage first
# Then test the API:

curl https://localhost:7172/api/Drivers/me \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -k

# Should return your driver profile, NOT 401
```

---

## 🐛 Still Not Working?

### **Check 1: Backend is Running**

```bash
# Check if backend is up:
curl -k https://localhost:7172/health

# Should return: "Healthy" or similar
```

### **Check 2: Check Logs**

Look at your backend console for errors like:
- `JWT token validation failed`
- `User does not have required role`
- `CORS policy blocked request`

### **Check 3: Token Configuration**

In `appsettings.json`, verify:
```json
{
  "Jwt": {
    "Secret": "your-secret-key-here",
    "Issuer": "https://localhost:7172",
    "Audience": "https://localhost:7172",
    "ExpiryMinutes": 60
  }
}
```

---

## 📝 Common Scenarios

### **Scenario 1: "I just created a driver account"**

**Issue**: New driver accounts need:
1. User account created
2. Driver role assigned
3. Driver profile record created

**Fix**:
```sql
-- Check if driver profile exists:
SELECT * FROM Drivers WHERE UserId = 'YOUR_USER_ID';

-- If empty, you need to create driver profile or use registration endpoint
```

### **Scenario 2: "It worked yesterday"**

**Issue**: Token likely expired (default 60 minutes)

**Fix**: Just log in again

### **Scenario 3: "I'm admin but can't access driver dashboard"**

**Issue**: Admin ≠ Driver. Different roles.

**Fix**: Create a separate account with Driver role, or add Driver role to admin user:
```sql
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT u.Id, r.Id
FROM AspNetUsers u, AspNetRoles r
WHERE u.Email = 'admin@example.com' AND r.Name = 'Driver';
```

---

## 🚀 Quick Command Reference

### **JavaScript Console Commands**

```javascript
// Check login status
localStorage.getItem('bec_access_token') !== null

// Decode token and see contents
const token = localStorage.getItem('bec_access_token');
JSON.parse(atob(token.split('.')[1]))

// Force logout
localStorage.clear(); location.reload();

// Check token expiry
const payload = JSON.parse(atob(localStorage.getItem('bec_access_token').split('.')[1]));
new Date(payload.exp * 1000) > new Date() ? 'Token valid' : 'Token EXPIRED'
```

### **SQL Diagnostic Queries**

```sql
-- List all users with their roles
SELECT u.Email, u.UserName, STRING_AGG(r.Name, ', ') as Roles
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
GROUP BY u.Email, u.UserName;

-- Find users without roles
SELECT u.Email, u.UserName
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
WHERE ur.UserId IS NULL;

-- Check if driver profile exists for user
SELECT u.Email, d.FirstName, d.LastName, d.Status
FROM AspNetUsers u
LEFT JOIN Drivers d ON u.Id = d.UserId
WHERE u.Email = 'driver@example.com';
```

---

## ✅ Success Checklist

- [ ] Token exists in localStorage
- [ ] Token is not expired
- [ ] Token contains correct role (Driver/Customer/Admin)
- [ ] User has corresponding profile record (Drivers table)
- [ ] Backend is running (https://localhost:7172)
- [ ] CORS is configured correctly
- [ ] Authorization header is sent with requests
- [ ] No CORS errors in browser console
- [ ] API returns 200, not 401/404

---

## 🆘 Emergency Reset

If nothing works, nuclear option:

```bash
# 1. Stop backend
# 2. Clear frontend storage
localStorage.clear();
sessionStorage.clear();

# 3. Delete cookies (DevTools → Application → Cookies → Delete all)

# 4. Restart backend

# 5. Create fresh account:
#    - Go to /register
#    - Register as Driver
#    - Verify email sent
#    - Ensure role assigned
```

---

## 📞 Need More Help?

1. **Check browser console** (F12) for error messages
2. **Check backend logs** for authentication failures
3. **Check Network tab** to see exact request/response
4. **Verify database** has user with correct role

**Most common fix**: Just log in again as the correct user type! 🎯
