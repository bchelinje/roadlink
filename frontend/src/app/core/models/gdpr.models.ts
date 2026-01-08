/**
 * GDPR Data Privacy Models
 * DTOs matching the backend API endpoints
 */

export enum DeletionType {
  Soft = 'soft',
  Hard = 'hard'
}

export enum DeletionRequestStatus {
  Pending = 'pending',
  Confirmed = 'confirmed',
  Approved = 'approved',
  Rejected = 'rejected',
  Completed = 'completed',
  Cancelled = 'cancelled'
}

/**
 * User's data summary showing count of related records
 */
export interface UserDataSummary {
  userId: string;
  username: string;
  email: string;
  totalRelatedRecords: number;
  details: {
    jobsAsCustomer: number;
    jobsAsDriver: number;
    paymentsAsCustomer: number;
    paymentsAsDriver: number;
    reviews: number;
    supportTickets: number;
    chatMessages: number;
    complaints: number;
    notifications: number;
    activityLogs: number;
    documents: number;
    savedAddresses: number;
    favoriteDrivers: number;
  };
}

/**
 * Complete user data export (GDPR Article 20)
 */
export interface UserDataExport {
  user: {
    id: string;
    userName: string;
    email: string;
    phoneNumber: string;
    createdAt: string;
    lastLoginAt?: string;
  };
  customerProfile?: any;
  driverProfile?: any;
  jobs: any[];
  payments: any[];
  reviews: any[];
  supportTickets: any[];
  chatMessages: any[];
  complaints: any[];
  notifications: any[];
  activityLogs: any[];
  documents: any[];
  savedAddresses: any[];
  favoriteDrivers: any[];
  exportedAt: string;
}

/**
 * Request to delete user data
 */
export interface RequestDataDeletionDto {
  reason: string;
  deletionType: DeletionType;
  requestDataExport: boolean;
  gracePeriodDays?: number;
}

/**
 * Data deletion request entity
 */
export interface DataDeletionRequest {
  id: string;
  userId: string;
  userName?: string;
  userEmail?: string;
  reason: string;
  deletionType: DeletionType;
  status: DeletionRequestStatus;
  requestDataExport: boolean;
  exportedDataPath?: string;
  requestedAt: string;
  scheduledDeletionDate?: string;
  confirmedAt?: string;
  approvedAt?: string;
  approvedByUserId?: string;
  approvedByUserName?: string;
  rejectedAt?: string;
  rejectedByUserId?: string;
  rejectedByUserName?: string;
  rejectionReason?: string;
  completedAt?: string;
  completedByUserId?: string;
  completedByUserName?: string;
  cancelledAt?: string;
  confirmationToken?: string;
  confirmationTokenExpiresAt?: string;
  gracePeriodDays: number;
  relatedJobsCount?: number;
  relatedPaymentsCount?: number;
  relatedReviewsCount?: number;
  relatedMessagesCount?: number;
  relatedRecordsCount?: number;
}

/**
 * Admin review deletion request DTO
 */
export interface ReviewDeletionRequestDto {
  approved: boolean;
  rejectionReason?: string;
}

/**
 * Result of deletion/anonymization operation
 */
export interface DeletionResult {
  success: boolean;
  message: string;
  deletionType: DeletionType;
  affectedRecords?: {
    [tableName: string]: number;
  };
  exportedDataPath?: string;
}

/**
 * Direct anonymization request for admin
 */
export interface AnonymizeUserDto {
  deletionType: DeletionType;
  performedByUserId: string;
  reason: string;
}

/**
 * Paginated response for deletion requests
 */
export interface PaginatedDeletionRequestsResponse {
  data: DataDeletionRequest[];
  pagination: {
    page: number;
    pageSize: number;
    total: number;
    totalPages: number;
  };
}
