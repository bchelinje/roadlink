import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { CustomersService, Review } from '@core/api';

@Component({
  selector: 'app-my-reviews',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="container mx-auto px-4 py-8">
      <div class="page-header">
        <div>
          <h1>My Reviews</h1>
          <p class="subtitle">Reviews you've submitted for completed jobs</p>
        </div>
      </div>

      <!-- Loading State -->
      <div *ngIf="isLoading" class="text-center py-12">
        <div class="inline-block w-12 h-12 border-4 border-gray-300 border-t-blue-600 rounded-full animate-spin"></div>
        <p class="mt-4 text-gray-600">Loading reviews...</p>
      </div>

      <!-- Error State -->
      <div *ngIf="errorMessage && !isLoading" class="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
        <p class="text-red-800">{{ errorMessage }}</p>
      </div>

      <!-- Empty State -->
      <div *ngIf="!isLoading && reviews.length === 0 && !errorMessage" class="text-center py-12">
        <svg class="w-24 h-24 mx-auto text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z" />
        </svg>
        <h3 class="mt-4 text-lg font-medium text-gray-900">No reviews yet</h3>
        <p class="mt-2 text-gray-600">Your submitted reviews will appear here</p>
      </div>

      <!-- Reviews List -->
      <div *ngIf="!isLoading && reviews.length > 0" class="space-y-6">
        <div
          *ngFor="let review of reviews"
          class="bg-white border border-gray-200 rounded-lg p-6"
        >
          <!-- Review Header -->
          <div class="flex items-start justify-between mb-4">
            <div class="flex-1">
              <div class="flex items-center gap-2 mb-2">
                <div class="flex">
                  <svg
                    *ngFor="let star of [1,2,3,4,5]"
                    [class]="star <= (review.rating || 0) ? 'text-yellow-400' : 'text-gray-300'"
                    class="w-5 h-5 fill-current"
                    viewBox="0 0 20 20"
                  >
                    <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                  </svg>
                </div>
                <span class="text-sm text-gray-600">{{ formatDate(review.createdAt) }}</span>
              </div>

              <div *ngIf="review.revieweeName" class="text-sm text-gray-600 mb-2">
                Driver: <span class="font-medium text-gray-900">{{ review.revieweeName }}</span>
              </div>

              <div *ngIf="review.job?.jobNumber" class="text-sm text-gray-600">
                Job: <span class="font-mono text-gray-900">{{ review.job?.jobNumber }}</span>
              </div>
            </div>

            <div class="flex items-center gap-2">
              <span
                [class]="getStatusClass(review.status)"
                class="px-2 py-0.5 text-xs font-medium rounded"
              >
                {{ review.status }}
              </span>
            </div>
          </div>

          <!-- Review Content -->
          <div class="mb-4">
            <p class="text-gray-900">{{ review.comment || 'No comment provided' }}</p>
          </div>

          <!-- Driver Response -->
          <div *ngIf="review.response" class="mt-4 pt-4 border-t border-gray-200">
            <div class="bg-gray-50 rounded-lg p-4">
              <div class="flex items-center gap-2 mb-2">
                <svg class="w-5 h-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                        d="M3 10h10a8 8 0 018 8v2M3 10l6 6m-6-6l6-6" />
                </svg>
                <span class="text-sm font-medium text-gray-900">Driver Response</span>
                <span class="text-xs text-gray-500">{{ formatDate(review.responseDate) }}</span>
              </div>
              <p class="text-sm text-gray-700">{{ review.response }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 2rem;
      padding: 2rem;
      background: linear-gradient(135deg, #003d82 0%, #001f3f 100%);
      border-radius: 1rem;
      color: white;
    }
    .page-header h1 {
      margin: 0 0 0.5rem 0;
      font-size: 2rem;
      font-weight: 700;
    }
    .page-header .subtitle {
      margin: 0;
      opacity: 0.9;
      font-size: 1rem;
    }
    @media (max-width: 768px) {
      .page-header {
        flex-direction: column;
        align-items: flex-start;
        gap: 1.5rem;
      }
    }
  `]
})
export class MyReviewsComponent implements OnInit {
  private customersService = inject(CustomersService);

  reviews: Review[] = [];
  isLoading = false;
  errorMessage = '';

  ngOnInit(): void {
    this.loadReviews();
  }

  loadReviews(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.customersService.apiCustomersMeReviewsGet().subscribe({
      next: (reviews: Review[]) => {
        this.reviews = reviews || [];
        this.isLoading = false;
      },
      error: (error: any) => {
        console.error('Error loading reviews:', error);
        this.errorMessage = 'Failed to load reviews. Please try again.';
        this.isLoading = false;
      }
    });
  }

  getStatusClass(status: string | null | undefined): string {
    switch (status) {
      case 'Approved':
        return 'bg-green-100 text-green-800';
      case 'Pending':
        return 'bg-yellow-100 text-yellow-800';
      case 'Rejected':
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  formatDate(dateString: string | null | undefined): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }
}
