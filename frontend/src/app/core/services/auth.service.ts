import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';
import { TokenService } from './token.service';
import { environment } from '@environments/environment';

export interface TokenResponse {
  access_token: string;
  token_type: string;
  expires_in: number;
  refresh_token?: string;
  scope?: string;
}

export interface UserInfo {
  sub: string;
  email: string;
  name?: string;
  roles?: string[];
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokenService = inject(TokenService);
  private readonly router = inject(Router);

  private currentUserSubject = new BehaviorSubject<UserInfo | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  constructor() {
    // Check if user is already authenticated on service initialization
    // But DON'T make HTTP calls here - it causes circular dependency
    this.checkAuthenticationStatus();
  }

  /**
   * Check authentication status on app initialization
   * Loads user info synchronously from JWT or schedules async load from JWE
   */
  private checkAuthenticationStatus(): void {
    const token = this.tokenService.getAccessToken();
    if (token && !this.tokenService.isTokenExpired(token)) {
      this.isAuthenticatedSubject.next(true);

      // Try to load user info synchronously from JWT token
      if (!this.tokenService.isJWE(token)) {
        // Standard JWT - can decode immediately
        try {
          const payload = this.tokenService.getTokenPayload(token);
          if (payload) {
            const email = payload.email || payload['preferred_username'] || '';
            let roles: string[] = [];
            if (payload.role) {
              roles = Array.isArray(payload.role) ? payload.role : [payload.role];
            }

            const userInfo: UserInfo = {
              sub: payload.sub || '',
              email: email,
              name: payload.name || '',
              roles: roles
            };
            this.currentUserSubject.next(userInfo);
            console.log('User info loaded synchronously from JWT:', userInfo);
            return; // User info loaded, no need for async call
          }
        } catch (error) {
          console.error('Error loading user info synchronously:', error);
        }
      }

      // For JWE tokens or if JWT decode failed, load async from /userinfo endpoint
      setTimeout(() => this.loadUserInfo(), 0);
    }
  }

  /**
   * Login with email and password using Resource Owner Password flow
   */
  login(email: string, password: string): Observable<boolean> {
    const body = new URLSearchParams();
    body.set('grant_type', 'password');
    body.set('username', email);
    body.set('password', password);
    body.set('scope', environment.auth.scope);
    body.set('client_id', environment.auth.clientId);

    if (environment.auth.clientSecret) {
      body.set('client_secret', environment.auth.clientSecret);
    }

    const headers = new HttpHeaders({
      'Content-Type': 'application/x-www-form-urlencoded'
    });

    return this.http.post<TokenResponse>(
      environment.auth.tokenEndpoint,
      body.toString(),
      { headers }
    ).pipe(
      tap(response => {
        console.log('Login successful, storing tokens');
        this.tokenService.setAccessToken(response.access_token);
        if (response.refresh_token) {
          this.tokenService.setRefreshToken(response.refresh_token);
        }
        this.setAuthenticatedUser();
      }),
      map(() => true),
      catchError(error => {
        console.error('Login failed:', error);
        return of(false);
      })
    );
  }

  /**
   * Set user as authenticated and load user info
   */
  private setAuthenticatedUser(): void {
    this.isAuthenticatedSubject.next(true);
    this.loadUserInfo();
  }

  /**
   * Load user information from token or userinfo endpoint
   */
  private loadUserInfo(): void {
    const token = this.tokenService.getAccessToken();
    if (!token) {
      return;
    }

    // Check if token is JWE (encrypted) - if so, must use /userinfo endpoint
    if (this.tokenService.isJWE(token)) {
      console.log('JWE token detected - fetching user info from /userinfo endpoint');
      this.fetchUserInfoFromEndpoint().subscribe();
      return;
    }

    // Try to decode JWT token
    try {
      const payload = this.tokenService.getTokenPayload(token);
      if (payload) {
        const email = payload.email || payload['preferred_username'] || '';

        let roles: string[] = [];
        if (payload.role) {
          roles = Array.isArray(payload.role) ? payload.role : [payload.role];
        }

        const userInfo: UserInfo = {
          sub: payload.sub || '',
          email: email,
          name: payload.name || '',
          roles: roles
        };
        this.currentUserSubject.next(userInfo);
        console.log('User info loaded from token:', userInfo);
      } else {
        // Fallback to userinfo endpoint if token decode fails
        console.log('Could not decode token, fetching from /userinfo endpoint');
        this.fetchUserInfoFromEndpoint().subscribe();
      }
    } catch (error) {
      console.error('Error loading user info:', error);
      // Fallback to userinfo endpoint
      this.fetchUserInfoFromEndpoint().subscribe();
    }
  }

