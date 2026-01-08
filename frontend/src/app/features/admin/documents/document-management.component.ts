import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  DocumentsService,
  DocumentViewModel,
  DocumentStatisticsViewModel,
  DocumentListViewModel,
  RejectDocumentModel
} from '@core/api';

@Component({
  selector: 'app-document-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="min-h-screen bg-gray-50 py-8">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <!-- Page Header -->
        <div class="page-header">
          <div>
            <h1>Document Management</h1>
            <p class="subtitle">Review and manage driver documents</p>
          </div>
        </div>

      <!-- Loading State -->
      <div *ngIf="isLoading" class="text-center py-12">
        <div class="inline-block w-12 h-12 border-4 border-gray-300 border-t-blue-600 rounded-full animate-spin"></div>
        <p class="mt-4 text-gray-600">Loading documents...</p>
      </div>

      <!-- Error State -->
      <div *ngIf="errorMessage && !isLoading" class="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
        <p class="text-red-800">{{ errorMessage }}</p>
      </div>

      <!-- Success Message -->
      <div *ngIf="successMessage" class="mb-6 bg-green-50 border border-green-200 rounded-lg p-4">
        <p class="text-green-800">{{ successMessage }}</p>
      </div>

      <div *ngIf="!isLoading">
        <!-- Statistics Dashboard -->
        <div class="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-6 gap-4 mb-8">
          <div class="bg-white border border-gray-200 rounded-lg p-4">
            <p class="text-xs text-gray-600 mb-1">Total Documents</p>
            <p class="text-2xl font-bold text-gray-900">{{ statistics?.totalDocuments || 0 }}</p>
          </div>
          <div class="bg-orange-50 border border-orange-200 rounded-lg p-4">
            <p class="text-xs text-orange-600 mb-1">Pending</p>
            <p class="text-2xl font-bold text-orange-600">{{ statistics?.pendingVerification || 0 }}</p>
          </div>
          <div class="bg-green-50 border border-green-200 rounded-lg p-4">
            <p class="text-xs text-green-600 mb-1">Verified</p>
            <p class="text-2xl font-bold text-green-600">{{ statistics?.verified || 0 }}</p>
          </div>
          <div class="bg-red-50 border border-red-200 rounded-lg p-4">
            <p class="text-xs text-red-600 mb-1">Rejected</p>
            <p class="text-2xl font-bold text-red-600">{{ statistics?.rejected || 0 }}</p>
          </div>
          <div class="bg-gray-50 border border-gray-200 rounded-lg p-4">
            <p class="text-xs text-gray-600 mb-1">Expired</p>
            <p class="text-2xl font-bold text-gray-900">{{ statistics?.expired || 0 }}</p>
          </div>
          <div class="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
            <p class="text-xs text-yellow-700 mb-1">Expiring Soon</p>
            <p class="text-2xl font-bold text-yellow-700">{{ statistics?.expiringSoon || 0 }}</p>
          </div>
        </div>

        <!-- Tabs -->
        <div class="mb-6 border-b border-gray-200">
          <nav class="-mb-px flex space-x-8">
            <button
              (click)="activeTab = 'pending'"
              [class]="activeTab === 'pending'
                ? 'border-blue-500 text-blue-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'"
              class="whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm"
            >
              Pending Verification ({{ statistics?.pendingVerification || 0 }})
            </button>
            <button
              (click)="activeTab = 'expiring'"
              [class]="activeTab === 'expiring'
                ? 'border-yellow-500 text-yellow-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'"
              class="whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm"
            >
              Expiring Soon ({{ statistics?.expiringSoon || 0 }})
            </button>
          </nav>
        </div>

        <!-- Pending Documents -->
        <div *ngIf="activeTab === 'pending'">
          <div *ngIf="pendingDocuments.length === 0" class="text-center py-12">
            <svg class="w-24 h-24 mx-auto text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            </svg>
            <h3 class="mt-4 text-lg font-medium text-gray-900">No pending documents</h3>
            <p class="mt-2 text-gray-600">All documents have been reviewed</p>
          </div>

          <div *ngIf="pendingDocuments.length > 0" class="space-y-4">
            <div
              *ngFor="let doc of pendingDocuments"
              class="bg-white border border-gray-200 rounded-lg p-6 hover:shadow-md transition-shadow"
            >
              <div class="flex items-start justify-between mb-4">
                <div class="flex-1">
                  <div class="flex items-center gap-3 mb-2">
                    <h3 class="text-lg font-semibold text-gray-900">{{ doc.type }}</h3>
                    <span class="px-2 py-0.5 bg-orange-100 text-orange-800 text-xs font-medium rounded">
                      Pending
                    </span>
                  </div>
                  <div class="space-y-1 text-sm text-gray-600">
                    <p *ngIf="doc.driver?.firstName || doc.driver?.lastName">
                      Driver: <span class="font-medium text-gray-900">{{ getDriverName(doc.driver) }}</span>
                    </p>
                    <p *ngIf="doc.fileName">File: <span class="font-mono text-gray-900">{{ doc.fileName }}</span></p>
                    <p>Uploaded: {{ formatDate(doc.uploadedDate) }}</p>
                    <p *ngIf="doc.expiryDate">Expires: {{ formatDate(doc.expiryDate) }}</p>
                  </div>
                </div>
              </div>

              <!-- Actions -->
              <div class="flex items-center gap-2 pt-4 border-t border-gray-200">
                <button
                  (click)="viewDocument(doc)"
                  class="px-4 py-2 text-blue-600 hover:text-blue-700 rounded-lg font-medium text-sm transition-colors"
                >
                  View Document
                </button>
                <button
                  (click)="verifyDocument(doc)"
                  [disabled]="isProcessing"
                  class="px-4 py-2 bg-green-600 hover:bg-green-700 text-white rounded-lg font-medium text-sm transition-colors disabled:opacity-50"
                >
                  Verify
                </button>
                <button
                  (click)="openRejectModal(doc)"
                  [disabled]="isProcessing"
                  class="px-4 py-2 bg-red-600 hover:bg-red-700 text-white rounded-lg font-medium text-sm transition-colors disabled:opacity-50"
                >
                  Reject
                </button>
              </div>
            </div>

            <!-- Pagination -->
            <div *ngIf="totalPages > 1" class="flex items-center justify-between mt-6">
              <p class="text-sm text-gray-600">
                Showing {{ (currentPage - 1) * pageSize + 1 }} to {{ Math.min(currentPage * pageSize, totalCount) }} of {{ totalCount }} documents
              </p>
              <div class="flex gap-2">
                <button
                  (click)="loadPendingDocuments(currentPage - 1)"
                  [disabled]="currentPage === 1"
                  class="px-4 py-2 border border-gray-300 rounded-lg text-sm font-medium disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  Previous
                </button>
                <button
                  (click)="loadPendingDocuments(currentPage + 1)"
                  [disabled]="currentPage === totalPages"
                  class="px-4 py-2 border border-gray-300 rounded-lg text-sm font-medium disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  Next
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Expiring Documents -->
        <div *ngIf="activeTab === 'expiring'">
          <div class="mb-4">
            <label class="text-sm font-medium text-gray-700">Show documents expiring within:</label>
            <select
              [(ngModel)]="daysAhead"
              (change)="loadExpiringDocuments()"
              class="ml-2 px-3 py-2 border border-gray-300 rounded-lg"
            >
              <option [value]="7">7 days</option>
              <option [value]="14">14 days</option>
              <option [value]="30">30 days</option>
              <option [value]="60">60 days</option>
              <option [value]="90">90 days</option>
            </select>
          </div>

          <div *ngIf="expiringDocuments.length === 0" class="text-center py-12">
            <svg class="w-24 h-24 mx-auto text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <h3 class="mt-4 text-lg font-medium text-gray-900">No expiring documents</h3>
            <p class="mt-2 text-gray-600">All documents are valid for the selected period</p>
          </div>

          <div *ngIf="expiringDocuments.length > 0" class="space-y-4">
            <div
              *ngFor="let doc of expiringDocuments"
              class="bg-white border border-yellow-200 rounded-lg p-6"
            >
              <div class="flex items-start justify-between mb-4">
                <div class="flex-1">
                  <div class="flex items-center gap-3 mb-2">
                    <h3 class="text-lg font-semibold text-gray-900">{{ doc.type }}</h3>
                    <span
                      [class]="getExpiryStatusClass(doc.expiryDate)"
                      class="px-2 py-0.5 text-xs font-medium rounded"
                    >
                      {{ getExpiryStatus(doc.expiryDate) }}
                    </span>
                  </div>
                  <div class="space-y-1 text-sm text-gray-600">
                    <p *ngIf="doc.driver?.firstName || doc.driver?.lastName">
                      Driver: <span class="font-medium text-gray-900">{{ getDriverName(doc.driver) }}</span>
                    </p>
                    <p *ngIf="doc.driver?.email">Email: <span class="text-gray-900">{{ doc.driver?.email }}</span></p>
                    <p *ngIf="doc.driver?.phoneNumber">Phone: <span class="text-gray-900">{{ doc.driver?.phoneNumber }}</span></p>
                    <p class="font-medium" [class.text-red-600]="isExpiringSoon(doc.expiryDate)">
                      Expires: {{ formatDate(doc.expiryDate) }} ({{ getDaysUntilExpiry(doc.expiryDate) }} days)
                    </p>
                  </div>
                </div>
              </div>

              <div class="flex items-center gap-2 pt-4 border-t border-gray-200">
                <button
                  (click)="viewDocument(doc)"
                  class="px-4 py-2 text-blue-600 hover:text-blue-700 rounded-lg font-medium text-sm transition-colors"
                >
                  View Document
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
      </div>
    </div>

    <!-- Reject Modal -->
    <div
      *ngIf="showRejectModal"
      class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50"
      (click)="closeRejectModal()"
    >
      <div
        class="bg-white rounded-lg p-6 max-w-md w-full mx-4"
        (click)="$event.stopPropagation()"
      >
        <h3 class="text-lg font-semibold text-gray-900 mb-4">Reject Document</h3>
        <p class="text-sm text-gray-600 mb-4">Please provide a reason for rejecting this document:</p>
        <textarea
          [(ngModel)]="rejectReason"
          rows="4"
          placeholder="Reason for rejection..."
          class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-red-500 focus:border-red-500 mb-4"
        ></textarea>
        <div class="flex items-center gap-2 justify-end">
          <button
            (click)="closeRejectModal()"
            class="px-4 py-2 text-gray-700 hover:text-gray-900 font-medium text-sm"
          >
            Cancel
          </button>
          <button
            (click)="confirmReject()"
            [disabled]="!rejectReason || isProcessing"
            class="px-4 py-2 bg-red-600 hover:bg-red-700 text-white rounded-lg font-medium text-sm transition-colors disabled:opacity-50"
          >
            {{ isProcessing ? 'Rejecting...' : 'Reject Document' }}
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
export class DocumentManagementComponent implements OnInit {
  private documentsService = inject(DocumentsService);

  statistics: DocumentStatisticsViewModel | null = null;
  pendingDocuments: DocumentViewModel[] = [];
  expiringDocuments: DocumentViewModel[] = [];

  isLoading = false;
  errorMessage = '';
  successMessage = '';
  isProcessing = false;

  activeTab: 'pending' | 'expiring' = 'pending';

  // Pagination
  currentPage = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;

  // Expiring documents filter
  daysAhead = 30;

  // Reject modal
  showRejectModal = false;
  selectedDocument: DocumentViewModel | null = null;
  rejectReason = '';

  Math = Math;

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loadStatistics();
    this.loadPendingDocuments();
    this.loadExpiringDocuments();
  }

  loadStatistics(): void {
    this.documentsService.apiDocumentsStatisticsGet().subscribe({
      next: (stats: DocumentStatisticsViewModel) => {
        this.statistics = stats;
      },
      error: (error: any) => {
        console.error('Error loading statistics:', error);
      }
    });
  }

  loadPendingDocuments(page: number = 1): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.currentPage = page;

    this.documentsService.apiDocumentsPendingGet(page, this.pageSize).subscribe({
      next: (result: DocumentListViewModel) => {
        this.pendingDocuments = result.documents || [];
        this.totalCount = result.totalCount || 0;
        this.totalPages = result.totalPages || 0;
        this.isLoading = false;
      },
      error: (error: any) => {
        console.error('Error loading pending documents:', error);
        this.errorMessage = 'Failed to load pending documents.';
        this.isLoading = false;
      }
    });
  }

  loadExpiringDocuments(): void {
    this.documentsService.apiDocumentsExpiringGet(this.daysAhead).subscribe({
      next: (documents: DocumentViewModel[]) => {
        this.expiringDocuments = documents || [];
      },
      error: (error: any) => {
        console.error('Error loading expiring documents:', error);
        this.errorMessage = 'Failed to load expiring documents.';
      }
    });
  }

  verifyDocument(doc: DocumentViewModel): void {
    if (!doc.id) return;

    this.isProcessing = true;
    this.errorMessage = '';

    this.documentsService.apiDocumentsIdVerifyPost(doc.id).subscribe({
      next: () => {
        this.successMessage = `Document verified successfully`;
        this.isProcessing = false;
        this.loadData();
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (error: any) => {
        console.error('Error verifying document:', error);
        this.errorMessage = 'Failed to verify document. Please try again.';
        this.isProcessing = false;
      }
    });
  }

  openRejectModal(doc: DocumentViewModel): void {
    this.selectedDocument = doc;
    this.rejectReason = '';
    this.showRejectModal = true;
  }

  closeRejectModal(): void {
    this.showRejectModal = false;
    this.selectedDocument = null;
    this.rejectReason = '';
  }

  confirmReject(): void {
    if (!this.selectedDocument?.id || !this.rejectReason) return;

    this.isProcessing = true;
    this.errorMessage = '';

    const rejectModel: RejectDocumentModel = {
      reason: this.rejectReason
    };

    this.documentsService.apiDocumentsIdRejectPost(this.selectedDocument.id, rejectModel).subscribe({
      next: () => {
        this.successMessage = `Document rejected successfully`;
        this.isProcessing = false;
        this.closeRejectModal();
        this.loadData();
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (error: any) => {
        console.error('Error rejecting document:', error);
        this.errorMessage = 'Failed to reject document. Please try again.';
        this.isProcessing = false;
      }
    });
  }

  viewDocument(doc: DocumentViewModel): void {
    if (doc.fileUrl) {
      window.open(doc.fileUrl, '_blank');
    }
  }

  getDaysUntilExpiry(expiryDate: string | null | undefined): number {
    if (!expiryDate) return 0;
    const today = new Date();
    const expiry = new Date(expiryDate);
    const diffTime = expiry.getTime() - today.getTime();
    return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
  }

  isExpiringSoon(expiryDate: string | null | undefined): boolean {
    const days = this.getDaysUntilExpiry(expiryDate);
    return days <= 14 && days >= 0;
  }

  getExpiryStatus(expiryDate: string | null | undefined): string {
    const days = this.getDaysUntilExpiry(expiryDate);
    if (days < 0) return 'Expired';
    if (days <= 7) return `${days} days left`;
    if (days <= 14) return `${days} days left`;
    return `${days} days left`;
  }

  getExpiryStatusClass(expiryDate: string | null | undefined): string {
    const days = this.getDaysUntilExpiry(expiryDate);
    if (days < 0) return 'bg-red-100 text-red-800';
    if (days <= 7) return 'bg-red-100 text-red-800';
    if (days <= 14) return 'bg-orange-100 text-orange-800';
    return 'bg-yellow-100 text-yellow-800';
  }

  formatDate(dateString: string | null | undefined): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }

  getDriverName(driver: any): string {
    if (!driver) return '';
    const firstName = driver.firstName || '';
    const lastName = driver.lastName || '';
    return `${firstName} ${lastName}`.trim();
  }
}
