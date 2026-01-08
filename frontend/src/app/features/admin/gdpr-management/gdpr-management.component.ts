import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, FormsModule } from '@angular/forms';
import { GdprService } from '../../../core/services/gdpr.service';
import { ToastService } from '../../../core/services/toast.service';
import {
  DataDeletionRequest,
  DeletionRequestStatus,
  DeletionType,
  ReviewDeletionRequestDto,
  DeletionResult
} from '../../../core/models/gdpr.models';

@Component({
  selector: 'app-gdpr-management',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './gdpr-management.component.html',
  styleUrls: ['./gdpr-management.component.scss']
})
export class GdprManagementComponent implements OnInit {
  deletionRequests: DataDeletionRequest[] = [];
  filteredRequests: DataDeletionRequest[] = [];
  selectedRequest: DataDeletionRequest | null = null;

  isLoading = false;
  isProcessing = false;

  // Filters
  statusFilter: string = 'all';
  searchTerm: string = '';

  // Modals
  showReviewModal = false;
  showProcessModal = false;
  showDetailsModal = false;

  // Forms
  reviewForm: FormGroup;

  // Enums for template
  DeletionRequestStatus = DeletionRequestStatus;
  DeletionType = DeletionType;

  // Pagination
  currentPage = 1;
  pageSize = 20;
  totalRecords = 0;
  totalPages = 1;

  constructor(
    private gdprService: GdprService,
    private toastService: ToastService,
    private fb: FormBuilder
  ) {
    this.reviewForm = this.fb.group({
      approved: [false, Validators.required],
      rejectionReason: ['']
    });
  }

  ngOnInit(): void {
    this.loadDeletionRequests();
  }

  /**
   * Load all deletion requests
   */
  loadDeletionRequests(): void {
    this.isLoading = true;
    const status = this.statusFilter === 'all' ? undefined : this.statusFilter;

    this.gdprService.getAllDeletionRequests(status, this.currentPage, this.pageSize).subscribe({
      next: (response) => {
        this.deletionRequests = response.data || [];
        this.totalRecords = response.pagination?.total || 0;
        this.totalPages = response.pagination?.totalPages || 1;
        this.currentPage = response.pagination?.page || 1;
        this.applyFilters();
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading deletion requests:', error);
        this.toastService.error('Error', 'Failed to load deletion requests');
        this.isLoading = false;
      }
    });
  }

  /**
   * Apply filters to the requests
   */
  applyFilters(): void {
    this.filteredRequests = this.deletionRequests.filter(request => {
      // Search filter
      if (this.searchTerm) {
        const term = this.searchTerm.toLowerCase();
        const matchesSearch =
          request.userName?.toLowerCase().includes(term) ||
          request.userEmail?.toLowerCase().includes(term) ||
          request.reason.toLowerCase().includes(term);

        if (!matchesSearch) return false;
      }

      return true;
    });

    this.totalRecords = this.filteredRequests.length;
  }

  /**
   * Handle status filter change
   */
  onStatusFilterChange(status: string): void {
    this.statusFilter = status;
    this.currentPage = 1;
    this.loadDeletionRequests();
  }

  /**
   * Handle search
   */
  onSearch(term: string): void {
    this.searchTerm = term;
    this.applyFilters();
  }

  /**
   * Open review modal
   */
  openReviewModal(request: DataDeletionRequest): void {
    this.selectedRequest = request;
    this.reviewForm.reset({ approved: false });
    this.showReviewModal = true;
  }

  /**
   * Close review modal
   */
  closeReviewModal(): void {
    this.showReviewModal = false;
    this.selectedRequest = null;
  }

  /**
   * Submit review (approve/reject)
   */
  reviewRequest(): void {
    this.submitReview();
  }

  /**
   * Submit review (approve/reject)
   */
  submitReview(): void {
    if (!this.selectedRequest || this.reviewForm.invalid) {
      return;
    }

    const review: ReviewDeletionRequestDto = this.reviewForm.value;

    // Validate rejection reason if rejecting
    if (!review.approved && !review.rejectionReason) {
      this.toastService.warning('Warning', 'Please provide a reason for rejection');
      return;
    }

    this.isProcessing = true;

    this.gdprService.reviewDeletionRequest(this.selectedRequest.id, review).subscribe({
      next: (response) => {
        const action = review.approved ? 'approved' : 'rejected';
        this.toastService.success('Success', `Deletion request ${action} successfully`);
        this.loadDeletionRequests();
        this.closeReviewModal();
        this.isProcessing = false;
      },
      error: (error) => {
        console.error('Error reviewing deletion request:', error);
        this.toastService.error('Error', error.error?.message || 'Failed to review deletion request');
        this.isProcessing = false;
      }
    });
  }

