# GDPR Privacy Features - Implementation Patterns

This guide explains how to implement GDPR privacy features following the established codebase conventions.

## Quick Start Template

### 1. Create GDPR Models

**File**: `/app/core/models/gdpr.models.ts`

```typescript
// Request/Response DTOs
export interface DataExportRequest {
  userId: string;
  dataTypes: string[]; // 'profile', 'activities', 'documents', etc.
  format: 'json' | 'csv' | 'xml';
}

export interface DataExportResponse {
  exportId: string;
  userId: string;
  status: 'pending' | 'processing' | 'completed' | 'failed';
  url?: string;
  createdAt: Date;
  completedAt?: Date;
}

export interface DataDeleteRequest {
  userId: string;
  reason?: string;
  confirmations: {
    understand: boolean;
    permanent: boolean;
  };
}

export interface ConsentRequest {
  userId: string;
  type: 'marketing' | 'analytics' | 'profiling';
  granted: boolean;
  timestamp: Date;
}

// User-facing models
export interface GdprSettings {
  id: string;
  userId: string;
  dataExportConsent: boolean;
  analyticsConsent: boolean;
  marketingConsent: boolean;
  profilingConsent: boolean;
  lastUpdated: Date;
  lastExportDate?: Date;
}

export interface UpdateGdprSettingsDto {
  dataExportConsent?: boolean;
  analyticsConsent?: boolean;
  marketingConsent?: boolean;
  profilingConsent?: boolean;
}

export interface DataRequest {
  id: string;
  userId: string;
  type: 'export' | 'delete' | 'rectify';
  status: 'pending' | 'approved' | 'completed' | 'rejected';
  requestDate: Date;
  completionDate?: Date;
  reason?: string;
}
```

### 2. Create GDPR Service

**File**: `/app/core/services/gdpr.service.ts`

```typescript
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import {
  DataExportRequest,
  DataExportResponse,
  DataDeleteRequest,
  ConsentRequest,
  GdprSettings,
  UpdateGdprSettingsDto,
  DataRequest
} from '@core/models/gdpr.models';

@Injectable({
  providedIn: 'root'
})
export class GdprService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/api/gdpr`;

  // Data Export Endpoints
  requestDataExport(request: DataExportRequest): Observable<DataExportResponse> {
    return this.http.post<DataExportResponse>(`${this.apiUrl}/export/request`, request);
  }

  getDataExportStatus(exportId: string): Observable<DataExportResponse> {
    return this.http.get<DataExportResponse>(`${this.apiUrl}/export/${exportId}`);
  }

  downloadDataExport(exportId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/export/${exportId}/download`, {
      responseType: 'blob'
    });
  }

  // Data Deletion Endpoints
  requestDataDeletion(request: DataDeleteRequest): Observable<DataRequest> {
    return this.http.post<DataRequest>(`${this.apiUrl}/delete/request`, request);
  }

  confirmDataDeletion(requestId: string, confirmationCode: string): Observable<DataRequest> {
    return this.http.post<DataRequest>(`${this.apiUrl}/delete/${requestId}/confirm`, {
      confirmationCode
    });
  }

  cancelDataDeletion(requestId: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/delete/${requestId}/cancel`, {});
  }

  // Consent Management Endpoints
  grantConsent(request: ConsentRequest): Observable<ConsentRequest> {
    return this.http.post<ConsentRequest>(`${this.apiUrl}/consent`, request);
  }

  revokeConsent(type: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/consent/${type}`);
  }

  getConsentHistory(): Observable<ConsentRequest[]> {
    return this.http.get<ConsentRequest[]>(`${this.apiUrl}/consent/history`);
  }

  // GDPR Settings Endpoints
  getGdprSettings(): Observable<GdprSettings> {
    return this.http.get<GdprSettings>(`${this.apiUrl}/settings`);
  }

  updateGdprSettings(dto: UpdateGdprSettingsDto): Observable<GdprSettings> {
    return this.http.put<GdprSettings>(`${this.apiUrl}/settings`, dto);
  }

  // Data Requests Management (Admin only)
  getPendingDataRequests(page: number = 1, pageSize: number = 20): Observable<any> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<any>(`${this.apiUrl}/requests`, { params });
  }

  approveDataRequest(requestId: string): Observable<DataRequest> {
    return this.http.post<DataRequest>(`${this.apiUrl}/requests/${requestId}/approve`, {});
  }

  rejectDataRequest(requestId: string, reason: string): Observable<DataRequest> {
    return this.http.post<DataRequest>(`${this.apiUrl}/requests/${requestId}/reject`, { reason });
  }

  // Privacy Dashboard Data
  getPrivacyDashboard(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/dashboard`);
  }
}
```

### 3. Create Customer GDPR Settings Component

**File**: `/app/features/customer/settings/gdpr-settings.component.ts`

```typescript
import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { GdprService } from '@core/services/gdpr.service';
import { ToastService } from '@core/services/toast.service';
import { GdprSettings, UpdateGdprSettingsDto, DataExportRequest } from '@core/models/gdpr.models';

