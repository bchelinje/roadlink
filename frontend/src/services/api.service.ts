import { apiClient } from '@/lib/api-client';
import {
  LoginRequest,
  AuthResponse,
  DriverRegistrationRequest,
  CustomerRegistrationRequest,
  Driver,
  Customer,
  PendingDriversResponse,
  ApproveDriverRequest,
  RejectDriverRequest,
  SuspendDriverRequest,
} from '@/types';

export class ApiService {
  // ==================== Authentication ====================

  static async login(credentials: LoginRequest): Promise<AuthResponse> {
    return apiClient.post<AuthResponse>('/connect/token', {
      grant_type: 'password',
      username: credentials.email,
      password: credentials.password,
      scope: 'openid profile email roles',
    });
  }

  static async refreshToken(refreshToken: string): Promise<AuthResponse> {
    return apiClient.post<AuthResponse>('/connect/token', {
      grant_type: 'refresh_token',
      refresh_token: refreshToken,
    });
  }

  static async logout(): Promise<void> {
    return apiClient.post<void>('/connect/logout');
  }

  // ==================== Registration ====================

  static async registerDriver(data: DriverRegistrationRequest): Promise<{ userId: string; message: string }> {
    return apiClient.post<{ userId: string; message: string }>('/api/registration/driver', data);
  }

  static async registerCustomer(data: CustomerRegistrationRequest): Promise<{ userId: string; message: string }> {
    return apiClient.post<{ userId: string; message: string }>('/api/registration/customer', data);
  }

  // ==================== Vetting / Approval ====================

  static async getPendingDrivers(params?: {
    page?: number;
    pageSize?: number;
    searchTerm?: string;
  }): Promise<PendingDriversResponse> {
    return apiClient.get<PendingDriversResponse>('/api/vetting/pending', params);
  }

  static async getDriverById(driverId: string): Promise<Driver> {
    return apiClient.get<Driver>(`/api/vetting/driver/${driverId}`);
  }

  static async approveDriver(driverId: string, data?: ApproveDriverRequest): Promise<void> {
    return apiClient.post<void>(`/api/vetting/approve/${driverId}`, data);
  }

  static async rejectDriver(driverId: string, data: RejectDriverRequest): Promise<void> {
    return apiClient.post<void>(`/api/vetting/reject/${driverId}`, data);
  }

  static async suspendDriver(driverId: string, data: SuspendDriverRequest): Promise<void> {
    return apiClient.post<void>(`/api/vetting/suspend/${driverId}`, data);
  }

  static async bulkApproveDrivers(driverIds: string[]): Promise<void> {
    return apiClient.post<void>('/api/vetting/bulk-approve', { driverIds });
  }

  static async bulkRejectDrivers(driverIds: string[], reason: string): Promise<void> {
    return apiClient.post<void>('/api/vetting/bulk-reject', { driverIds, reason });
  }

  // ==================== Driver Management ====================

  static async getAllDrivers(params?: {
    page?: number;
    pageSize?: number;
    status?: string;
    searchTerm?: string;
  }): Promise<{ drivers: Driver[]; totalCount: number }> {
    return apiClient.get<{ drivers: Driver[]; totalCount: number }>('/api/drivers', params);
  }

  static async getApprovedDrivers(params?: {
    page?: number;
    pageSize?: number;
  }): Promise<{ drivers: Driver[]; totalCount: number }> {
    return apiClient.get<{ drivers: Driver[]; totalCount: number }>('/api/drivers/approved', params);
  }

  static async getRejectedDrivers(params?: {
    page?: number;
    pageSize?: number;
  }): Promise<{ drivers: Driver[]; totalCount: number }> {
    return apiClient.get<{ drivers: Driver[]; totalCount: number }>('/api/drivers/rejected', params);
  }

  static async getSuspendedDrivers(params?: {
    page?: number;
    pageSize?: number;
  }): Promise<{ drivers: Driver[]; totalCount: number }> {
    return apiClient.get<{ drivers: Driver[]; totalCount: number }>('/api/drivers/suspended', params);
  }

  // ==================== Customer Management ====================

  static async getAllCustomers(params?: {
    page?: number;
    pageSize?: number;
    searchTerm?: string;
  }): Promise<{ customers: Customer[]; totalCount: number }> {
    return apiClient.get<{ customers: Customer[]; totalCount: number }>('/api/customers', params);
  }

  static async getCustomerById(customerId: string): Promise<Customer> {
    return apiClient.get<Customer>(`/api/customers/${customerId}`);
  }

  // ==================== Document Upload ====================

  static async uploadDriverDocument(
    driverId: string,
    documentType: string,
    file: File,
    onProgress?: (progress: number) => void
  ): Promise<{ documentId: string; message: string }> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('documentType', documentType);

    return apiClient.uploadFile<{ documentId: string; message: string }>(
      `/api/drivers/${driverId}/documents`,
      file,
      onProgress
    );
  }

  static async verifyDocument(documentId: string): Promise<void> {
    return apiClient.post<void>(`/api/documents/${documentId}/verify`);
  }

  // ==================== User Profile ====================

  static async getCurrentUser(): Promise<Driver | Customer> {
    return apiClient.get<Driver | Customer>('/api/users/me');
  }

  static async updateProfile(data: Partial<Driver | Customer>): Promise<void> {
    return apiClient.put<void>('/api/users/me', data);
  }

  // ==================== Admin Statistics ====================

  static async getAdminStats(): Promise<{
    totalDrivers: number;
    pendingDrivers: number;
    approvedDrivers: number;
    rejectedDrivers: number;
    suspendedDrivers: number;
    totalCustomers: number;
  }> {
    return apiClient.get('/api/admin/statistics');
  }
}
