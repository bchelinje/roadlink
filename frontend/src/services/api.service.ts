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
  Job,
  CreateJobDto,
  BookJobDto,
  BookJobResponse,
  UpdateJobStatusDto,
  RescheduleJobDto,
  CancelJobDto,
  JobBid,
  CreateJobBidDto,
  Payment,
  CreatePaymentDto,
  ProcessRefundDto,
  Payout,
  Review,
  CreateReviewDto,
  UpdateReviewDto,
  Notification,
  NotificationPreferences,
  DocumentDetails,
  UploadDocumentDto,
  PricingCalculationRequest,
  PricingCalculationResult,
  JobTemplate,
  CreateJobTemplateDto,
  RecurringJob,
  CreateRecurringJobDto,
  Message,
  SendMessageDto,
  AdminStatistics,
  PaginatedResponse,
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

  static async getAdminDashboard(): Promise<AdminStatistics> {
    return apiClient.get<AdminStatistics>('/api/admin/dashboard');
  }

  // ==================== Jobs ====================

  static async getAllJobs(params?: {
    status?: string;
    jobType?: string;
    vehicleType?: string;
    driverId?: string;
    customerId?: string;
    page?: number;
    limit?: number;
  }): Promise<PaginatedResponse<Job>> {
    return apiClient.get<PaginatedResponse<Job>>('/api/jobs', params);
  }

  static async getJobById(jobId: string): Promise<Job> {
    return apiClient.get<Job>(`/api/jobs/${jobId}`);
  }

  static async createJob(data: CreateJobDto): Promise<Job> {
    return apiClient.post<Job>('/api/jobs', data);
  }

  static async bookJob(data: BookJobDto): Promise<BookJobResponse> {
    return apiClient.post<BookJobResponse>('/api/customers/jobs/book', data);
  }

  static async updateJobStatus(jobId: string, data: UpdateJobStatusDto): Promise<Job> {
    return apiClient.patch<Job>(`/api/jobs/${jobId}/status`, data);
  }

  static async rescheduleJob(jobId: string, data: RescheduleJobDto): Promise<Job> {
    return apiClient.post<Job>(`/api/jobs/${jobId}/reschedule`, data);
  }

  static async cancelJob(jobId: string, data: CancelJobDto): Promise<Job> {
    return apiClient.post<Job>(`/api/jobs/${jobId}/cancel`, data);
  }

  static async getAvailableJobs(params?: {
    vehicleType?: string;
    page?: number;
    limit?: number;
  }): Promise<PaginatedResponse<Job>> {
    return apiClient.get<PaginatedResponse<Job>>('/api/jobs/available', params);
  }

  static async assignJob(jobId: string, driverId: string): Promise<Job> {
    return apiClient.post<Job>(`/api/jobs/${jobId}/assign`, { driverId });
  }

  static async getCustomerJobs(params?: {
    status?: string;
    page?: number;
    pageSize?: number;
  }): Promise<Job[]> {
    return apiClient.get<Job[]>('/api/customers/jobs', params);
  }

  // ==================== Job Bidding ====================

  static async createJobBid(data: CreateJobBidDto): Promise<JobBid> {
    return apiClient.post<JobBid>('/api/jobbids', data);
  }

  static async getJobBids(jobId: string, status?: string): Promise<JobBid[]> {
    return apiClient.get<JobBid[]>(`/api/jobbids/job/${jobId}`, { status });
  }

  static async getMyBids(status?: string): Promise<JobBid[]> {
    return apiClient.get<JobBid[]>('/api/jobbids/driver/me', { status });
  }

  static async acceptBid(bidId: string, notes?: string): Promise<JobBid> {
    return apiClient.post<JobBid>(`/api/jobbids/${bidId}/accept`, { notes });
  }

  static async rejectBid(bidId: string, notes?: string): Promise<JobBid> {
    return apiClient.post<JobBid>(`/api/jobbids/${bidId}/reject`, { notes });
  }

  static async withdrawBid(bidId: string): Promise<JobBid> {
    return apiClient.post<JobBid>(`/api/jobbids/${bidId}/withdraw`);
  }

  // ==================== Payments ====================

  static async createPayment(data: CreatePaymentDto): Promise<Payment> {
    return apiClient.post<Payment>('/api/payments', data);
  }

  static async getPaymentById(paymentId: string): Promise<Payment> {
    return apiClient.get<Payment>(`/api/payments/${paymentId}`);
  }

  static async getJobPayments(jobId: string): Promise<Payment[]> {
    return apiClient.get<Payment[]>(`/api/payments/jobs/${jobId}`);
  }

  static async getMyPayments(params?: {
    status?: string;
    page?: number;
    pageSize?: number;
  }): Promise<Payment[]> {
    return apiClient.get<Payment[]>('/api/customers/me/payments', params);
  }

  static async getMyPayouts(params?: {
    page?: number;
    pageSize?: number;
  }): Promise<Payout[]> {
    return apiClient.get<Payout[]>('/api/drivers/me/payouts', params);
  }

  static async processRefund(paymentId: string, data: ProcessRefundDto): Promise<void> {
    return apiClient.post<void>(`/api/payments/${paymentId}/refund`, data);
  }

  static async downloadReceipt(paymentId: string): Promise<Blob> {
    return apiClient.get<Blob>(`/api/payments/${paymentId}/receipt`);
  }

  // ==================== Reviews ====================

  static async createReview(data: CreateReviewDto): Promise<Review> {
    return apiClient.post<Review>('/api/reviews', data);
  }

  static async getReviewById(reviewId: string): Promise<Review> {
    return apiClient.get<Review>(`/api/reviews/${reviewId}`);
  }

  static async getDriverReviews(driverId: string, params?: {
    page?: number;
    pageSize?: number;
  }): Promise<Review[]> {
    return apiClient.get<Review[]>(`/api/reviews/drivers/${driverId}`, params);
  }

  static async updateReview(reviewId: string, data: UpdateReviewDto): Promise<Review> {
    return apiClient.put<Review>(`/api/reviews/${reviewId}`, data);
  }

  static async deleteReview(reviewId: string): Promise<void> {
    return apiClient.delete<void>(`/api/reviews/${reviewId}`);
  }

  static async respondToReview(reviewId: string, response: string): Promise<Review> {
    return apiClient.post<Review>(`/api/reviews/${reviewId}/response`, { response });
  }

  // ==================== Notifications ====================

  static async getMyNotifications(params?: {
    isRead?: boolean;
    type?: string;
    page?: number;
    pageSize?: number;
  }): Promise<Notification[]> {
    return apiClient.get<Notification[]>('/api/notifications/me', params);
  }

  static async getUnreadCount(): Promise<{ count: number }> {
    return apiClient.get<{ count: number }>('/api/notifications/me/unread-count');
  }

  static async markNotificationAsRead(notificationId: string): Promise<Notification> {
    return apiClient.patch<Notification>(`/api/notifications/${notificationId}/read`);
  }

  static async markAllNotificationsAsRead(): Promise<void> {
    return apiClient.patch<void>('/api/notifications/read-all');
  }

  static async deleteNotification(notificationId: string): Promise<void> {
    return apiClient.delete<void>(`/api/notifications/${notificationId}`);
  }

  static async getNotificationPreferences(): Promise<NotificationPreferences> {
    return apiClient.get<NotificationPreferences>('/api/notifications/preferences');
  }

  static async updateNotificationPreferences(data: Partial<NotificationPreferences>): Promise<NotificationPreferences> {
    return apiClient.put<NotificationPreferences>('/api/notifications/preferences', data);
  }

  // ==================== Documents ====================

  static async getMyDocuments(): Promise<DocumentDetails[]> {
    return apiClient.get<DocumentDetails[]>('/api/drivers/me/documents');
  }

  static async uploadDocument(data: UploadDocumentDto): Promise<DocumentDetails> {
    const formData = new FormData();
    formData.append('file', data.file);
    formData.append('documentType', data.documentType);
    if (data.expiryDate) {
      formData.append('expiryDate', data.expiryDate);
    }

    return apiClient.post<DocumentDetails>('/api/drivers/me/documents', formData);
  }

  static async deleteDocument(documentId: string): Promise<void> {
    return apiClient.delete<void>(`/api/drivers/me/documents/${documentId}`);
  }

  static async getPendingDocuments(params?: {
    page?: number;
    pageSize?: number;
  }): Promise<DocumentDetails[]> {
    return apiClient.get<DocumentDetails[]>('/api/documents/pending', params);
  }

  static async rejectDocument(documentId: string, reason: string, notes?: string): Promise<DocumentDetails> {
    return apiClient.post<DocumentDetails>(`/api/documents/${documentId}/reject`, { reason, notes });
  }

  // ==================== Pricing ====================

  static async calculatePricing(data: PricingCalculationRequest): Promise<PricingCalculationResult> {
    return apiClient.post<PricingCalculationResult>('/api/pricing/calculate', data);
  }

  static async estimatePrice(distanceInMiles: number, vehicleType: string): Promise<{ estimatedPrice: number }> {
    return apiClient.get<{ estimatedPrice: number }>('/api/pricing/estimate', {
      distanceInMiles,
      vehicleType,
    });
  }

  // ==================== Job Templates ====================

  static async getMyJobTemplates(): Promise<JobTemplate[]> {
    return apiClient.get<JobTemplate[]>('/api/jobtemplates/me');
  }

  static async getJobTemplateById(templateId: string): Promise<JobTemplate> {
    return apiClient.get<JobTemplate>(`/api/jobtemplates/${templateId}`);
  }

  static async createJobTemplate(data: CreateJobTemplateDto): Promise<JobTemplate> {
    return apiClient.post<JobTemplate>('/api/jobtemplates', data);
  }

  static async updateJobTemplate(templateId: string, data: Partial<CreateJobTemplateDto>): Promise<JobTemplate> {
    return apiClient.put<JobTemplate>(`/api/jobtemplates/${templateId}`, data);
  }

  static async deleteJobTemplate(templateId: string): Promise<void> {
    return apiClient.delete<void>(`/api/jobtemplates/${templateId}`);
  }

  static async createJobFromTemplate(templateId: string, scheduledDate: string): Promise<Job> {
    return apiClient.post<Job>(`/api/jobtemplates/${templateId}/create-job`, { scheduledDate });
  }

  // ==================== Recurring Jobs ====================

  static async getMyRecurringJobs(): Promise<RecurringJob[]> {
    return apiClient.get<RecurringJob[]>('/api/recurringjobs/me');
  }

  static async getRecurringJobById(jobId: string): Promise<RecurringJob> {
    return apiClient.get<RecurringJob>(`/api/recurringjobs/${jobId}`);
  }

  static async createRecurringJob(data: CreateRecurringJobDto): Promise<RecurringJob> {
    return apiClient.post<RecurringJob>('/api/recurringjobs', data);
  }

  static async updateRecurringJob(jobId: string, data: Partial<CreateRecurringJobDto>): Promise<RecurringJob> {
    return apiClient.put<RecurringJob>(`/api/recurringjobs/${jobId}`, data);
  }

  static async updateRecurringJobStatus(jobId: string, status: string): Promise<RecurringJob> {
    return apiClient.patch<RecurringJob>(`/api/recurringjobs/${jobId}/status`, { status });
  }

  static async deleteRecurringJob(jobId: string): Promise<void> {
    return apiClient.delete<void>(`/api/recurringjobs/${jobId}`);
  }

  // ==================== Messages ====================

  static async sendMessage(data: SendMessageDto): Promise<Message> {
    return apiClient.post<Message>('/api/messages/send', data);
  }

  static async getMyMessages(params?: {
    conversationWith?: string;
    isRead?: boolean;
    page?: number;
    pageSize?: number;
  }): Promise<Message[]> {
    return apiClient.get<Message[]>('/api/messages/me', params);
  }

  static async markMessageAsRead(messageId: string): Promise<Message> {
    return apiClient.patch<Message>(`/api/messages/${messageId}/read`);
  }
}
