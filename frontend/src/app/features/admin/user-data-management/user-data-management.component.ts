import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { GdprService } from '../../../core/services/gdpr.service';
import { ToastService } from '../../../core/services/toast.service';
import { AuthService } from '../../../core/services/auth.service';
import {
  UserDataSummary,
  DeletionType,
  AnonymizeUserDto,
  DeletionResult
} from '../../../core/models/gdpr.models';

@Component({
  selector: 'app-user-data-management',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './user-data-management.component.html',
  styleUrls: ['./user-data-management.component.scss']
})
export class UserDataManagementComponent {
  searchForm: FormGroup;
  anonymizeForm: FormGroup;

  userDataSummary: UserDataSummary | null = null;
  isSearching = false;
  isLoadingSummary = false;
  isExporting = false;
  isAnonymizing = false;

  showAnonymizeModal = false;
  showDataSummaryModal = false;

  // Enums for template
  DeletionType = DeletionType;

  constructor(
    private gdprService: GdprService,
    private authService: AuthService,
    private toastService: ToastService,
    private fb: FormBuilder
  ) {
    this.searchForm = this.fb.group({
      userId: ['', [Validators.required]]
    });

    this.anonymizeForm = this.fb.group({
      deletionType: [DeletionType.Soft, Validators.required],
      reason: ['', [Validators.required, Validators.minLength(10)]]
    });
  }

  /**
   * Search for user and load data summary
   */
  searchUser(): void {
    if (this.searchForm.invalid) {
      this.toastService.warning('Warning', 'Please enter a valid user ID');
      return;
    }

    const userId = this.searchForm.value.userId.trim();
    this.loadUserDataSummary(userId);
  }

  /**
   * Load user data summary
   */
  loadUserDataSummary(userId: string): void {
    this.isLoadingSummary = true;

    this.gdprService.getUserDataSummary(userId).subscribe({
      next: (summary) => {
        this.userDataSummary = summary;
        this.isLoadingSummary = false;
        this.toastService.success('Success', 'User data loaded successfully');
      },
      error: (error) => {
        console.error('Error loading user data summary:', error);
        this.toastService.error('Error', error.error?.message || 'Failed to load user data');
        this.userDataSummary = null;
        this.isLoadingSummary = false;
      }
    });
  }

  /**
   * Export user data
   */
  exportUserData(): void {
    if (!this.userDataSummary) {
      return;
    }

    this.isExporting = true;

    this.gdprService.downloadUserDataExport(this.userDataSummary.userId).subscribe({
      next: (blob) => {
        // Create download link
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `user-${this.userDataSummary?.username}-data-export-${new Date().toISOString().split('T')[0]}.json`;
        link.click();
        window.URL.revokeObjectURL(url);

        this.toastService.success('Success', 'User data exported successfully');
        this.isExporting = false;
      },
      error: (error) => {
        console.error('Error exporting user data:', error);
        this.toastService.error('Error', 'Failed to export user data');
        this.isExporting = false;
      }
    });
  }

  /**
   * Open anonymize modal
   */
  openAnonymizeModal(): void {
    if (!this.userDataSummary) {
      return;
    }

    this.anonymizeForm.reset({ deletionType: DeletionType.Soft });
    this.showAnonymizeModal = true;
  }

  /**
   * Close anonymize modal
   */
  closeAnonymizeModal(): void {
    this.showAnonymizeModal = false;
  }

  /**
   * Anonymize user
   */
  anonymizeUser(): void {
    if (!this.userDataSummary || this.anonymizeForm.invalid) {
      this.toastService.warning('Warning', 'Please fill in all required fields');
      return;
    }

    if (!confirm(`Are you sure you want to ${this.anonymizeForm.value.deletionType === DeletionType.Soft ? 'anonymize' : 'permanently delete'} this user's data? This action cannot be undone!`)) {
      return;
    }

    this.isAnonymizing = true;

    // Get current admin user ID
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser || !currentUser.sub) {
      this.toastService.error('Error', 'Unable to identify current user');
      this.isAnonymizing = false;
      return;
    }

    const request: AnonymizeUserDto = {
      deletionType: this.anonymizeForm.value.deletionType,
      performedByUserId: currentUser.sub,
      reason: this.anonymizeForm.value.reason
    };

    this.gdprService.anonymizeUser(this.userDataSummary.userId, request).subscribe({
      next: (result: DeletionResult) => {
        this.toastService.success('Success', result.message);
        this.closeAnonymizeModal();
        this.userDataSummary = null;
        this.searchForm.reset();
        this.isAnonymizing = false;
      },
      error: (error) => {
        console.error('Error anonymizing user:', error);
        this.toastService.error('Error', error.error?.message || 'Failed to anonymize user');
        this.isAnonymizing = false;
      }
    });
  }

  /**
   * Open data summary modal
   */
  openDataSummaryModal(): void {
    this.showDataSummaryModal = true;
  }

  /**
   * Close data summary modal
   */
  closeDataSummaryModal(): void {
    this.showDataSummaryModal = false;
  }

  /**
   * Get total records count
   */
  getTotalRecords(): number {
    return this.userDataSummary?.totalRelatedRecords || 0;
  }

  /**
   * Clear search
   */
  clearSearch(): void {
    this.searchForm.reset();
    this.userDataSummary = null;
  }
}
