/**
 * Authentication Diagnostic Utility
 *
 * Paste this in your browser console to diagnose authentication issues:
 *
 * ```javascript
 * // Copy the entire checkAuthStatus function and run it in console
 * ```
 */

export function checkAuthStatus(): void {
  console.log('🔍 BeC Authentication Diagnostic Tool\n');
  console.log('=' .repeat(50));

  // Check 1: Token existence
  const token = localStorage.getItem('bec_access_token');
  console.log('\n✓ Step 1: Token Check');
  if (!token) {
    console.error('❌ NO TOKEN FOUND');
    console.log('   Solution: Log in again');
    console.log('   Command: localStorage.clear(); location.reload();');
    return;
  }
  console.log('✅ Token exists');

  // Check 2: Decode token
  console.log('\n✓ Step 2: Token Payload');
  try {
    const parts = token.split('.');
    if (parts.length !== 3) {
      console.error('❌ INVALID TOKEN FORMAT');
      console.log('   Solution: Clear storage and log in again');
      return;
    }

    const payload = JSON.parse(atob(parts[1]));
    console.log('   User ID:', payload.sub || payload.userId || 'NOT FOUND');
    console.log('   Email:', payload.email || payload.unique_name || 'NOT FOUND');
    console.log('   Roles:', payload.role || payload.roles || 'NO ROLES');
    console.log('   Issued:', payload.iat ? new Date(payload.iat * 1000).toLocaleString() : 'Unknown');
    console.log('   Expires:', payload.exp ? new Date(payload.exp * 1000).toLocaleString() : 'Unknown');

    // Check 3: Token expiration
    console.log('\n✓ Step 3: Token Expiration');
    if (payload.exp) {
      const now = Math.floor(Date.now() / 1000);
      const isExpired = payload.exp < now;

      if (isExpired) {
        const expiredAgo = Math.floor((now - payload.exp) / 60);
        console.error(`❌ TOKEN EXPIRED ${expiredAgo} minutes ago`);
        console.log('   Solution: Log in again');
        console.log('   Command: localStorage.clear(); location.reload();');
        return;
      } else {
        const expiresIn = Math.floor((payload.exp - now) / 60);
        console.log(`✅ Token valid for ${expiresIn} more minutes`);
      }
    }

    // Check 4: Role verification
    console.log('\n✓ Step 4: Role Verification');
    const roles = payload.role || payload.roles || [];
    const rolesArray = Array.isArray(roles) ? roles : [roles];

    console.log('   Your roles:', rolesArray);

    const currentPath = window.location.pathname;
    if (currentPath.includes('/driver') && !rolesArray.includes('Driver')) {
      console.error('❌ ROLE MISMATCH: You are accessing /driver routes but don\'t have Driver role');
      console.log('   Solution: Log in as a Driver user, or assign Driver role to your account');
    } else if (currentPath.includes('/customer') && !rolesArray.includes('Customer')) {
      console.error('❌ ROLE MISMATCH: You are accessing /customer routes but don\'t have Customer role');
      console.log('   Solution: Log in as a Customer user, or assign Customer role to your account');
    } else if (currentPath.includes('/admin') && !rolesArray.includes('Admin') && !rolesArray.includes('SuperAdmin')) {
      console.error('❌ ROLE MISMATCH: You are accessing /admin routes but don\'t have Admin role');
      console.log('   Solution: Log in as an Admin user');
    } else {
      console.log('✅ Role matches current route');
    }

    // Check 5: API Configuration
    console.log('\n✓ Step 5: API Configuration');
    console.log('   Current URL:', window.location.origin);
    console.log('   API should be:', 'https://localhost:7172');

    // Check 6: Test API call
    console.log('\n✓ Step 6: Testing API Connection...');
    fetch('https://localhost:7172/api/Drivers/me', {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    })
    .then(response => {
      console.log('   API Response Status:', response.status);
      if (response.status === 200) {
        console.log('✅ API call successful');
        return response.json();
      } else if (response.status === 401) {
        console.error('❌ 401 UNAUTHORIZED');
        console.log('   Possible causes:');
        console.log('   - Token is invalid or corrupted');
        console.log('   - Token signature verification failed');
        console.log('   - Wrong JWT secret on backend');
        console.log('   Solution: Log in again');
      } else if (response.status === 403) {
        console.error('❌ 403 FORBIDDEN');
        console.log('   You don\'t have permission for this resource');
        console.log('   Solution: Check your role assignment');
      } else if (response.status === 404) {
        console.error('❌ 404 NOT FOUND');
        console.log('   Endpoint doesn\'t exist or wrong URL');
        console.log('   Check: Backend is running on https://localhost:7172');
      }
      throw new Error(`Status ${response.status}`);
    })
    .then(data => {
      console.log('   Profile data:', data);
    })
    .catch(error => {
      console.error('   Error:', error.message);
    });

  } catch (e) {
    console.error('❌ ERROR DECODING TOKEN:', e);
    console.log('   Solution: Clear storage and log in again');
    console.log('   Command: localStorage.clear(); location.reload();');
  }

  // Summary
  console.log('\n' + '='.repeat(50));
  console.log('\n📋 SUMMARY');
  console.log('\nIf you saw errors above:');
  console.log('1. Token expired → Log in again');
  console.log('2. No role → Contact admin to assign role');
  console.log('3. Wrong role → Log in as correct user type');
  console.log('4. 401/403/404 → Check backend is running');
  console.log('\n💡 Quick fixes:');
  console.log('   localStorage.clear(); location.reload();  // Force re-login');
  console.log('   // Check backend: curl -k https://localhost:7172/health');
}

// Make available globally for console use
if (typeof window !== 'undefined') {
  (window as any).checkAuthStatus = checkAuthStatus;
}

// Auto-run on import in development
if (typeof window !== 'undefined' && !window.location.hostname.includes('localhost')) {
  console.log('💡 Run checkAuthStatus() in console to diagnose auth issues');
}
