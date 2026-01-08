import { Injectable } from '@angular/core';

interface TokenPayload {
  exp?: number;
  sub?: string;
  email?: string;
  name?: string;
  role?: string | string[];
  [key: string]: any;
}

@Injectable({
  providedIn: 'root'
})
export class TokenService {
  private readonly ACCESS_TOKEN_KEY = 'access_token';
  private readonly REFRESH_TOKEN_KEY = 'refresh_token';

  /**
   * Store access token
   */
  setAccessToken(token: string): void {
    if (token) {
      localStorage.setItem(this.ACCESS_TOKEN_KEY, token);
    }
  }

  /**
   * Get access token
   */
  getAccessToken(): string | null {
    return localStorage.getItem(this.ACCESS_TOKEN_KEY);
  }

  /**
   * Store refresh token
   */
  setRefreshToken(token: string): void {
    if (token) {
      localStorage.setItem(this.REFRESH_TOKEN_KEY, token);
    }
  }

  /**
   * Get refresh token
   */
  getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  /**
   * Check if token is expired
   * For JWE tokens, we can't decode them on the client side
   * So we'll just check if the token exists and let the server validate
   */
  isTokenExpired(token?: string): boolean {
    const tokenToCheck = token ?? this.getAccessToken();

    if (!tokenToCheck) {
      return true;
    }

    // Check if it's a JWE token (5 parts) or JWT (3 parts)
    const parts = tokenToCheck.split('.');

    if (parts.length === 5) {
      // JWE token - we cannot decode it client-side
      // Just return false and let the server validate on each request
      console.log('JWE token detected - client-side expiration check not supported');
      return false; // Assume valid, server will reject if expired
    }

    if (parts.length === 3) {
      // Standard JWT - decode and check expiration
      try {
        const payload = this.getTokenPayload(tokenToCheck);
        if (!payload || !payload.exp) {
          console.warn('Token has no expiration');
          return true;
        }

        const expirationDate = new Date(payload.exp * 1000);
        const now = new Date();

        return expirationDate <= now;
      } catch (error) {
        console.error('Error checking token expiration:', error);
        return true;
      }
    }

    // Unknown format
    console.error('Unknown token format');
    return true;
  }

  /**
   * Decode JWT token payload
   * Note: This only works for standard JWT (3 parts), not JWE (5 parts)
   */
  getTokenPayload(token: string): TokenPayload | null {
    if (!token) {
      return null;
    }

    try {
      const parts = token.split('.');

      // Check if it's JWE (5 parts) - cannot decode client-side
      if (parts.length === 5) {
        console.warn('Cannot decode JWE token client-side - token is encrypted');
        return null;
      }

      // Standard JWT (3 parts)
      if (parts.length !== 3) {
        console.error('Invalid token format: token must have 3 or 5 parts');
        return null;
      }

      // Decode the payload (second part)
      const payload = parts[1];

      // Base64Url decode
      const base64 = payload.replace(/-/g, '+').replace(/_/g, '/');
      const jsonPayload = decodeURIComponent(
        atob(base64)
          .split('')
          .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      );

      return JSON.parse(jsonPayload);
    } catch (error) {
      console.error('Error decoding token:', error);
      return null;
    }
  }

  /**
   * Get user info from token
   * For JWE tokens, this will return null since we can't decode them
   * User info should be fetched from /userinfo endpoint instead
   */
  getUserInfo(): { email: string; name?: string; roles: string[] } | null {
    const token = this.getAccessToken();
    if (!token) {
      return null;
    }

    // Check if it's a JWE token
    const parts = token.split('.');
    if (parts.length === 5) {
      console.log('JWE token - cannot extract user info client-side');
      return null; // Must use /userinfo endpoint
    }

    try {
      const payload = this.getTokenPayload(token);
      if (!payload) {
        return null;
      }

      // Extract roles (handle both single role and array)
      let roles: string[] = [];
      if (payload.role) {
        roles = Array.isArray(payload.role) ? payload.role : [payload.role];
      }

      // Try different claim names for email
      const email = payload.email ||
        payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] ||
        payload.sub ||
        '';

      // Try different claim names for name
      const name = payload.name ||
        payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ||
        payload['given_name'] ||
        '';

      return {
        email,
        name,
        roles
      };
    } catch (error) {
      console.error('Error getting user info from token:', error);
      return null;
    }
  }

  /**
   * Clear all tokens
   */
  clearTokens(): void {
    localStorage.removeItem(this.ACCESS_TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
  }

  /**
   * Check if user has valid token
   */
  hasValidToken(): boolean {
    const token = this.getAccessToken();
    return !!token && !this.isTokenExpired(token);
  }

  /**
   * Check if token is JWE (encrypted)
   */
  isJWE(token?: string): boolean {
    const tokenToCheck = token ?? this.getAccessToken();
    if (!tokenToCheck) return false;

    return tokenToCheck.split('.').length === 5;
  }
}
