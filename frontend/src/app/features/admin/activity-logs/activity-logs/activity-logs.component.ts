// components/activity-logs/activity-logs.component.ts - UPDATED
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivityLogService } from '@core/services/activity-log.service';
import {
  ActivityLogFilter,
  ActivityAction,
  EntityType,
  LogSeverity
} from '@app/dtos/activity-log.model';
import {ActivityLog} from '@core/api';
import { ActivityLogDetailModalComponent } from '../activity-log-detail-modal/activity-log-detail-modal.component';

@Component({
  selector: 'app-activity-logs',
  standalone: true,
  imports: [CommonModule, FormsModule, ActivityLogDetailModalComponent], // ⭐ Add modal
  templateUrl: './activity-logs.component.html',
  styleUrls: ['./activity-logs.component.scss']
})
export class ActivityLogsComponent implements OnInit {
  logs: ActivityLog[] = [];
  filteredLogs: ActivityLog[] = [];
  loading = false;
  error = '';

  // Pagination
  currentPage = 1;
  pageSize = 20;
  totalPages = 0;
  totalCount = 0;

  // Filters
  filter: ActivityLogFilter = {
    page: 1,
    pageSize: 20
  };

  searchTerm = '';
  selectedAction: ActivityAction | '' = '';
  selectedEntityType: EntityType | '' = '';
  selectedSeverity: LogSeverity | '' = '';
  startDate = '';
  endDate = '';

  // Enums for template
  ActivityAction = ActivityAction;
  EntityType = EntityType;
  LogSeverity = LogSeverity;

  // Enum arrays for dropdowns
  actions = Object.values(ActivityAction);
  entityTypes = Object.values(EntityType);
  severities = Object.values(LogSeverity);

  // View modes
  viewMode: 'list' | 'timeline' = 'timeline';

  // Export
  exporting = false;

  // ⭐ Modal state
  selectedLog: ActivityLog | null = null;
  isModalOpen = false;

  constructor(private activityLogService: ActivityLogService) {}

  ngOnInit(): void {
    this.loadLogs();
  }

  loadLogs(): void {
    this.loading = true;
    this.error = '';

    this.filter = {
      page: this.currentPage,
      pageSize: this.pageSize,
      searchTerm: this.searchTerm || undefined,
      action: this.selectedAction || undefined,
      entityType: this.selectedEntityType || undefined,
      severity: this.selectedSeverity || undefined,
      startDate: this.startDate ? new Date(this.startDate) : undefined,
      endDate: this.endDate ? new Date(this.endDate) : undefined
    };

    this.activityLogService.getLogs(this.filter).subscribe({
      next: (response) => {
        this.logs = response.logs ?? [];
        this.filteredLogs = response.logs ?? [];
        this.totalCount = response.totalCount ?? 0;
        this.totalPages = response.totalPages ?? 0;
        this.currentPage = response.page ?? 1;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load activity logs. Please try again.';
        console.error('Error loading logs:', err);
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    this.currentPage = 1;
    this.loadLogs();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedAction = '';
    this.selectedEntityType = '';
    this.selectedSeverity = '';
    this.startDate = '';
    this.endDate = '';
    this.currentPage = 1;
    this.loadLogs();
  }

  onSearch(): void {
    this.currentPage = 1;
    this.loadLogs();
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadLogs();
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.loadLogs();
    }
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.loadLogs();
    }
  }

  exportToCsv(): void {
    this.exporting = true;

    this.activityLogService.exportLogs(this.filter).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `activity-logs-${new Date().toISOString().split('T')[0]}.csv`;
        link.click();
        window.URL.revokeObjectURL(url);
        this.exporting = false;
      },
      error: (err) => {
        console.error('Error exporting logs:', err);
        alert('Failed to export logs. Please try again.');
        this.exporting = false;
      }
    });
  }

  toggleViewMode(): void {
    this.viewMode = this.viewMode === 'list' ? 'timeline' : 'list';
  }

  // ⭐ Modal methods
  viewLogDetails(log: ActivityLog): void {
    this.selectedLog = log;
    this.isModalOpen = true;
  }

  closeModal(): void {
    this.isModalOpen = false;
    this.selectedLog = null;
  }

  getActionDisplayName(action: string | null | undefined): string {
    if (!action) return 'Unknown';
    return action.replace(/_/g, ' ').toLowerCase()
      .split(' ')
      .map(word => word.charAt(0).toUpperCase() + word.slice(1))
      .join(' ');
  }

  getSeverityColor(severity: string | null | undefined): string {
    const colorMap: Record<string, string> = {
      'INFO': 'blue',
      'WARNING': 'yellow',
      'ERROR': 'red',
      'CRITICAL': 'purple'
    };
    return colorMap[severity ?? 'INFO'] || 'gray';
  }

  getActionIcon(action: string | null | undefined): string {
    if (!action) return 'activity';

    const actionMap: Record<string, string> = {
      'USER_CREATED': 'user-plus',
      'USER_UPDATED': 'user-edit',
      'USER_DELETED': 'user-minus',
      'LOGIN_SUCCESS': 'login',
      'LOGIN_FAILED': 'x-circle',
      'LOGOUT': 'logout',
    };
    return actionMap[action] || 'activity';
  }

  formatDate(date: string | Date | null | undefined): string {
    if (!date) return 'Unknown';

    const d = new Date(date);
    if (isNaN(d.getTime())) return 'Invalid Date';

    const now = new Date();
    const diffMs = now.getTime() - d.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins} minute${diffMins > 1 ? 's' : ''} ago`;
    if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`;
    if (diffDays < 7) return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`;

    return d.toLocaleString('en-GB', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  getUserInitials(name: string | null | undefined): string {
    if (!name) return 'U';
    const parts = name.split(' ');
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.substring(0, 2).toUpperCase();
  }

  getUserAvatarColor(name: string | null | undefined): string {
    if (!name) return 'bg-gray-500';

    const colors = [
      'bg-blue-500',
      'bg-green-500',
      'bg-yellow-500',
      'bg-red-500',
      'bg-purple-500',
      'bg-pink-500',
      'bg-indigo-500',
      'bg-teal-500'
    ];
    const index = name.length % colors.length;
    return colors[index];
  }

  getPageNumbers(): number[] {
    const pages: number[] = [];
    const maxVisible = 5;

    let start = Math.max(1, this.currentPage - Math.floor(maxVisible / 2));
    let end = Math.min(this.totalPages, start + maxVisible - 1);

    if (end - start + 1 < maxVisible) {
      start = Math.max(1, end - maxVisible + 1);
    }

    for (let i = start; i <= end; i++) {
      pages.push(i);
    }

    return pages;
  }

  protected readonly Math = Math;
}
