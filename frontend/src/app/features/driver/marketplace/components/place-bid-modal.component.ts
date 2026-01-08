import { Component, Input, Output, EventEmitter, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { JobDto } from '@core/api';
import { JobBidsService } from '@core/services/job-bids.service';
import { CreateBidRequest } from '@core/models/job-bid.models';
import { ToastService } from '@core/services/toast.service';

@Component({
  selector: 'app-place-bid-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div *ngIf="isOpen" class="modal-overlay" (click)="onClose()">
      <div class="modal-content" (click)="$event.stopPropagation()">
        <!-- Modal Header -->
        <div class="modal-header">
          <div>
            <h2>Place Bid on Job</h2>
            <p class="text-sm text-gray-600 mt-1">#{{ job?.jobNumber }}</p>
          </div>
          <button (click)="onClose()" class="btn-close">
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <!-- Job Summary -->
        <div class="job-summary">
          <div class="summary-item">
            <svg class="w-5 h-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
            </svg>
            <div>
              <p class="summary-label">Route</p>
              <p class="summary-value">{{ formatLocation(job?.pickupLocation) }} → {{ formatLocation(job?.deliveryLocation) }}</p>
            </div>
          </div>

          <div class="summary-item">
            <svg class="w-5 h-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>
            <div>
              <p class="summary-label">Scheduled</p>
              <p class="summary-value">{{ formatDate(job?.scheduledDate) }} at {{ job?.scheduledTime }}</p>
            </div>
          </div>

          <div class="summary-item" *ngIf="job?.distance">
            <svg class="w-5 h-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-.553-.894L15 4m0 13V4m0 0L9 7" />
            </svg>
            <div>
              <p class="summary-label">Distance</p>
              <p class="summary-value">{{ job?.distance }} miles</p>
            </div>
          </div>
        </div>

        <!-- Bid Form -->
        <form (ngSubmit)="onSubmit()" class="bid-form">
          <!-- Bid Amount -->
          <div class="form-group">
            <label class="form-label required">Your Bid Amount</label>
            <div class="input-with-icon">
              <span class="input-icon">$</span>
              <input
                type="number"
                [(ngModel)]="bidAmount"
                name="bidAmount"
                class="form-control"
                [class.error]="showErrors && !bidAmount"
                placeholder="0.00"
                step="0.01"
                min="1"
                required
                (input)="calculateEarnings()">
            </div>
            <div class="help-text">
              <span *ngIf="suggestedMin && suggestedMax">
                Suggested range: \${{ suggestedMin.toFixed(2) }} - \${{ suggestedMax.toFixed(2) }}
              </span>
            </div>
            <div *ngIf="bidAmount" class="earnings-breakdown">
              <div class="breakdown-row">
                <span>Your earnings (85%)</span>
                <span class="text-green-600 font-semibold">\${{ driverEarnings.toFixed(2) }}</span>
              </div>
              <div class="breakdown-row text-xs">
                <span class="text-gray-500">Platform fee (15%)</span>
                <span class="text-gray-500">\${{ platformFee.toFixed(2) }}</span>
              </div>
            </div>
            <p *ngIf="showErrors && !bidAmount" class="error-message">Bid amount is required</p>
          </div>

          <!-- Estimated Duration -->
          <div class="form-group">
            <label class="form-label required">Estimated Duration (minutes)</label>
            <input
              type="number"
              [(ngModel)]="estimatedDuration"
              name="estimatedDuration"
              class="form-control"
              [class.error]="showErrors && !estimatedDuration"
              placeholder="e.g., 120"
              min="1"
              required>
            <p class="help-text">How long do you estimate this job will take?</p>
            <p *ngIf="showErrors && !estimatedDuration" class="error-message">Estimated duration is required</p>
          </div>

          <!-- Proposed Pickup Time -->
          <div class="form-group">
            <label class="form-label">Proposed Pickup Time (Optional)</label>
            <input
              type="datetime-local"
              [(ngModel)]="proposedPickupTime"
              name="proposedPickupTime"
              class="form-control"
              [min]="minPickupTime">
            <p class="help-text">Suggest a pickup time if different from scheduled</p>
          </div>

          <!-- Message to Customer -->
          <div class="form-group">
            <label class="form-label">Message to Customer (Optional)</label>
            <textarea
              [(ngModel)]="message"
              name="message"
              class="form-control"
              rows="4"
              maxlength="500"
              placeholder="Introduce yourself and explain why you're the best fit for this job..."></textarea>
            <p class="help-text">{{ message.length || 0 }}/500 characters</p>
          </div>

          <!-- Bid Expiration -->
          <div class="form-group">
            <label class="form-label">Bid Expires In</label>
            <select [(ngModel)]="expirationDays" name="expirationDays" class="form-control">
              <option [value]="1">1 day</option>
              <option [value]="2" selected>2 days (default)</option>
              <option [value]="3">3 days</option>
              <option [value]="5">5 days</option>
              <option [value]="7">7 days</option>
            </select>
          </div>

          <!-- Form Actions -->
          <div class="modal-actions">
            <button type="button" (click)="onClose()" class="btn btn-outline" [disabled]="isSubmitting">
              Cancel
            </button>
            <button type="submit" class="btn btn-primary" [disabled]="isSubmitting">
              <span *ngIf="!isSubmitting">Place Bid</span>
              <span *ngIf="isSubmitting" class="flex items-center">
                <svg class="animate-spin -ml-1 mr-2 h-4 w-4" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                Submitting...
              </span>
            </button>
          </div>
        </form>
      </div>
    </div>
  `,
  styles: [`
    .modal-overlay {
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.6);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
      padding: 1rem;
      overflow-y: auto;
    }

    .modal-content {
      background: white;
      border-radius: 1rem;
      max-width: 40rem;
      width: 100%;
      max-height: 90vh;
      overflow-y: auto;
      box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
    }

    .modal-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      padding: 1.5rem;
      border-bottom: 1px solid #e5e7eb;
      position: sticky;
      top: 0;
      background: white;
      z-index: 10;
    }

    .modal-header h2 {
      font-size: 1.5rem;
      font-weight: 700;
      color: #111827;
      margin: 0;
    }

    .btn-close {
      background: none;
      border: none;
      color: #6b7280;
      cursor: pointer;
      padding: 0.25rem;
      transition: color 0.2s;
    }

    .btn-close:hover {
      color: #111827;
    }

    .job-summary {
      padding: 1.5rem;
      background: linear-gradient(to bottom, #f9fafb, #ffffff);
      border-bottom: 1px solid #e5e7eb;
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .summary-item {
      display: flex;
      gap: 0.75rem;
      align-items: flex-start;
    }

    .summary-label {
      font-size: 0.75rem;
      font-weight: 500;
      color: #6b7280;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .summary-value {
      font-size: 0.875rem;
      color: #111827;
      margin-top: 0.25rem;
    }

    .bid-form {
      padding: 1.5rem;
    }

    .form-group {
      margin-bottom: 1.5rem;
    }

    .form-label {
      display: block;
      font-size: 0.875rem;
      font-weight: 600;
      color: #374151;
      margin-bottom: 0.5rem;
    }

    .form-label.required::after {
      content: " *";
      color: #ef4444;
    }

    .form-control {
      width: 100%;
      padding: 0.625rem 0.75rem;
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

    .form-control.error {
      border-color: #ef4444;
    }

    .form-control.error:focus {
      box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.1);
    }

    .input-with-icon {
      position: relative;
    }

    .input-icon {
      position: absolute;
      left: 0.75rem;
      top: 50%;
      transform: translateY(-50%);
      color: #6b7280;
      font-weight: 500;
      pointer-events: none;
    }

    .input-with-icon .form-control {
      padding-left: 2rem;
    }

    .help-text {
      font-size: 0.75rem;
      color: #6b7280;
      margin-top: 0.375rem;
    }

    .error-message {
      font-size: 0.75rem;
      color: #ef4444;
      margin-top: 0.375rem;
    }

    .earnings-breakdown {
      margin-top: 0.75rem;
      padding: 0.75rem;
      background: #f0fdf4;
      border: 1px solid #86efac;
      border-radius: 0.5rem;
    }

    .breakdown-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.25rem;
      font-size: 0.875rem;
    }

    .breakdown-row:last-child {
      margin-bottom: 0;
    }

    .modal-actions {
      display: flex;
      gap: 0.75rem;
      justify-content: flex-end;
      padding-top: 1rem;
      border-top: 1px solid #e5e7eb;
      margin-top: 1.5rem;
    }

    .btn {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      padding: 0.625rem 1.25rem;
      border-radius: 0.5rem;
      font-weight: 500;
      font-size: 0.875rem;
      transition: all 0.2s;
      cursor: pointer;
      border: none;
    }

    .btn:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .btn-primary {
      background: #3b82f6;
      color: white;
    }

    .btn-primary:hover:not(:disabled) {
      background: #2563eb;
    }

    .btn-outline {
      background: white;
      color: #374151;
      border: 1px solid #d1d5db;
    }

    .btn-outline:hover:not(:disabled) {
      background: #f9fafb;
    }

    @media (max-width: 640px) {
      .modal-content {
        max-height: 100vh;
        border-radius: 0;
      }

      .modal-overlay {
        padding: 0;
      }
    }
  `]
})
export class PlaceBidModalComponent implements OnInit {
  private jobBidsService = inject(JobBidsService);
  private toastService = inject(ToastService);

  @Input() job: JobDto | null = null;
  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();
  @Output() bidPlaced = new EventEmitter<void>();

  // Form fields
  bidAmount: number | null = null;
  estimatedDuration: number | null = null;
  proposedPickupTime: string | null = null;
  message: string = '';
  expirationDays: number = 2;

  // Earnings calculation
  platformFee: number = 0;
  driverEarnings: number = 0;

  // Suggested pricing
  suggestedMin: number = 0;
  suggestedMax: number = 0;

  // Validation
  showErrors = false;
  isSubmitting = false;

  // Min pickup time (now)
  minPickupTime: string = '';

  ngOnInit(): void {
    this.minPickupTime = new Date().toISOString().slice(0, 16);
    this.calculateSuggestedPricing();
  }

  calculateSuggestedPricing(): void {
    if (!this.job || !this.job.distance) {
      this.suggestedMin = 50;
      this.suggestedMax = 150;
      return;
    }

    // Simple calculation: $2-3 per mile + base fee
    const baseFee = 30;
    const perMileMin = 2;
    const perMileMax = 3;

    this.suggestedMin = Math.round(baseFee + (this.job.distance * perMileMin));
    this.suggestedMax = Math.round(baseFee + (this.job.distance * perMileMax));

    // Set default bid to middle of range
    if (!this.bidAmount) {
      this.bidAmount = Math.round((this.suggestedMin + this.suggestedMax) / 2);
      this.calculateEarnings();
    }
  }

  calculateEarnings(): void {
    if (!this.bidAmount) {
      this.platformFee = 0;
      this.driverEarnings = 0;
      return;
    }

    this.platformFee = Math.round(this.bidAmount * 0.15 * 100) / 100;
    this.driverEarnings = Math.round(this.bidAmount * 0.85 * 100) / 100;
  }

  onSubmit(): void {
    this.showErrors = true;

    // Validate required fields
    if (!this.bidAmount || !this.estimatedDuration) {
      this.toastService.error('Validation Error', 'Please fill in all required fields');
      return;
    }

    if (!this.job || !this.job.id) {
      this.toastService.error('Error', 'Job information is missing');
      return;
    }

    this.isSubmitting = true;

    const expiresAt = new Date();
    expiresAt.setDate(expiresAt.getDate() + this.expirationDays);

    const bidRequest: CreateBidRequest = {
      jobId: this.job.id,
      bidAmount: this.bidAmount,
      estimatedDuration: this.estimatedDuration,
      message: this.message || undefined
    };

    this.jobBidsService.createBid(bidRequest).subscribe({
      next: () => {
        this.toastService.success(
          'Bid Placed Successfully',
          `Your bid of $${this.bidAmount!.toFixed(2)} has been submitted. The customer will be notified.`
        );
        this.isSubmitting = false;
        this.bidPlaced.emit();
        this.onClose();
      },
      error: (error) => {
        console.error('Error placing bid:', error);
        this.toastService.error(
          'Failed to Place Bid',
          error.error?.title || 'Please try again.'
        );
        this.isSubmitting = false;
      }
    });
  }

  onClose(): void {
    if (!this.isSubmitting) {
      this.resetForm();
      this.close.emit();
    }
  }

  resetForm(): void {
    this.bidAmount = null;
    this.estimatedDuration = null;
    this.proposedPickupTime = null;
    this.message = '';
    this.expirationDays = 2;
    this.showErrors = false;
    this.platformFee = 0;
    this.driverEarnings = 0;
  }

  formatLocation(location: string | null | undefined): string {
    if (!location) return 'N/A';
    // Handle JSON or plain text
    if (location.startsWith('{')) {
      try {
        const parsed = JSON.parse(location);
        return parsed.address || parsed.city || location;
      } catch {
        return location;
      }
    }
    return location.length > 50 ? location.substring(0, 50) + '...' : location;
  }

  formatDate(date: string | null | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString('en-US', {
      weekday: 'short',
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  }
}
