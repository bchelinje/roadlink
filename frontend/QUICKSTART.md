# Quick Start Guide

Get the BeC Marketplace frontend up and running in 5 minutes!

## Prerequisites

- Node.js 18+ installed
- Backend API running on http://localhost:5000

## Setup Steps

### 1. Install Dependencies

```bash
cd frontend
npm install
```

### 2. Configure Environment

The `.env` file is already created with default settings:
```env
NEXT_PUBLIC_API_URL=http://localhost:5000
```

If your backend runs on a different port, update the URL.

### 3. Start Development Server

```bash
npm run dev
```

The app will be available at **http://localhost:3000**

## Test the Application

### 1. Register as a Driver

1. Visit http://localhost:3000
2. Click "Register as Driver"
3. Fill in the registration form (all sections required)
4. Submit the form
5. You'll be redirected to login after successful registration

### 2. Register as a Customer

1. Visit http://localhost:3000
2. Click "Register as Customer"
3. Fill in the simpler customer registration form
4. Submit and get redirected to login

### 3. Admin Approval Workflow

To test the approval workflow, you need an admin account:

#### Option A: Create Admin via Backend

Use the backend API or database to create an admin user.

#### Option B: Use Existing Admin

If you have an admin account, login at http://localhost:3000/login

Once logged in as admin:
1. You'll be redirected to `/admin/dashboard`
2. See pending driver applications
3. Click "View" to see full driver details
4. Click "Approve" to approve a driver
5. Click "Reject" to reject a driver
6. Approved drivers can now login and access their dashboard

### 4. Driver Dashboard

After a driver is approved:
1. Login as the driver
2. You'll see the driver dashboard
3. Current features:
   - View approval status
   - See total jobs and rating
   - Getting started guide

### 5. Customer Dashboard

After customer registration:
1. Login as the customer
2. You'll see the customer dashboard
3. Current features:
   - View job statistics
   - Getting started guide
   - Post job button (placeholder)

## Available Routes

| Route | Description | Access |
|-------|-------------|--------|
| `/` | Landing page | Public |
| `/login` | Login page | Public |
| `/register/driver` | Driver signup | Public |
| `/register/customer` | Customer signup | Public |
| `/admin/dashboard` | Admin approval dashboard | Admin only |
| `/driver/dashboard` | Driver dashboard | Driver only |
| `/customer/dashboard` | Customer dashboard | Customer only |
| `/unauthorized` | Unauthorized access page | Public |

## Default Test Data

You can use these sample data formats for testing:

### UK Phone Number
- Format: `07123456789` or `+447123456789`

### UK Postcode
- Format: `SW1A 1AA` or `M1 1AE`

### National Insurance Number
- Format: `AB123456C`

### Sort Code
- Format: `123456` (6 digits)

### Account Number
- Format: `12345678` (8 digits)

### License Number
- Format: `SMITH751234AB9CD`

## Troubleshooting

### "Network Error" or "No response from server"

**Problem**: Cannot connect to backend API

**Solution**:
1. Check if backend is running: `curl http://localhost:5000`
2. Verify `NEXT_PUBLIC_API_URL` in `.env`
3. Check for CORS issues in backend configuration

### "401 Unauthorized" after login

**Problem**: Token not being sent or invalid

**Solution**:
1. Clear browser localStorage
2. Re-login
3. Check browser console for errors
4. Verify JWT configuration in backend

### Form validation errors

**Problem**: Form won't submit

**Solution**:
1. Check all required fields are filled
2. Ensure formats match (phone, postcode, etc.)
3. Password must be 8+ characters
4. Passwords must match

### Page not found (404)

**Problem**: Route doesn't exist

**Solution**:
1. Restart development server: `npm run dev`
2. Clear `.next` folder: `rm -rf .next && npm run dev`

### TypeScript errors

**Problem**: Type errors in IDE

**Solution**:
1. Run type check: `npm run type-check`
2. Restart TypeScript server in your IDE
3. Check for missing imports

## Next Steps

After getting the app running:

1. **Explore the code**: Check `src/types/index.ts` for all data structures
2. **Test API calls**: Use browser DevTools Network tab to see requests
3. **Customize styling**: Edit `tailwind.config.js` for theme changes
4. **Add features**: Refer to the main README for the project structure

## Production Build

When ready to deploy:

```bash
npm run build
npm start
```

The production build will be optimized and ready to serve.

## Support

- Check the main README.md for detailed documentation
- Review backend API documentation for endpoint details
- Check browser console for client-side errors
- Check backend logs for server-side errors

Happy coding!