@Component({
  selector: 'app-gdpr-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './gdpr-settings.component.html',
  styleUrls: ['./gdpr-settings.component.scss']
})
export class GdprSettingsComponent implements OnInit {
  private readonly gdprService = inject(GdprService);
  private readonly toastService = inject(ToastService);

  gdprSettings: GdprSettings | null = null;
  loading = false;
  saving = false;
  exporting = false;
  deleting = false;

  // Form models
  consentForm: UpdateGdprSettingsDto = {};
  
  // Modal states
  showExportModal = false;
  showDeleteModal = false;
  showExportTypeModal = false;
  selectedExportTypes: string[] = [];
  deleteConfirmation = {
    understand: false,
    permanent: false
  };

  exportTypes = [
    { value: 'profile', label: 'Profile Information' },
    { value: 'activities', label: 'Activity Logs' },
    { value: 'documents', label: 'Documents' },
    { value: 'communications', label: 'Messages & Communications' },
    { value: 'transactions', label: 'Transaction History' }
  ];

  ngOnInit(): void {
    this.loadGdprSettings();
  }

  loadGdprSettings(): void {
    this.loading = true;
    this.gdprService.getGdprSettings().subscribe({
      next: (data) => {
        this.gdprSettings = data;
        this.initializeForm();
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading GDPR settings:', error);
        this.toastService.error('Error', 'Failed to load GDPR settings');
        this.loading = false;
      }
    });
  }

  initializeForm(): void {
    if (!this.gdprSettings) return;
    this.consentForm = {
      dataExportConsent: this.gdprSettings.dataExportConsent,
      analyticsConsent: this.gdprSettings.analyticsConsent,
      marketingConsent: this.gdprSettings.marketingConsent,
      profilingConsent: this.gdprSettings.profilingConsent
    };
  }

  updateConsents(): void {
    this.saving = true;
    this.gdprService.updateGdprSettings(this.consentForm).subscribe({
      next: (data) => {
        this.gdprSettings = data;
        this.toastService.success('Success', 'Consent settings updated successfully');
        this.saving = false;
      },
      error: (error) => {
        console.error('Error updating GDPR settings:', error);
        this.toastService.error('Error', 'Failed to update consent settings');
        this.saving = false;
      }
    });
  }

  openExportModal(): void {
    this.selectedExportTypes = [];
    this.showExportModal = true;
  }

  closeExportModal(): void {
    this.showExportModal = false;
  }

  requestDataExport(): void {
    if (this.selectedExportTypes.length === 0) {
      this.toastService.warning('Warning', 'Please select at least one data type');
      return;
    }

    this.exporting = true;
    const request: DataExportRequest = {
      userId: '', // Will be set by backend from auth token
      dataTypes: this.selectedExportTypes,
      format: 'json'
    };

    this.gdprService.requestDataExport(request).subscribe({
      next: (response) => {
        this.toastService.success('Success', 
          'Data export request submitted. You will receive an email when it is ready.');
        this.closeExportModal();
        this.exporting = false;
      },
      error: (error) => {
        console.error('Error requesting data export:', error);
        this.toastService.error('Error', 'Failed to request data export');
        this.exporting = false;
      }
    });
  }

  openDeleteModal(): void {
    this.deleteConfirmation = { understand: false, permanent: false };
    this.showDeleteModal = true;
  }

  closeDeleteModal(): void {
    this.showDeleteModal = false;
  }

  requestDataDeletion(): void {
    if (!this.deleteConfirmation.understand || !this.deleteConfirmation.permanent) {
      this.toastService.warning('Warning', 'Please confirm all deletions statements');
      return;
    }

    this.deleting = true;
    const request = {
      userId: '',
      reason: 'User requested account deletion',
      confirmations: this.deleteConfirmation
    };

    this.gdprService.requestDataDeletion(request).subscribe({
      next: (response) => {
        this.toastService.success('Success',
          'Account deletion request submitted. Please check your email to confirm.');
        this.closeDeleteModal();
        this.deleting = false;
      },
      error: (error) => {
        console.error('Error requesting data deletion:', error);
        this.toastService.error('Error', 'Failed to request account deletion');
        this.deleting = false;
      }
    });
  }

  toggleExportType(type: string): void {
    const index = this.selectedExportTypes.indexOf(type);
    if (index > -1) {
      this.selectedExportTypes.splice(index, 1);
    } else {
      this.selectedExportTypes.push(type);
    }
  }

  isExportTypeSelected(type: string): boolean {
    return this.selectedExportTypes.includes(type);
  }
}
```

### 4. Add Route to Customer Settings

**File**: `/app/features/customer/settings/customer-settings.component.ts` (Update existing)

```typescript
// In the component, add GDPR settings tab
activeTab: 'booking' | 'payment' | 'notifications' | 'display' | 'accessibility' | 'gdpr' = 'booking';