  /**
   * Open process modal
   */
  openProcessModal(request: DataDeletionRequest): void {
    this.selectedRequest = request;
    this.showProcessModal = true;
  }

  /**
   * Close process modal
   */
  closeProcessModal(): void {
    this.showProcessModal = false;
    this.selectedRequest = null;
  }

  /**
   * Process (execute) deletion
   */
  processDeletion(): void {
    if (!this.selectedRequest) {
      return;
    }

    this.isProcessing = true;

    this.gdprService.processDeletionRequest(this.selectedRequest.id).subscribe({
      next: (result: DeletionResult) => {
        this.toastService.success('Success', result.message);
        this.loadDeletionRequests();
        this.closeProcessModal();
        this.isProcessing = false;
      },
      error: (error) => {
        console.error('Error processing deletion:', error);
        this.toastService.error('Error', error.error?.message || 'Failed to process deletion');
        this.isProcessing = false;
      }
    });
  }

  /**
   * Open details modal
   */
  openDetailsModal(request: DataDeletionRequest): void {
    this.selectedRequest = request;
    this.showDetailsModal = true;
  }

  /**
   * Close details modal
   */
  closeDetailsModal(): void {
    this.showDetailsModal = false;
    this.selectedRequest = null;
  }

  /**
   * Get status badge class (Tailwind CSS)
   */
  getStatusBadgeClass(status: DeletionRequestStatus): string {
    switch (status) {
      case DeletionRequestStatus.Pending:
        return 'bg-yellow-100 text-yellow-800';
      case DeletionRequestStatus.Confirmed:
        return 'bg-blue-100 text-blue-800';
      case DeletionRequestStatus.Approved:
        return 'bg-green-100 text-green-800';
      case DeletionRequestStatus.Rejected:
        return 'bg-red-100 text-red-800';
      case DeletionRequestStatus.Completed:
        return 'bg-green-100 text-green-800';
      case DeletionRequestStatus.Cancelled:
        return 'bg-gray-100 text-gray-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  /**
   * Check if request can be reviewed
   */
  canReview(request: DataDeletionRequest): boolean {
    return request.status === DeletionRequestStatus.Confirmed;
  }

  /**
   * Check if request can be processed
   */
  canProcess(request: DataDeletionRequest): boolean {
    return request.status === DeletionRequestStatus.Approved;
  }

  /**
   * Get pending requests count
   */
  getPendingCount(): number {
    return this.deletionRequests.filter(r =>
      r.status === DeletionRequestStatus.Pending ||
      r.status === DeletionRequestStatus.Confirmed
    ).length;
  }

  /**
   * Get approved requests count
   */
  getApprovedCount(): number {
    return this.deletionRequests.filter(r =>
      r.status === DeletionRequestStatus.Approved
    ).length;
  }

  /**
   * Get completed requests count
   */
  getCompletedCount(): number {
    return this.deletionRequests.filter(r =>
      r.status === DeletionRequestStatus.Completed
    ).length;
  }

  /**
   * Check if approved checkbox should update rejection reason requirement
   */
  onApprovedChange(): void {
    const approved = this.reviewForm.get('approved')?.value;
    const rejectionReasonControl = this.reviewForm.get('rejectionReason');

    if (approved) {
      rejectionReasonControl?.clearValidators();
    } else {
      rejectionReasonControl?.setValidators([Validators.required]);
    }
    rejectionReasonControl?.updateValueAndValidity();
  }

  /**
   * Clear all filters
   */
  clearFilters(): void {
    this.searchTerm = '';
    this.statusFilter = 'all';
    this.currentPage = 1;
    this.loadDeletionRequests();
  }

  /**
   * Navigate to next page
   */
  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.loadDeletionRequests();
    }
  }

  /**
   * Navigate to previous page
   */
  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadDeletionRequests();
    }
  }
}
