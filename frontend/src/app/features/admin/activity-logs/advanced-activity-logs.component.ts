import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  ActivityLogsService,
  ActivityLog,
  ActivityStatistics
} from '@core/api';

@Component({
  selector: 'app-advanced-activity-logs',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container mx-auto px-4 py-8">

      <!-- Page Header -->
      <div class="page-header">
        <div>
          <h1>Activity Logs</h1>
          <p class="subtitle">Monitor system activity and user actions</p>
        </div>
      </div>

      <!-- Statistics Dashboard -->
      <div *ngIf="statistics" class="mb-8">
        <h3 class="text-lg font-semibold text-gray-900 mb-4">Statistics</h3>
        <div class="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
          <div class="bg-white border border-gray-200 rounded-lg p-4">
            <p class="text-xs text-gray-600 mb-1">Total Logs</p>
            <p class="text-2xl font-bold text-gray-900">{{ statistics.totalLogs?.toLocaleString() || 0 }}</p>
          </div>

          <div class="bg-blue-50 border border-blue-200 rounded-lg p-4">
            <p class="text-xs text-blue-600 mb-1">Info</p>
            <p class="text-2xl font-bold text-blue-600">{{ (statistics.logsBySeverity?.['INFO'] || 0).toLocaleString() }}</p>
          </div>

          <div class="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
            <p class="text-xs text-yellow-700 mb-1">Warnings</p>
            <p class="text-2xl font-bold text-yellow-700">{{ (statistics.logsBySeverity?.['WARNING'] || 0).toLocaleString() }}</p>
          </div>

          <div class="bg-red-50 border border-red-200 rounded-lg p-4">
            <p class="text-xs text-red-600 mb-1">Errors</p>
            <p class="text-2xl font-bold text-red-600">{{ (statistics.logsBySeverity?.['ERROR'] || 0).toLocaleString() + (statistics.logsBySeverity?.['CRITICAL'] || 0) }}</p>
          </div>
        </div>

        <!-- Top Actions -->
        <div *ngIf="statistics.logsByAction && getTopItems(statistics.logsByAction).length > 0" class="bg-white border border-gray-200 rounded-lg p-4 mb-4">
          <h4 class="text-sm font-semibold text-gray-900 mb-3">Top Actions</h4>
          <div class="space-y-2">
            <div *ngFor="let item of getTopItems(statistics.logsByAction)" class="flex items-center justify-between">
              <span class="text-sm text-gray-700">{{ formatActionName(item.key) }}</span>
              <span class="text-sm font-medium text-gray-900">{{ item.value.toLocaleString() }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Actions Bar -->
      <div class="bg-white border border-gray-200 rounded-lg p-4 mb-6">
        <div class="flex flex-wrap items-center gap-3">
          <button
            (click)="exportLogs()"
            [disabled]="isExporting"
            class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium text-sm disabled:opacity-50 transition-colors"
          >
            {{ isExporting ? 'Exporting...' : 'Export to CSV' }}
          </button>

          <button
            (click)="showCleanupModal = true"
            class="px-4 py-2 bg-orange-600 hover:bg-orange-700 text-white rounded-lg font-medium text-sm transition-colors"
          >
            Cleanup Old Logs
          </button>

          <button
            (click)="loadData()"
            [disabled]="isLoading"
            class="px-4 py-2 border border-gray-300 hover:bg-gray-50 text-gray-700 rounded-lg font-medium text-sm transition-colors disabled:opacity-50"
          >
            Refresh
          </button>
        </div>
      </div>

      <!-- Filters -->
      <div class="bg-white border border-gray-200 rounded-lg p-4 mb-6">
        <h3 class="text-sm font-semibold text-gray-900 mb-3">Filters</h3>
        <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
          <div>
            <label class="block text-xs font-medium text-gray-700 mb-1">Search</label>
            <input
              type="text"
              [(ngModel)]="searchTerm"
              (keyup.enter)="applyFilters()"
              placeholder="Search..."
              class="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
            />
          </div>

          <div>
            <label class="block text-xs font-medium text-gray-700 mb-1">Action</label>
            <select
              [(ngModel)]="actionFilter"
              (change)="applyFilters()"
              class="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
            >
              <option value="">All Actions</option>
              <option value="CREATE">Create</option>
              <option value="UPDATE">Update</option>
              <option value="DELETE">Delete</option>
              <option value="LOGIN">Login</option>
              <option value="LOGOUT">Logout</option>
            </select>
          </div>

          <div>
            <label class="block text-xs font-medium text-gray-700 mb-1">Severity</label>
            <select
              [(ngModel)]="severityFilter"
              (change)="applyFilters()"
              class="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
            >
              <option value="">All Severities</option>
              <option value="INFO">Info</option>
              <option value="WARNING">Warning</option>
              <option value="ERROR">Error</option>
              <option value="CRITICAL">Critical</option>
            </select>
          </div>

          <div>
            <label class="block text-xs font-medium text-gray-700 mb-1">Entity Type</label>
            <select
              [(ngModel)]="entityTypeFilter"
              (change)="applyFilters()"
              class="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
            >
              <option value="">All Types</option>
              <option value="User">User</option>
              <option value="Job">Job</option>
              <option value="Driver">Driver</option>
              <option value="Vehicle">Vehicle</option>
              <option value="Payment">Payment</option>
            </select>
          </div>
        </div>

        <div class="mt-3 flex gap-2">
          <button
            (click)="applyFilters()"
            class="px-4 py-2 bg-gray-900 hover:bg-gray-800 text-white rounded-lg font-medium text-sm transition-colors"
          >
            Apply Filters
          </button>
          <button
            (click)="clearFilters()"
            class="px-4 py-2 border border-gray-300 hover:bg-gray-50 text-gray-700 rounded-lg font-medium text-sm transition-colors"
          >
            Clear
          </button>
        </div>
      </div>

      <!-- Success/Error Messages -->
      <div *ngIf="successMessage" class="mb-6 bg-green-50 border border-green-200 rounded-lg p-4">
        <p class="text-green-800">{{ successMessage }}</p>
      </div>

      <div *ngIf="errorMessage" class="mb-6 bg-red-50 border border-red-200 rounded-lg p-4">
        <p class="text-red-800">{{ errorMessage }}</p>
      </div>

      <!-- Loading State -->
      <div *ngIf="isLoading" class="text-center py-12">
        <div class="inline-block w-12 h-12 border-4 border-gray-300 border-t-blue-600 rounded-full animate-spin"></div>
        <p class="mt-4 text-gray-600">Loading activity logs...</p>
      </div>

      <!-- Logs List -->
      <div *ngIf="!isLoading" class="bg-white border border-gray-200 rounded-lg">
        <div class="p-4 border-b border-gray-200">
          <h3 class="text-sm font-semibold text-gray-900">Activity Logs ({{ totalCount }})</h3>
        </div>

        <div *ngIf="logs.length === 0" class="text-center py-12">
          <p class="text-gray-600">No activity logs found</p>
        </div>

        <div *ngIf="logs.length > 0" class="divide-y divide-gray-200">
          <div
            *ngFor="let log of logs"
            class="p-4 hover:bg-gray-50 transition-colors"
          >
            <div class="flex items-start gap-3">
              <div
                [class]="getSeverityIconClass(log.severity)"
                class="w-2 h-2 rounded-full mt-2"
              ></div>

              <div class="flex-1 min-w-0">
                <div class="flex items-start justify-between mb-1">
                  <div class="flex-1">
                    <p class="text-sm font-medium text-gray-900">{{ log.description }}</p>
                    <div class="mt-1 flex flex-wrap items-center gap-2 text-xs text-gray-600">
                      <span class="flex items-center gap-1">
                        <span class="font-medium">{{ log.userName }}</span>
                        <span class="text-gray-400">•</span>
                        <span>{{ log.action }}</span>
                      </span>
                      <span *ngIf="log.entityType" class="flex items-center gap-1">
                        <span class="text-gray-400">•</span>
                        <span>{{ log.entityType }}</span>
                        <span *ngIf="log.entityName">({{ log.entityName }})</span>
                      </span>
                      <span class="flex items-center gap-1">
                        <span class="text-gray-400">•</span>
                        <span>{{ formatDateTime(log.timestamp) }}</span>
                      </span>
                    </div>
                  </div>
                  <span
                    [class]="getSeverityBadgeClass(log.severity)"
                    class="px-2 py-0.5 text-xs font-medium rounded ml-2"
                  >
                    {{ log.severity }}
                  </span>
                </div>

                <div *ngIf="log.ipAddress" class="mt-2 text-xs text-gray-500">
                  IP: {{ log.ipAddress }}
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Pagination -->
        <div *ngIf="totalPages > 1" class="p-4 border-t border-gray-200 flex items-center justify-between">
          <p class="text-sm text-gray-600">
            Showing {{ (page - 1) * pageSize + 1 }} to {{ Math.min(page * pageSize, totalCount) }} of {{ totalCount }}
          </p>
          <div class="flex gap-2">
            <button
              (click)="previousPage()"
              [disabled]="page === 1"
              class="px-3 py-1 border border-gray-300 rounded text-sm disabled:opacity-50"
            >
              Previous
            </button>
            <button
              (click)="nextPage()"
              [disabled]="page === totalPages"
              class="px-3 py-1 border border-gray-300 rounded text-sm disabled:opacity-50"
            >
              Next
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Cleanup Modal -->
    <div
      *ngIf="showCleanupModal"
      class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50"
      (click)="showCleanupModal = false"
    >
      <div
        class="bg-white rounded-lg p-6 max-w-md w-full mx-4"
        (click)="$event.stopPropagation()"
      >
        <h3 class="text-lg font-semibold text-gray-900 mb-4">Cleanup Old Logs</h3>
        <p class="text-sm text-gray-600 mb-4">Delete activity logs older than the specified number of days:</p>

        <div class="mb-4">
          <label class="block text-sm font-medium text-gray-700 mb-2">Days to keep</label>
          <input
            type="number"
            [(ngModel)]="cleanupDays"
            min="1"
            max="365"
            class="w-full px-3 py-2 border border-gray-300 rounded-lg"
          />
          <p class="mt-1 text-xs text-gray-500">Logs older than {{ cleanupDays }} days will be deleted</p>
        </div>

        <div class="flex items-center gap-2 justify-end">
          <button
            (click)="showCleanupModal = false"
            class="px-4 py-2 text-gray-700 hover:text-gray-900 font-medium text-sm"
          >
            Cancel
          </button>
          <button
            (click)="confirmCleanup()"
            [disabled]="isCleaningUp"
            class="px-4 py-2 bg-red-600 hover:bg-red-700 text-white rounded-lg font-medium text-sm disabled:opacity-50"
          >
            {{ isCleaningUp ? 'Deleting...' : 'Delete Old Logs' }}
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class AdvancedActivityLogsComponent implements OnInit {
  private activityLogsService = inject(ActivityLogsService);

  logs: ActivityLog[] = [];
  statistics: ActivityStatistics | null = null;

  isLoading = false;
  isExporting = false;
  isCleaningUp = false;
  errorMessage = '';
  successMessage = '';

  // Pagination
  page = 1;
  pageSize = 20;
  totalCount = 0;
  totalPages = 0;

  // Filters
  searchTerm = '';
  actionFilter = '';
  severityFilter = '';
  entityTypeFilter = '';
  userIdFilter = '';

  // Cleanup
  showCleanupModal = false;
  cleanupDays = 90;

  Math = Math;

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loadLogs();
    this.loadStatistics();
  }

  loadLogs(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.activityLogsService.apiActivityLogsGet(
      this.userIdFilter || undefined,
      this.actionFilter || undefined,
      this.entityTypeFilter || undefined,
      this.severityFilter || undefined,
      undefined, // startDate
      undefined, // endDate
      this.searchTerm || undefined,
      this.page,
      this.pageSize
    ).subscribe({
      next: (response) => {
        this.logs = response.logs || [];
        this.totalCount = response.totalCount || 0;
        this.totalPages = response.totalPages || 0;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading logs:', error);
        this.errorMessage = 'Failed to load activity logs.';
        this.isLoading = false;
      }
    });
  }

  loadStatistics(): void {
    this.activityLogsService.apiActivityLogsStatisticsGet(undefined).subscribe({
      next: (stats) => {
        this.statistics = stats;
      },
      error: (error) => {
        console.error('Error loading statistics:', error);
      }
    });
  }

  applyFilters(): void {
    this.page = 1;
    this.loadLogs();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.actionFilter = '';
    this.severityFilter = '';
    this.entityTypeFilter = '';
    this.userIdFilter = '';
    this.page = 1;
    this.loadLogs();
  }

  previousPage(): void {
    if (this.page > 1) {
      this.page--;
      this.loadLogs();
    }
  }

  nextPage(): void {
    if (this.page < this.totalPages) {
      this.page++;
      this.loadLogs();
    }
  }

  exportLogs(): void {
    this.isExporting = true;
    this.errorMessage = '';

    this.activityLogsService.apiActivityLogsExportGet(
      this.userIdFilter || undefined,
      this.actionFilter || undefined,
      this.entityTypeFilter || undefined,
      this.severityFilter || undefined,
      undefined, // startDate
      undefined, // endDate
      this.searchTerm || undefined
    ).subscribe({
      next: (blob: any) => {
        const url = window.URL.createObjectURL(new Blob([blob]));
        const link = document.createElement('a');
        link.href = url;
        link.download = `activity-logs-${new Date().toISOString().split('T')[0]}.csv`;
        link.click();
        window.URL.revokeObjectURL(url);
        this.successMessage = 'Logs exported successfully!';
        this.isExporting = false;
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (error) => {
        console.error('Error exporting logs:', error);
        this.errorMessage = 'Failed to export logs.';
        this.isExporting = false;
      }
    });
  }

  confirmCleanup(): void {
    if (!confirm(`Are you sure you want to delete all logs older than ${this.cleanupDays} days? This action cannot be undone.`)) {
      return;
    }

    this.isCleaningUp = true;
    this.errorMessage = '';

    // Calculate the before date based on days to keep
    const beforeDate = new Date();
    beforeDate.setDate(beforeDate.getDate() - this.cleanupDays);

    this.activityLogsService.apiActivityLogsCleanupDelete(beforeDate.toISOString()).subscribe({
      next: (response) => {
        this.successMessage = `Successfully deleted ${response.deletedCount} old log(s)`;
        this.showCleanupModal = false;
        this.isCleaningUp = false;
        this.loadData();
        setTimeout(() => this.successMessage = '', 5000);
      },
      error: (error) => {
        console.error('Error cleaning up logs:', error);
        this.errorMessage = 'Failed to cleanup logs.';
        this.isCleaningUp = false;
      }
    });
  }

  getSeverityIconClass(severity: string): string {
    switch (severity) {
      case 'INFO':
        return 'bg-blue-500';
      case 'WARNING':
        return 'bg-yellow-500';
      case 'ERROR':
        return 'bg-red-500';
      case 'CRITICAL':
        return 'bg-purple-500';
      default:
        return 'bg-gray-500';
    }
  }

  getSeverityBadgeClass(severity: string): string {
    switch (severity) {
      case 'INFO':
        return 'bg-blue-100 text-blue-800';
      case 'WARNING':
        return 'bg-yellow-100 text-yellow-800';
      case 'ERROR':
        return 'bg-red-100 text-red-800';
      case 'CRITICAL':
        return 'bg-purple-100 text-purple-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  formatDateTime(dateString: string | null | undefined): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;

    return date.toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  formatActionName(action: string): string {
    return action.replace(/_/g, ' ').toLowerCase()
      .split(' ')
      .map(word => word.charAt(0).toUpperCase() + word.slice(1))
      .join(' ');
  }

  getTopItems(dict: { [key: string]: number }): Array<{key: string, value: number}> {
    return Object.entries(dict)
      .map(([key, value]) => ({ key, value }))
      .sort((a, b) => b.value - a.value)
      .slice(0, 5);
  }
}