// And add to setTab method
setTab(tab: any): void {
  this.activeTab = tab;
}
```

### 5. Create Admin GDPR Management Component

**File**: `/app/features/admin/gdpr/gdpr-management.component.ts`

```typescript
import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { GdprService } from '@core/services/gdpr.service';
import { ToastService } from '@core/services/toast.service';

@Component({
  selector: 'app-gdpr-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './gdpr-management.component.html',
  styleUrls: ['./gdpr-management.component.scss']
})
export class GdprManagementComponent implements OnInit {
  private readonly gdprService = inject(GdprService);
  private readonly toastService = inject(ToastService);

  dataRequests: any[] = [];
  loading = false;
  processing = false;
  currentPage = 1;
  pageSize = 20;
  totalCount = 0;

  selectedRequest: any = null;
  showDetailModal = false;
  rejectionReason = '';

  ngOnInit(): void {
    this.loadDataRequests();
  }

  loadDataRequests(): void {
    this.loading = true;
    this.gdprService.getPendingDataRequests(this.currentPage, this.pageSize).subscribe({
      next: (response) => {
        this.dataRequests = response.items;
        this.totalCount = response.totalCount;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading data requests:', error);
        this.toastService.error('Error', 'Failed to load data requests');
        this.loading = false;
      }
    });
  }

  openDetail(request: any): void {
    this.selectedRequest = request;
    this.rejectionReason = '';
    this.showDetailModal = true;
  }

  closeDetailModal(): void {
    this.showDetailModal = false;
    this.selectedRequest = null;
  }

  approveRequest(): void {
    if (!this.selectedRequest) return;

    this.processing = true;
    this.gdprService.approveDataRequest(this.selectedRequest.id).subscribe({
      next: () => {
        this.toastService.success('Success', 'Data request approved');
        this.closeDetailModal();
        this.loadDataRequests();
        this.processing = false;
      },
      error: (error) => {
        console.error('Error approving request:', error);
        this.toastService.error('Error', 'Failed to approve request');
        this.processing = false;
      }
    });
  }

  rejectRequest(): void {
    if (!this.selectedRequest || !this.rejectionReason) {
      this.toastService.warning('Warning', 'Please provide a rejection reason');
      return;
    }

    this.processing = true;
    this.gdprService.rejectDataRequest(this.selectedRequest.id, this.rejectionReason).subscribe({
      next: () => {
        this.toastService.success('Success', 'Data request rejected');
        this.closeDetailModal();
        this.loadDataRequests();
        this.processing = false;
      },
      error: (error) => {
        console.error('Error rejecting request:', error);
        this.toastService.error('Error', 'Failed to reject request');
        this.processing = false;
      }
    });
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadDataRequests();
  }
}
```

### 6. Add Routes to app.routes.ts

```typescript
// In customer portal section
{
  path: 'gdpr-settings',
  loadComponent: () => import('./features/customer/settings/gdpr-settings.component')
    .then(m => m.GdprSettingsComponent)
}

// In admin portal section
{
  path: 'gdpr',
  loadComponent: () => import('./features/admin/gdpr/gdpr-management.component')
    .then(m => m.GdprManagementComponent)
}
```

### 7. Update Navigation

**File**: `/app/layout/customer-layout/customer-layout.component.ts` (Add to menuItems)
```typescript
menuItems = [
  // ... existing items
  { path: '/customer/gdpr-settings', icon: 'shield', label: 'Privacy & Data' },
  { path: '/customer/settings', icon: 'settings', label: 'Settings' }
];
```

**File**: `/app/layout/main-layout/main-layout.component.ts` (Add to navItems)
```typescript
navItems: NavItem[] = [
  // ... existing items
  {
    label: 'GDPR & Privacy',
    icon: 'shield',
    route: '/gdpr'
  }
];
```

## Key Patterns Used

1. **Service Pattern**: Centralized `GdprService` in `/core/services`
2. **Model Pattern**: Type-safe DTOs in `/core/models/gdpr.models.ts`
3. **Component Pattern**: Standalone components with dependency injection
4. **Error Handling**: Consistent toast notifications and error logging
5. **State Management**: Loading/saving flags for async operations
6. **Authentication**: Leverages existing auth guards and JWT tokens
7. **API Integration**: RESTful endpoints with observable returns

## Testing Checklist

- [ ] Service methods return correct Observable types
- [ ] Components handle loading/error states
- [ ] Guards protect GDPR routes appropriately
- [ ] Toast notifications work for success/error
- [ ] Forms reset after successful submission
- [ ] API calls include proper auth tokens
- [ ] Models match backend contracts
- [ ] Navigation links added to layouts