  /**
   * Fetch user info from /userinfo endpoint
   * This is necessary for JWE tokens which cannot be decoded client-side
   */
  private fetchUserInfoFromEndpoint(): Observable<UserInfo | null> {
    const token = this.tokenService.getAccessToken();
    if (!token) {
      return of(null);
    }

    const headers = new HttpHeaders({
      'Authorization': `Bearer ${token}`
    });

    // Use the userinfo endpoint from your auth config
    const userinfoEndpoint = environment.auth.userinfoEndpoint ||
      `${environment.auth.authority}/connect/userinfo`;

    console.log('Fetching user info from:', userinfoEndpoint);

    return this.http.get<any>(userinfoEndpoint, { headers }).pipe(
      map(response => {
        console.log('User info response:', response);

        // Handle different claim formats
        let roles: string[] = [];
        if (response.role) {
          roles = Array.isArray(response.role) ? response.role : [response.role];
        }

        const userInfo: UserInfo = {
          sub: response.sub || '',
          email: response.email || response['preferred_username'] || '',
          name: response.name || response['given_name'] || '',
          roles: roles
        };

        this.currentUserSubject.next(userInfo);
        console.log('User info loaded from endpoint:', userInfo);
        return userInfo;
      }),
      catchError(error => {
        console.error('Error fetching user info from endpoint:', error);

        // If JWE token, we can't decode it, so just set minimal info
        if (this.tokenService.isJWE(token)) {
          const minimalUserInfo: UserInfo = {
            sub: 'unknown',
            email: 'unknown',
            roles: []
          };
          this.currentUserSubject.next(minimalUserInfo);
        }

        return of(null);
      })
    );
  }

  /**
   * Logout and clear tokens
   */
  logout(): void {
    this.tokenService.clearTokens();
    this.currentUserSubject.next(null);
    this.isAuthenticatedSubject.next(false);
  }

  /**
   * Check if user is authenticated
   */
  isAuthenticated(): boolean {
    const token = this.tokenService.getAccessToken();
    return !!token && !this.tokenService.isTokenExpired(token);
  }

  /**
   * Get current user
   */
  getCurrentUser(): UserInfo | null {
    return this.currentUserSubject.value;
  }

  /**
   * Refresh access token using refresh token
   */
  refreshToken(): Observable<boolean> {
    const refreshToken = this.tokenService.getRefreshToken();
    if (!refreshToken) {
      return of(false);
    }

    const body = new URLSearchParams();
    body.set('grant_type', 'refresh_token');
    body.set('refresh_token', refreshToken);
    body.set('client_id', environment.auth.clientId);

    if (environment.auth.clientSecret) {
      body.set('client_secret', environment.auth.clientSecret);
    }

    const headers = new HttpHeaders({
      'Content-Type': 'application/x-www-form-urlencoded'
    });

    return this.http.post<TokenResponse>(
      environment.auth.tokenEndpoint,
      body.toString(),
      { headers }
    ).pipe(
      tap(response => {
        this.tokenService.setAccessToken(response.access_token);
        if (response.refresh_token) {
          this.tokenService.setRefreshToken(response.refresh_token);
        }
      }),
      map(() => true),
      catchError(error => {
        console.error('Token refresh failed:', error);
        this.logout();
        return of(false);
      })
    );
  }

  /**
   * Check if user has a specific role
   */
  hasRole(role: string): boolean {
    const user = this.getCurrentUser();
    return user?.roles?.includes(role) ?? false;
  }

  /**
   * Check if user has any of the specified roles
   */
  hasAnyRole(roles: string[]): boolean {
    const user = this.getCurrentUser();
    return roles.some(role => user?.roles?.includes(role));
  }

  /**
   * Get the default route for the current user based on their role
   */
  getDefaultRoute(): string {
    const user = this.getCurrentUser();
    if (!user || !user.roles || user.roles.length === 0) {
      return '/customer/dashboard'; // Default to customer dashboard for users without roles
    }

    // Check for Customer role
    if (user.roles.includes('Customer')) {
      return '/customer/dashboard';
    }

    // Check for Driver role
    if (user.roles.includes('Driver')) {
      return '/driver/dashboard';
    }

    // Admin and SuperAdmin go to admin dashboard
    return '/dashboard';
  }
}
