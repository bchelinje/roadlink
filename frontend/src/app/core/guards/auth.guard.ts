import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '@core/services/auth.service';

/**
 * Auth Guard - Protects routes that require authentication
 * Usage in routes:
 * { path: 'protected', component: Component, canActivate: [authGuard] }
 */
export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Use synchronous check to avoid race conditions on page refresh
  if (!authService.isAuthenticated()) {
    // Not authenticated, redirect to login
    router.navigate(['/login'], {
      queryParams: { returnUrl: state.url }
    });
    return false;
  }

  // Check for role-based access if specified in route data
  const requiredRoles = route.data?.['roles'] as string[];
  if (requiredRoles && requiredRoles.length > 0) {
    const hasRequiredRole = authService.hasAnyRole(requiredRoles);
    if (!hasRequiredRole) {
      // User doesn't have required role
      router.navigate(['/unauthorized']);
      return false;
    }
  }

  return true;
};

/**
 * Role Guard - Protects routes that require specific roles
 * Usage in routes:
 * {
 *   path: 'admin',
 *   component: AdminComponent,
 *   canActivate: [roleGuard],
 *   data: { roles: ['Admin', 'SuperAdmin'] }
 * }
 */
export const roleGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const allowedRoles = route.data?.['roles'] as string[];

  if (!allowedRoles || allowedRoles.length === 0) {
    console.warn('roleGuard: No roles specified in route data');
    return true;
  }

  // Use synchronous check to avoid race conditions on page refresh
  if (!authService.isAuthenticated()) {
    router.navigate(['/login']);
    return false;
  }

  if (authService.hasAnyRole(allowedRoles)) {
    return true;
  }

  // User doesn't have required role
  router.navigate(['/unauthorized']);
  return false;
};

/**
 * Guest Guard - Prevents authenticated users from accessing certain routes
 * Usage in routes (for login/register pages):
 * { path: 'login', component: LoginComponent, canActivate: [guestGuard] }
 */
export const guestGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Use synchronous check to avoid race conditions on page refresh
  if (authService.isAuthenticated()) {
    // Already authenticated, redirect to default route based on role
    const defaultRoute = authService.getDefaultRoute();
    router.navigate([defaultRoute]);
    return false;
  }
  return true;
};
