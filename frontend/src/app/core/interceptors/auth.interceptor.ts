import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { TokenService } from '../services/token.service';

/**
 * Auth interceptor to add JWT token to requests
 * IMPORTANT: Do NOT inject AuthService here - it causes circular dependency!
 * Only inject TokenService which has no HttpClient dependency
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // Inject TokenService directly (no circular dependency)
  const tokenService = inject(TokenService);

  // Get token from storage
  const token = tokenService.getAccessToken();

  // If no token or token is expired, proceed without auth header
  if (!token) {
    return next(req);
  }

  // For JWE tokens, skip expiration check and always send token
  // The backend will validate and reject if expired
  const isJWE = token.split('.').length === 5;

  if (!isJWE) {
    // For standard JWT, check if expired
    if (tokenService.isTokenExpired(token)) {
      return next(req);
    }
  }

  // Clone request and add Authorization header
  const authReq = req.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });

  return next(authReq);
};
