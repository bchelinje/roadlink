import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import {
  Complaint,
  CreateComplaintRequest,
  ResolveComplaintRequest,
  EscalateComplaintRequest,
  AddInvestigationNoteRequest,
  ComplaintStatistics,
  ComplaintFilter
} from '@core/models/complaint.models';

@Injectable({
  providedIn: 'root'
})
export class ComplaintsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/complaints`;

  /**
   * File a new complaint
   */
  createComplaint(request: CreateComplaintRequest): Observable<Complaint> {
    return this.http.post<Complaint>(this.baseUrl, request);
  }

  /**
   * Get complaints list with filtering
   */
  getComplaints(filter?: ComplaintFilter): Observable<Complaint[]> {
    let params = new HttpParams();
    if (filter?.status) params = params.set('status', filter.status);
    if (filter?.severity) params = params.set('severity', filter.severity);
    if (filter?.category) params = params.set('category', filter.category);
    if (filter?.complainantId) params = params.set('complainantId', filter.complainantId);
    if (filter?.againstId) params = params.set('againstId', filter.againstId);
    if (filter?.pageNumber) params = params.set('pageNumber', filter.pageNumber.toString());
    if (filter?.pageSize) params = params.set('pageSize', filter.pageSize.toString());

    return this.http.get<Complaint[]>(this.baseUrl, { params });
  }

  /**
   * Get a specific complaint by ID
   */
  getComplaint(complaintId: string): Observable<Complaint> {
    return this.http.get<Complaint>(`${this.baseUrl}/${complaintId}`);
  }

  /**
   * Resolve complaint (Admin only)
   */
  resolveComplaint(complaintId: string, request: ResolveComplaintRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${complaintId}/resolve`, request);
  }

  /**
   * Escalate complaint (Admin only)
   */
  escalateComplaint(complaintId: string, request: EscalateComplaintRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${complaintId}/escalate`, request);
  }

  /**
   * Add investigation note (Admin only)
   */
  addInvestigationNote(complaintId: string, request: AddInvestigationNoteRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${complaintId}/notes`, request);
  }

  /**
   * Get complaint statistics (Admin only)
   */
  getStatistics(): Observable<ComplaintStatistics> {
    return this.http.get<ComplaintStatistics>(`${this.baseUrl}/statistics`);
  }
}
