import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  UserDataSummary,
  UserDataExport,
  RequestDataDeletionDto,
  DataDeletionRequest,
  ReviewDeletionRequestDto,
  DeletionResult,
  AnonymizeUserDto,
  PaginatedDeletionRequestsResponse
} from '../models/gdpr.models';

/**
 * GDPR Data Privacy Service
 * Handles all GDPR-related API calls for data export, deletion, and anonymization
 */
@Injectable({
  providedIn: 'root'
})
export class GdprService {
  private readonly apiUrl = `${environment.apiBaseUrl}/api/data-privacy`;

  constructor(private http: HttpClient) {}

  // ==========================================
  // USER SELF-SERVICE ENDPOINTS (GDPR Rights)
  // ==========================================

  /**
   * Export all personal data (GDPR Article 20 - Right to Data Portability)
   * GET /api/data-privacy/export
   */
  exportMyData(): Observable<UserDataExport> {
    return this.http.get<UserDataExport>(`${this.apiUrl}/export`);
  }

  /**
   * Download exported data as JSON file
   */
  downloadMyDataExport(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/export`, {
      responseType: 'blob'
    });
  }

  /**
   * Get summary of all related data
   * GET /api/data-privacy/my-data-summary
   */
  getMyDataSummary(): Observable<UserDataSummary> {
    return this.http.get<UserDataSummary>(`${this.apiUrl}/my-data-summary`);
  }

  /**
   * Request account deletion (GDPR Article 17 - Right to Erasure)
   * POST /api/data-privacy/request-deletion
   */
  requestDeletion(request: RequestDataDeletionDto): Observable<DataDeletionRequest> {
    return this.http.post<DataDeletionRequest>(`${this.apiUrl}/request-deletion`, request);
  }

  /**
   * Get all deletion requests for current user
   * GET /api/data-privacy/my-deletion-requests
   */
  getMyDeletionRequests(): Observable<DataDeletionRequest[]> {
    return this.http.get<DataDeletionRequest[]>(`${this.apiUrl}/my-deletion-requests`);
  }

  /**
   * Confirm deletion request with email token
   * POST /api/data-privacy/confirm-deletion/{id}
   */
  confirmDeletion(requestId: string, token: string): Observable<DataDeletionRequest> {
    return this.http.post<DataDeletionRequest>(
      `${this.apiUrl}/confirm-deletion/${requestId}`,
      { token }
    );
  }

  /**
   * Cancel a pending deletion request
   * POST /api/data-privacy/cancel-deletion/{id}
   */
  cancelDeletion(requestId: string): Observable<DataDeletionRequest> {
    return this.http.post<DataDeletionRequest>(
      `${this.apiUrl}/cancel-deletion/${requestId}`,
      {}
    );
  }

  // ==========================================
  // ADMIN MANAGEMENT ENDPOINTS
  // ==========================================

  /**
   * Get all deletion requests (Admin only)
   * GET /api/data-privacy/admin/deletion-requests
   */
  getAllDeletionRequests(
    status?: string,
    pageNumber: number = 1,
    pageSize: number = 20
  ): Observable<PaginatedDeletionRequestsResponse> {
    let url = `${this.apiUrl}/admin/deletion-requests?page=${pageNumber}&pageSize=${pageSize}`;
    if (status) {
      url += `&status=${status}`;
    }
    return this.http.get<PaginatedDeletionRequestsResponse>(url);
  }

  /**
   * Review (approve/reject) a deletion request (Admin only)
   * POST /api/data-privacy/admin/deletion-requests/{id}/review
   */
  reviewDeletionRequest(
    requestId: string,
    review: ReviewDeletionRequestDto
  ): Observable<DataDeletionRequest> {
    return this.http.post<DataDeletionRequest>(
      `${this.apiUrl}/admin/deletion-requests/${requestId}/review`,
      review
    );
  }

  /**
   * Process (execute) an approved deletion request (Admin only)
   * POST /api/data-privacy/admin/deletion-requests/{id}/process
   */
  processDeletionRequest(requestId: string): Observable<DeletionResult> {
    return this.http.post<DeletionResult>(
      `${this.apiUrl}/admin/deletion-requests/${requestId}/process`,
      {}
    );
  }

  /**
   * Directly anonymize a user without request (SuperAdmin only)
   * POST /api/data-privacy/admin/users/{userId}/anonymize
   */
  anonymizeUser(userId: string, request: AnonymizeUserDto): Observable<DeletionResult> {
    return this.http.post<DeletionResult>(
      `${this.apiUrl}/admin/users/${userId}/anonymize`,
      request
    );
  }

  /**
   * Export a specific user's data (Admin only)
   * GET /api/data-privacy/admin/users/{userId}/export
   */
  exportUserData(userId: string): Observable<UserDataExport> {
    return this.http.get<UserDataExport>(`${this.apiUrl}/admin/users/${userId}/export`);
  }

  /**
   * Download a specific user's data as JSON file (Admin only)
   */
  downloadUserDataExport(userId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/admin/users/${userId}/export`, {
      responseType: 'blob'
    });
  }

  /**
   * Get data summary for a specific user (Admin only)
   * GET /api/data-privacy/admin/users/{userId}/data-summary
   */
  getUserDataSummary(userId: string): Observable<UserDataSummary> {
    return this.http.get<UserDataSummary>(`${this.apiUrl}/admin/users/${userId}/data-summary`);
  }
}
