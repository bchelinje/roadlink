import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import {
  DashboardOverview,
  UserAnalytics,
  JobAnalytics,
  RevenueAnalytics,
  PlatformHealth
} from '@core/models/admin-dashboard.models';

@Injectable({
  providedIn: 'root'
})
export class AdminDashboardService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/admin/dashboard`;

  /**
   * Get complete dashboard overview
   */
  getDashboardOverview(): Observable<DashboardOverview> {
    return this.http.get<DashboardOverview>(this.baseUrl);
  }

  /**
   * Get user analytics
   */
  getUserAnalytics(period: 'daily' | 'weekly' | 'monthly' = 'daily', days = 30): Observable<UserAnalytics> {
    const params = new HttpParams()
      .set('period', period)
      .set('days', days.toString());

    return this.http.get<UserAnalytics>(`${this.baseUrl}/analytics/users`, { params });
  }

  /**
   * Get job analytics
   */
  getJobAnalytics(period: 'daily' | 'weekly' | 'monthly' = 'daily', days = 30): Observable<JobAnalytics> {
    const params = new HttpParams()
      .set('period', period)
      .set('days', days.toString());

    return this.http.get<JobAnalytics>(`${this.baseUrl}/analytics/jobs`, { params });
  }

  /**
   * Get revenue analytics
   */
  getRevenueAnalytics(period: 'daily' | 'weekly' | 'monthly' = 'daily', days = 30): Observable<RevenueAnalytics> {
    const params = new HttpParams()
      .set('period', period)
      .set('days', days.toString());

    return this.http.get<RevenueAnalytics>(`${this.baseUrl}/analytics/revenue`, { params });
  }

  /**
   * Get platform health status
   */
  getPlatformHealth(): Observable<PlatformHealth> {
    return this.http.get<PlatformHealth>(`${this.baseUrl}/health`);
  }
}
