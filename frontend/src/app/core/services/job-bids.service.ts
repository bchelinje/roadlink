import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import { JobBid, CreateBidRequest, BidFilter } from '@core/models/job-bid.models';

@Injectable({
  providedIn: 'root'
})
export class JobBidsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/jobbids`;

  /**
   * Submit a bid on a job (Driver)
   */
  createBid(request: CreateBidRequest): Observable<JobBid> {
    return this.http.post<JobBid>(this.baseUrl, request);
  }

  /**
   * Get driver's own bids (Driver)
   */
  getMyBids(filter?: BidFilter): Observable<JobBid[]> {
    let params = new HttpParams();
    if (filter?.status) params = params.set('status', filter.status);
    if (filter?.pageNumber) params = params.set('pageNumber', filter.pageNumber.toString());
    if (filter?.pageSize) params = params.set('pageSize', filter.pageSize.toString());

    return this.http.get<JobBid[]>(`${this.baseUrl}/driver/me`, { params });
  }

  /**
   * Get all bids for a specific job (Customer/Admin)
   */
  getBidsForJob(jobId: string): Observable<JobBid[]> {
    return this.http.get<JobBid[]>(`${this.baseUrl}/job/${jobId}`);
  }

  /**
   * Accept a bid (Customer)
   */
  acceptBid(bidId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${bidId}/accept`, {});
  }

  /**
   * Reject a bid (Customer)
   */
  rejectBid(bidId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${bidId}/reject`, {});
  }

  /**
   * Withdraw a bid (Driver)
   */
  withdrawBid(bidId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${bidId}/withdraw`, {});
  }
}
