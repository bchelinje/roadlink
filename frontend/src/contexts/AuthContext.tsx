'use client';

import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { useRouter } from 'next/navigation';
import { ApiService } from '@/services/api.service';
import { LoginRequest, AuthResponse, UserRole, Driver, Customer } from '@/types';

interface AuthContextType {
  user: (Driver | Customer) | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (credentials: LoginRequest) => Promise<void>;
  logout: () => void;
  hasRole: (role: UserRole | UserRole[]) => boolean;
  refreshUser: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<(Driver | Customer) | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const router = useRouter();

  // Load user from localStorage on mount
  useEffect(() => {
    loadUserFromStorage();
  }, []);

  const loadUserFromStorage = async () => {
    try {
      const storedToken = localStorage.getItem('access_token');
      const storedUser = localStorage.getItem('user');

      if (storedToken && storedUser) {
        setToken(storedToken);
        setUser(JSON.parse(storedUser));

        // Optionally refresh user data from API
        try {
          const freshUser = await ApiService.getCurrentUser();
          setUser(freshUser);
          localStorage.setItem('user', JSON.stringify(freshUser));
        } catch (error) {
          console.error('Failed to refresh user data:', error);
        }
      }
    } catch (error) {
      console.error('Failed to load user from storage:', error);
      clearAuth();
    } finally {
      setIsLoading(false);
    }
  };

  const login = async (credentials: LoginRequest) => {
    try {
      setIsLoading(true);
      const authResponse = await ApiService.login(credentials);

      // Store tokens
      localStorage.setItem('access_token', authResponse.token);
      localStorage.setItem('refresh_token', authResponse.refreshToken);

      setToken(authResponse.token);

      // Fetch full user profile
      const userProfile = await ApiService.getCurrentUser();
      setUser(userProfile);
      localStorage.setItem('user', JSON.stringify(userProfile));

      // Redirect based on role
      const userRole = getUserRole(userProfile);
      redirectAfterLogin(userRole);
    } catch (error) {
      console.error('Login failed:', error);
      throw error;
    } finally {
      setIsLoading(false);
    }
  };

  const logout = () => {
    clearAuth();
    router.push('/login');
  };

  const clearAuth = () => {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    localStorage.removeItem('user');
    setToken(null);
    setUser(null);
  };

  const hasRole = (roles: UserRole | UserRole[]): boolean => {
    if (!user) return false;

    const userRole = getUserRole(user);
    if (!userRole) return false;

    if (Array.isArray(roles)) {
      return roles.includes(userRole);
    }
    return userRole === roles;
  };

  const refreshUser = async () => {
    try {
      const freshUser = await ApiService.getCurrentUser();
      setUser(freshUser);
      localStorage.setItem('user', JSON.stringify(freshUser));
    } catch (error) {
      console.error('Failed to refresh user:', error);
      throw error;
    }
  };

  const getUserRole = (user: Driver | Customer): UserRole | null => {
    // This is a simplified role detection
    // In a real implementation, the role should come from the JWT or user object
    if ('drivingLicense' in user) return UserRole.Driver;
    if ('companyName' in user) return UserRole.Customer;
    return null;
  };

  const redirectAfterLogin = (role: UserRole | null) => {
    switch (role) {
      case UserRole.SuperAdmin:
      case UserRole.Admin:
        router.push('/admin/dashboard');
        break;
      case UserRole.Driver:
        router.push('/driver/dashboard');
        break;
      case UserRole.Customer:
        router.push('/customer/dashboard');
        break;
      default:
        router.push('/');
    }
  };

  const value: AuthContextType = {
    user,
    token,
    isAuthenticated: !!token && !!user,
    isLoading,
    login,
    logout,
    hasRole,
    refreshUser,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
