import { Component, Input, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { JobBidsService } from '@core/services/job-bids.service';
import { JobBid } from '@core/models/job-bid.models';
import { ToastService } from '@core/services/toast.service';

@Component({
  selector: 'app-job-bids-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="bids-container">
      <!-- Header -->
      <div class="bids-header">
        <div class="flex items-center justify-between">
          <div>
            <h3 class="text-lg font-semibold text-gray-900">Driver Bids</h3>
            <p class="text-sm text-gray-600 mt-1">
              {{ getPendingBidsCount() }} pending bid{{ getPendingBidsCount() !== 1 ? 's' : '' }}
            </p>
          </div>
          <button
            (click)="refreshBids()"
            class="btn-secondary btn-sm"
            [disabled]="isLoading">
            <svg class="w-4 h-4" [class.animate-spin]="isLoading" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
            </svg>
            <span class="ml-2">Refresh</span>
          </button>
        </div>
      </div>

      <!-- Loading State -->
      <div *ngIf="isLoading && bids.length === 0" class="text-center py-12">
        <div class="inline-block w-12 h-12 border-4 border-gray-300 border-t-blue-600 rounded-full animate-spin"></div>
        <p class="mt-4 text-gray-600">Loading bids...</p>
      </div>

      <!-- Error State -->
      <div *ngIf="errorMessage && !isLoading" class="alert alert-error">
        {{ errorMessage }}
      </div>

      <!-- Empty State -->
      <div *ngIf="!isLoading && bids.length === 0 && !errorMessage" class="empty-state">
        <svg class="w-16 h-16 mx-auto text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
        </svg>
        <h4 class="mt-4 text-lg font-medium text-gray-900">No bids yet</h4>
        <p class="mt-2 text-gray-600">Drivers will submit bids for this job. Check back soon!</p>
      </div>

      <!-- Bids List -->
      <div *ngIf="!isLoading && bids.length > 0" class="bids-list">
        <div
          *ngFor="let bid of sortedBids(); trackBy: trackByBidId"
          class="bid-card"
          [class.bid-accepted]="bid.status === 'accepted'"
          [class.bid-rejected]="bid.status === 'rejected'">

          <div class="bid-header">
            <div class="flex items-start justify-between">
              <div class="flex items-center gap-3">
                <!-- Driver Avatar -->
                <div class="driver-avatar">
                  {{ getDriverInitials(bid.driverName) }}
                </div>

                <!-- Driver Info -->
                <div>
                  <h4 class="font-semibold text-gray-900">{{ bid.driverName }}</h4>
                  <div class="flex items-center gap-2 mt-1">
                    <div class="flex items-center">
                      <svg class="w-4 h-4 text-yellow-400" fill="currentColor" viewBox="0 0 20 20">
                        <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                      </svg>
                      <span class="ml-1 text-sm text-gray-700">{{ bid.driverRating?.toFixed(1) || 'N/A' }}</span>
                    </div>
                    <span class="text-gray-400">•</span>
                    <span class="text-sm text-gray-600">{{ formatEstimatedDuration(bid.estimatedDuration) }}</span>
                  </div>
                </div>
              </div>

              <!-- Bid Amount -->
              <div class="text-right">
                <div class="bid-amount">\${{ bid.bidAmount.toFixed(2) }}</div>
                <span class="bid-status" [ngClass]="getStatusClass(bid.status)">
                  {{ formatStatus(bid.status) }}
                </span>
              </div>
            </div>
          </div>

          <!-- Bid Message -->
          <div *ngIf="bid.message" class="bid-message">
            <p class="text-sm text-gray-700">{{ bid.message }}</p>
          </div>

          <!-- Bid Metadata -->
          <div class="bid-metadata">
            <div class="flex items-center justify-between text-xs text-gray-500">
              <span>Submitted {{ formatDate(bid.createdAt) }}</span>
              <span *ngIf="bid.status === 'pending'">Expires {{ formatDate(bid.expiresAt) }}</span>
            </div>
          </div>

          <!-- Actions -->
          <div *ngIf="bid.status === 'pending'" class="bid-actions">
            <button
              (click)="acceptBid(bid)"
              [disabled]="isProcessing"
              class="btn btn-primary btn-sm">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
              </svg>
              <span class="ml-2">Accept Bid</span>
            </button>

            <button
              (click)="openRejectModal(bid)"
              [disabled]="isProcessing"
              class="btn btn-outline btn-sm">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
              <span class="ml-2">Reject</span>
            </button>
          </div>

          <!-- Response Message (for accepted/rejected bids) -->
          <div *ngIf="bid.status !== 'pending'" class="bid-response">
            <div class="flex items-start gap-2">
              <svg *ngIf="bid.status === 'accepted'" class="w-5 h-5 text-green-600 flex-shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <svg *ngIf="bid.status === 'rejected'" class="w-5 h-5 text-red-600 flex-shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <div class="flex-1">
                <p class="text-sm font-medium" [class.text-green-800]="bid.status === 'accepted'" [class.text-red-800]="bid.status === 'rejected'">
                  {{ bid.status === 'accepted' ? 'Bid Accepted' : 'Bid Rejected' }}
                </p>
                <p class="text-xs text-gray-600 mt-1">
                  {{ bid.status === 'accepted' ? formatDate(bid.acceptedAt!) : formatDate(bid.rejectedAt!) }}
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Reject Modal -->
      <div *ngIf="showRejectModal" class="modal-overlay" (click)="closeRejectModal()">
        <div class="modal-content" (click)="$event.stopPropagation()">
          <div class="modal-header">
            <h3 class="text-lg font-semibold text-gray-900">Reject Bid</h3>
            <button (click)="closeRejectModal()" class="btn-close">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <div class="modal-body">
            <p class="text-sm text-gray-600 mb-4">
              Are you sure you want to reject the bid from <strong>{{ selectedBid?.driverName }}</strong> for <strong>\${{ selectedBid?.bidAmount?.toFixed(2) }}</strong>?
            </p>

            <div class="form-group">
              <label class="form-label">Reason (Optional)</label>
              <textarea
                [(ngModel)]="rejectReason"
                class="form-control"
                rows="3"
                placeholder="Let the driver know why you're rejecting their bid..."></textarea>
            </div>
          </div>

          <div class="modal-footer">
            <button (click)="closeRejectModal()" class="btn btn-outline" [disabled]="isProcessing">
              Cancel
            </button>
            <button (click)="confirmReject()" class="btn btn-danger" [disabled]="isProcessing">
              {{ isProcessing ? 'Rejecting...' : 'Reject Bid' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .bids-container {
      background: white;
      border-radius: 0.75rem;
      border: 1px solid #e5e7eb;
      overflow: hidden;
    }

    .bids-header {
      padding: 1.5rem;
      border-bottom: 1px solid #e5e7eb;
      background: linear-gradient(to bottom, #f9fafb, #ffffff);
    }

    .bids-list {
      padding: 1rem;
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .bid-card {
      border: 2px solid #e5e7eb;
      border-radius: 0.75rem;
      padding: 1.25rem;
      transition: all 0.2s;
      background: white;
    }

    .bid-card:hover {
      border-color: #3b82f6;
      box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
    }

    .bid-accepted {
      border-color: #10b981;
      background: #f0fdf4;
    }

    .bid-rejected {
      border-color: #ef4444;
      background: #fef2f2;
      opacity: 0.7;
    }

    .bid-header {
      margin-bottom: 1rem;
    }

    .driver-avatar {
      width: 3rem;
      height: 3rem;
      border-radius: 50%;
      background: linear-gradient(135deg, #3b82f6 0%, #8b5cf6 100%);
      display: flex;
      align-items: center;
      justify-content: center;
      color: white;
      font-weight: 600;
      font-size: 1.125rem;
    }

    .bid-amount {
      font-size: 1.875rem;
      font-weight: 700;
      color: #1f2937;
      line-height: 1;
    }

    .bid-status {
      display: inline-block;
      margin-top: 0.25rem;
      padding: 0.25rem 0.75rem;
      border-radius: 9999px;
      font-size: 0.75rem;
      font-weight: 600;
    }

    .status-pending {
      background: #fef3c7;
      color: #92400e;
    }

    .status-accepted {
      background: #d1fae5;
      color: #065f46;
    }

    .status-rejected {
      background: #fee2e2;
      color: #991b1b;
    }

    .status-withdrawn {
      background: #f3f4f6;
      color: #6b7280;
    }

    .bid-message {
      padding: 1rem;
      background: #f9fafb;
      border-radius: 0.5rem;
      margin-bottom: 1rem;
      border-left: 3px solid #3b82f6;
    }

    .bid-metadata {
      padding-top: 0.75rem;
      border-top: 1px solid #e5e7eb;
      margin-bottom: 1rem;
    }

    .bid-actions {
      display: flex;
      gap: 0.75rem;
      padding-top: 1rem;
      border-top: 1px solid #e5e7eb;
    }

    .bid-response {
      padding: 0.75rem;
      background: #f9fafb;
      border-radius: 0.5rem;
      margin-top: 1rem;
    }

    .empty-state {
      text-align: center;
      padding: 3rem 1.5rem;
    }

    .alert {
      padding: 1rem;
      border-radius: 0.5rem;
      margin: 1rem;
    }

    .alert-error {
      background: #fef2f2;
      color: #991b1b;
      border: 1px solid #fee2e2;
    }

    /* Modal Styles */
    .modal-overlay {
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.5);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
      padding: 1rem;
    }

    .modal-content {
      background: white;
      border-radius: 0.75rem;
      max-width: 32rem;
      width: 100%;
      box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1);
    }

    .modal-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 1.5rem;
      border-bottom: 1px solid #e5e7eb;
    }

    .modal-body {
      padding: 1.5rem;
    }

    .modal-footer {
      display: flex;
      gap: 0.75rem;
      justify-content: flex-end;
      padding: 1.5rem;
      border-top: 1px solid #e5e7eb;
      background: #f9fafb;
    }

    .btn {
      display: inline-flex;
      align-items: center;
      padding: 0.5rem 1rem;
      border-radius: 0.5rem;
      font-weight: 500;
      font-size: 0.875rem;
      transition: all 0.2s;
      cursor: pointer;
      border: none;
    }

    .btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .btn-sm {
      padding: 0.375rem 0.75rem;
      font-size: 0.813rem;
    }

    .btn-primary {
      background: #3b82f6;
      color: white;
    }

    .btn-primary:hover:not(:disabled) {
      background: #2563eb;
    }

    .btn-danger {
      background: #ef4444;
      color: white;
    }

    .btn-danger:hover:not(:disabled) {
      background: #dc2626;
    }

    .btn-outline {
      background: white;
      color: #374151;
      border: 1px solid #d1d5db;
    }

    .btn-outline:hover:not(:disabled) {
      background: #f9fafb;
    }

    .btn-secondary {
      background: #f3f4f6;
      color: #374151;
    }

    .btn-secondary:hover:not(:disabled) {
      background: #e5e7eb;
    }

    .btn-close {
      background: none;
      border: none;
      color: #6b7280;
      cursor: pointer;
      padding: 0.25rem;
    }

    .btn-close:hover {
      color: #374151;
    }

    .form-group {
      margin-bottom: 1rem;
    }

    .form-label {
      display: block;
      font-size: 0.875rem;
      font-weight: 500;
      color: #374151;
      margin-bottom: 0.5rem;
    }

    .form-control {
      width: 100%;
      padding: 0.5rem 0.75rem;
      border: 1px solid #d1d5db;
      border-radius: 0.5rem;
      font-size: 0.875rem;
      transition: all 0.2s;
    }

    .form-control:focus {
      outline: none;
      border-color: #3b82f6;
      box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
    }
  `]
})
export class JobBidsListComponent implements OnInit {
  private jobBidsService = inject(JobBidsService);
  private toastService = inject(ToastService);

  @Input() jobId!: string;
  @Input() jobStatus?: string;

  bids: JobBid[] = [];
  isLoading = false;
  errorMessage = '';
  isProcessing = false;

  // Reject modal
  showRejectModal = false;
  selectedBid: JobBid | null = null;
  rejectReason = '';

  ngOnInit(): void {
    if (this.jobId) {
      this.loadBids();
    }
  }

  loadBids(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.jobBidsService.getBidsForJob(this.jobId).subscribe({
      next: (bids) => {
        this.bids = bids;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading bids:', error);
        this.errorMessage = 'Failed to load bids. Please try again.';
        this.isLoading = false;
      }
    });
  }

  refreshBids(): void {
    this.loadBids();
  }

  acceptBid(bid: JobBid): void {
    if (!confirm(`Accept bid from ${bid.driverName} for $${bid.bidAmount.toFixed(2)}?\n\nThis will assign the job to this driver and reject all other pending bids.`)) {
      return;
    }

    this.isProcessing = true;

    this.jobBidsService.acceptBid(bid.id).subscribe({
      next: () => {
        this.toastService.success(
          'Bid Accepted',
          `${bid.driverName} has been assigned to your job. You'll receive a notification when they confirm.`
        );
        this.isProcessing = false;
        this.loadBids();
      },
      error: (error) => {
        console.error('Error accepting bid:', error);
        this.toastService.error(
          'Failed to Accept Bid',
          error.error?.title || 'Please try again.'
        );
        this.isProcessing = false;
      }
    });
  }

  openRejectModal(bid: JobBid): void {
    this.selectedBid = bid;
    this.rejectReason = '';
    this.showRejectModal = true;
  }

  closeRejectModal(): void {
    this.showRejectModal = false;
    this.selectedBid = null;
    this.rejectReason = '';
  }

  confirmReject(): void {
    if (!this.selectedBid) return;

    this.isProcessing = true;

    this.jobBidsService.rejectBid(this.selectedBid.id).subscribe({
      next: () => {
        this.toastService.success(
          'Bid Rejected',
          `You've rejected the bid from ${this.selectedBid!.driverName}.`
        );
        this.isProcessing = false;
        this.closeRejectModal();
        this.loadBids();
      },
      error: (error) => {
        console.error('Error rejecting bid:', error);
        this.toastService.error(
          'Failed to Reject Bid',
          error.error?.title || 'Please try again.'
        );
        this.isProcessing = false;
      }
    });
  }

  sortedBids(): JobBid[] {
    // Sort: pending first, then by amount (lowest first), then by date
    return [...this.bids].sort((a, b) => {
      // Pending bids first
      if (a.status === 'pending' && b.status !== 'pending') return -1;
      if (a.status !== 'pending' && b.status === 'pending') return 1;

      // Then by amount (lowest first for pending)
      if (a.status === 'pending' && b.status === 'pending') {
        return a.bidAmount - b.bidAmount;
      }

      // Then by date (newest first)
      return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
    });
  }

  getPendingBidsCount(): number {
    return this.bids.filter(bid => bid.status === 'pending').length;
  }

  getDriverInitials(name: string): string {
    if (!name) return '?';
    const parts = name.split(' ');
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.substring(0, 1).toUpperCase();
  }

  formatEstimatedDuration(minutes: number | null | undefined): string {
    if (!minutes) return 'N/A';
    if (minutes < 60) return `${minutes} min`;
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    return mins > 0 ? `${hours}h ${mins}m` : `${hours}h`;
  }

  formatStatus(status: string): string {
    return status.charAt(0).toUpperCase() + status.slice(1);
  }

  getStatusClass(status: string): string {
    return `status-${status}`;
  }

  formatDate(date: Date | string | undefined): string {
    if (!date) return 'N/A';
    const d = new Date(date);
    const now = new Date();
    const diffMs = now.getTime() - d.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMins < 1) return 'just now';
    if (diffMins < 60) return `${diffMins} min ago`;
    if (diffHours < 24) return `${diffHours} hour${diffHours !== 1 ? 's' : ''} ago`;
    if (diffDays < 7) return `${diffDays} day${diffDays !== 1 ? 's' : ''} ago`;

    return d.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: d.getFullYear() !== now.getFullYear() ? 'numeric' : undefined
    });
  }

  trackByBidId(index: number, bid: JobBid): string {
    return bid.id;
  }
}
