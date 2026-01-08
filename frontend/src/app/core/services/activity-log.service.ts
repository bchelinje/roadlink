// services/activity-log.service.ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {environment} from '@environments/environment';
import {ActivityAction, ActivityLog, ActivityLogFilter, LogSeverity} from '@app/dtos/activity-log.model';
import {ActivityLogResponse} from '@core/api';


@Injectable({
  providedIn: 'root'
})
export class ActivityLogService {
  private apiUrl = `${environment.apiBaseUrl}/api/activityLogs`;

  constructor(private http: HttpClient) {}

  /**
   * Get paginated activity logs with filters
   */
  getLogs(filter: ActivityLogFilter = {}): Observable<ActivityLogResponse> {
    let params = new HttpParams();

    if (filter.userId) params = params.set('userId', filter.userId);
    if (filter.action) params = params.set('action', filter.action);
    if (filter.entityType) params = params.set('entityType', filter.entityType);
    if (filter.severity) params = params.set('severity', filter.severity);
    if (filter.startDate) params = params.set('startDate', filter.startDate.toISOString());
    if (filter.endDate) params = params.set('endDate', filter.endDate.toISOString());
    if (filter.searchTerm) params = params.set('searchTerm', filter.searchTerm);
    if (filter.page) params = params.set('page', filter.page.toString());
    if (filter.pageSize) params = params.set('pageSize', filter.pageSize.toString());

    return this.http.get<ActivityLogResponse>(this.apiUrl, { params });
  }

  /**
   * Get a single activity log by ID
   */
  getLogById(id: string): Observable<ActivityLog> {
    return this.http.get<ActivityLog>(`${this.apiUrl}/${id}`);
  }

  /**
   * Get recent activity logs (last 24 hours)
   */
  getRecentLogs(limit: number = 20): Observable<ActivityLog[]> {
    return this.http.get<ActivityLog[]>(`${this.apiUrl}/recent`, {
      params: new HttpParams().set('limit', limit.toString())
    });
  }

  /**
   * Get activity logs for a specific user
   */
  getUserLogs(userId: string, page: number = 1, pageSize: number = 20): Observable<ActivityLogResponse> {
    return this.getLogs({ userId, page, pageSize });
  }

  /**
   * Export activity logs to CSV
   */
  exportLogs(filter: ActivityLogFilter = {}): Observable<Blob> {
    let params = new HttpParams();

    if (filter.userId) params = params.set('userId', filter.userId);
    if (filter.action) params = params.set('action', filter.action);
    if (filter.entityType) params = params.set('entityType', filter.entityType);
    if (filter.severity) params = params.set('severity', filter.severity);
    if (filter.startDate) params = params.set('startDate', filter.startDate.toISOString());
    if (filter.endDate) params = params.set('endDate', filter.endDate.toISOString());
    if (filter.searchTerm) params = params.set('searchTerm', filter.searchTerm);

    return this.http.get(`${this.apiUrl}/export`, {
      params,
      responseType: 'blob'
    });
  }

  /**
   * Create a new activity log (typically called from other services)
   */
  createLog(log: Partial<ActivityLog>): Observable<ActivityLog> {
    return this.http.post<ActivityLog>(this.apiUrl, log);
  }

  /**
   * Delete old logs (admin only)
   */
  deleteOldLogs(beforeDate: Date): Observable<{ deletedCount: number }> {
    return this.http.delete<{ deletedCount: number }>(`${this.apiUrl}/cleanup`, {
      params: new HttpParams().set('beforeDate', beforeDate.toISOString())
    });
  }

  /**
   * Get activity statistics
   */
  getStatistics(days: number = 30): Observable<ActivityStatistics> {
    return this.http.get<ActivityStatistics>(`${this.apiUrl}/statistics`, {
      params: new HttpParams().set('days', days.toString())
    });
  }

  /**
   * Helper: Get action display name
   */
  getActionDisplayName(action: ActivityAction): string {
    return action.replace(/_/g, ' ').toLowerCase()
      .split(' ')
      .map(word => word.charAt(0).toUpperCase() + word.slice(1))
      .join(' ');
  }

  /**
   * Helper: Get action icon class
   */
  getActionIcon(action: ActivityAction): string {
    const actionMap: Record<string, string> = {
      // User actions
      USER_CREATED: 'user-plus',
      USER_UPDATED: 'user-edit',
      USER_DELETED: 'user-minus',
      USER_LOCKED: 'lock',
      USER_UNLOCKED: 'unlock',
      USER_PASSWORD_CHANGED: 'key',
      USER_EMAIL_VERIFIED: 'check-circle',

      // Auth actions
      LOGIN_SUCCESS: 'login',
      LOGIN_FAILED: 'x-circle',
      LOGOUT: 'logout',
      PASSWORD_RESET_REQUESTED: 'mail',
      PASSWORD_RESET_COMPLETED: 'check',

      // Role actions
      ROLE_CREATED: 'shield-plus',
      ROLE_UPDATED: 'shield',
      ROLE_DELETED: 'shield-minus',
      ROLE_ASSIGNED: 'user-check',
      ROLE_REMOVED: 'user-x',

      // Move actions
      MOVE_CREATED: 'truck',
      MOVE_UPDATED: 'edit',
      MOVE_CANCELLED: 'x',
      MOVE_COMPLETED: 'check-circle',

      // Driver actions
      DRIVER_ASSIGNED: 'user-plus',
      DRIVER_UNASSIGNED: 'user-minus',
      DRIVER_STATUS_CHANGED: 'activity',

      // System actions
      SETTINGS_CHANGED: 'settings',
      DATA_EXPORTED: 'download',
      DATA_IMPORTED: 'upload',
      BACKUP_CREATED: 'database'
    };

    return actionMap[action] || 'activity';
  }

  /**
   * Helper: Get severity color class
   */
  getSeverityColor(severity: LogSeverity): string {
    const colorMap: Record<LogSeverity, string> = {
      INFO: 'blue',
      WARNING: 'yellow',
      ERROR: 'red',
      CRITICAL: 'purple'
    };

    return colorMap[severity] || 'gray';
  }
}

export interface ActivityStatistics {
  totalLogs: number;
  logsByAction: Record<ActivityAction, number>;
  logsByUser: Record<string, number>;
  logsBySeverity: Record<LogSeverity, number>;
  logsByDay: Array<{ date: string; count: number }>;
}
