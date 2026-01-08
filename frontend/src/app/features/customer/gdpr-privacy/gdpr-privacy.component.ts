import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { GdprService } from '../../../core/services/gdpr.service';
import { ToastService } from '../../../core/services/toast.service';
import {
  UserDataSummary,
  DataDeletionRequest,
  DeletionType,
  DeletionRequestStatus,
  RequestDataDeletionDto
} from '../../../core/models/gdpr.models';

@Component({
  selector: 'app-gdpr-privacy',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './gdpr-privacy.component.html',
  styleUrls: ['./gdpr-privacy.component.scss']
})
export class GdprPrivacyComponent implements OnInit {
  dataSummary: UserDataSummary | null = null;
  deletionRequests: DataDeletionRequest[] = [];
  deletionForm: FormGroup;
  confirmationForm: FormGroup;

  isLoadingSummary = false;
  isLoadingRequests = false;
  isExportingData = false;
  isRequestingDeletion = false;

  showDeletionRequestModal = false;
  showConfirmationModal = false;
  selectedRequest: DataDeletionRequest | null = null;

  // Enums for template
  DeletionType = DeletionType;
  DeletionRequestStatus = DeletionRequestStatus;

  constructor(
    private gdprService: GdprService,
    private toastService: ToastService,
    private fb: FormBuilder
  ) {
    this.deletionForm = this.fb.group({
      reason: ['', [Validators.required, Validators.minLength(10)]],
      deletionType: [DeletionType.Soft, Validators.required],
      requestDataExport: [true],
      gracePeriodDays: [30, [Validators.min(0), Validators.max(90)]]
    });

    this.confirmationForm = this.fb.group({
      token: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    this.loadDataSummary();
    this.loadDeletionRequests();
  }

  /**
   * Load user's data summary
   */
  loadDataSummary(): void {
    this.isLoadingSummary = true;
    this.gdprService.getMyDataSummary().subscribe({
      next: (summary) => {
        this.dataSummary = summary;
        this.isLoadingSummary = false;
      },
      error: (error) => {
        console.error('Error loading data summary:', error);
        this.toastService.error('Error', 'Failed to load data summary');
        this.isLoadingSummary = false;
      }
    });
  }

  /**
   * Load user's deletion requests
   */
  loadDeletionRequests(): void {
    this.isLoadingRequests = true;
    this.gdprService.getMyDeletionRequests().subscribe({
      next: (requests) => {
        this.deletionRequests = requests;
        this.isLoadingRequests = false;
      },
      error: (error) => {
        console.error('Error loading deletion requests:', error);
        this.toastService.error('Error', 'Failed to load deletion requests');
        this.isLoadingRequests = false;
      }
    });
  }

  /**
   * Export all personal data (GDPR Article 20)
   */
  exportData(): void {
    this.isExportingData = true;
    this.gdprService.downloadMyDataExport().subscribe({
      next: (blob) => {
        // Create download link
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `my-data-export-${new Date().toISOString().split('T')[0]}.json`;
        link.click();
        window.URL.revokeObjectURL(url);

        this.toastService.success('Success', 'Your data has been exported successfully');
        this.isExportingData = false;
      },
      error: (error) => {
        console.error('Error exporting data:', error);
        this.toastService.error('Error', 'Failed to export data');
        this.isExportingData = false;
      }
    });
  }

  /**
   * Open deletion request modal
   */
  openDeletionRequestModal(): void {
    this.deletionForm.reset({
      deletionType: DeletionType.Soft,
      requestDataExport: true,
      gracePeriodDays: 30
    });
    this.showDeletionRequestModal = true;
  }

  /**
   * Close deletion request modal
   */
  closeDeletionRequestModal(): void {
    this.showDeletionRequestModal = false;
  }

  /**
   * Submit deletion request
   */
  submitDeletionRequest(): void {
    if (this.deletionForm.invalid) {
      this.toastService.warning('Warning', 'Please fill in all required fields');
      return;
    }

    this.isRequestingDeletion = true;
    const request: RequestDataDeletionDto = this.deletionForm.value;

    this.gdprService.requestDeletion(request).subscribe({
      next: (response) => {
        this.toastService.success(
          'Success',
          'Deletion request submitted. Please check your email to confirm.'
        );
        this.loadDeletionRequests();
        this.closeDeletionRequestModal();
        this.isRequestingDeletion = false;
      },
      error: (error) => {
        console.error('Error submitting deletion request:', error);
        this.toastService.error('Error', error.error?.message || 'Failed to submit deletion request');
        this.isRequestingDeletion = false;
      }
    });
  }

  /**
   * Open confirmation modal
   */
  openConfirmationModal(request: DataDeletionRequest): void {
    this.selectedRequest = request;
    this.confirmationForm.reset();
    this.showConfirmationModal = true;
  }

  /**
   * Close confirmation modal
   */
  closeConfirmationModal(): void {
    this.showConfirmationModal = false;
    this.selectedRequest = null;
  }

  /**
   * Confirm deletion with token
   */
  confirmDeletion(): void {
    if (this.confirmationForm.invalid || !this.selectedRequest) {
      this.toastService.warning('Warning', 'Please enter the confirmation token');
      return;
    }

    const token = this.confirmationForm.value.token;
    this.gdprService.confirmDeletion(this.selectedRequest.id, token).subscribe({
      next: (response) => {
        this.toastService.success('Success', 'Deletion request confirmed successfully');
        this.loadDeletionRequests();
        this.closeConfirmationModal();
      },
      error: (error) => {
        console.error('Error confirming deletion:', error);
        this.toastService.error('Error', error.error?.message || 'Invalid or expired token');
      }
    });
  }

  /**
   * Cancel a deletion request
   */
  cancelDeletionRequest(request: DataDeletionRequest): void {
    if (!confirm('Are you sure you want to cancel this deletion request?')) {
      return;
    }

    this.gdprService.cancelDeletion(request.id).subscribe({
      next: (response) => {
        this.toastService.success('Success', 'Deletion request cancelled successfully');
        this.loadDeletionRequests();
      },
      error: (error) => {
        console.error('Error cancelling deletion request:', error);
        this.toastService.error('Error', 'Failed to cancel deletion request');
      }
    });
  }

  /**
   * Get status badge class
   */
  getStatusBadgeClass(status: DeletionRequestStatus): string {
    switch (status) {
      case DeletionRequestStatus.Pending:
        return 'badge-warning';
      case DeletionRequestStatus.Confirmed:
        return 'badge-info';
      case DeletionRequestStatus.Approved:
        return 'badge-primary';
      case DeletionRequestStatus.Rejected:
        return 'badge-danger';
      case DeletionRequestStatus.Completed:
        return 'badge-success';
      case DeletionRequestStatus.Cancelled:
        return 'badge-secondary';
      default:
        return 'badge-secondary';
    }
  }

  /**
   * Check if request can be cancelled
   */
  canCancelRequest(request: DataDeletionRequest): boolean {
    return request.status === DeletionRequestStatus.Pending ||
           request.status === DeletionRequestStatus.Confirmed;
  }

  /**
   * Check if request can be confirmed
   */
  canConfirmRequest(request: DataDeletionRequest): boolean {
    return request.status === DeletionRequestStatus.Pending &&
           !!request.confirmationToken;
  }

  /**
   * Get total records count
   */
  getTotalRecords(): number {
    return this.dataSummary?.totalRelatedRecords || 0;
  }
}
