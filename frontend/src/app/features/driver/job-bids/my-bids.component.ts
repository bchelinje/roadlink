import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { JobBidsService } from '@core/services/job-bids.service';
import { JobBid, CreateBidRequest } from '@core/models/job-bid.models';

@Component({
  selector: 'app-my-bids',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="container mx-auto px-4 py-8">
      <div class="page-header">
        <div>
          <h1>My Job Bids</h1>
          <p class="subtitle">Track and manage your job bids</p>
        </div>
      </div>

      @if (isLoading) {
        <div class="flex justify-center py-12">
          <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
        </div>
      } @else if (bids.length === 0) {
        <div class="bg-white rounded-lg shadow-md p-12 text-center">
          <svg class="mx-auto h-12 w-12 text-gray-400 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
          </svg>
          <p class="text-gray-500">No bids placed yet</p>
          <a
            routerLink="/driver/marketplace"
            class="mt-4 inline-block px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700">
            Browse Marketplace Jobs
          </a>
        </div>
      } @else {
        <div class="space-y-4">
          @for (bid of bids; track bid.id) {
            <div class="bg-white rounded-lg shadow-md p-6">
              <div class="flex justify-between items-start mb-4">
                <div>
                  <h3 class="text-lg font-semibold text-gray-800">Job #{{ bid.jobId }}</h3>
                  <p class="text-sm text-gray-500">Bid Amount: \${{ bid.bidAmount.toFixed(2) }}</p>
                  <p class="text-sm text-gray-500">Estimated Duration: {{ bid.estimatedDuration }} minutes</p>
                </div>
                <span [class]="getStatusClass(bid.status)">
                  {{ formatStatus(bid.status) }}
                </span>
              </div>

              @if (bid.message) {
                <p class="text-gray-600 mb-4">{{ bid.message }}</p>
              }

              <div class="flex gap-4 text-sm text-gray-500 mb-4">
                <span>Submitted: {{ formatDate(bid.createdAt) }}</span>
                <span>Expires: {{ formatDate(bid.expiresAt) }}</span>
              </div>

              @if (bid.status === 'pending') {
                <button
                  (click)="withdrawBid(bid.id)"
                  class="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 text-sm">
                  Withdraw Bid
                </button>
              }
            </div>
          }
        </div>
      }
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
export class MyBidsComponent implements OnInit {
  private jobBidsService = inject(JobBidsService);

  bids: JobBid[] = [];
  isLoading = false;

  ngOnInit(): void {
    this.loadBids();
  }

  loadBids(): void {
    this.isLoading = true;
    this.jobBidsService.getMyBids().subscribe({
      next: (bids) => {
        this.bids = bids;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading bids:', error);
        this.isLoading = false;
      }
    });
  }

  withdrawBid(bidId: string): void {
    if (!confirm('Are you sure you want to withdraw this bid?')) {
      return;
    }

    this.jobBidsService.withdrawBid(bidId).subscribe({
      next: () => {
        this.loadBids();
      },
      error: (error) => {
        console.error('Error withdrawing bid:', error);
      }
    });
  }

  formatStatus(status: string): string {
    return status.charAt(0).toUpperCase() + status.slice(1);
  }

  getStatusClass(status: string): string {
    const base = 'inline-flex px-2 py-1 text-xs font-semibold rounded-full ';
    switch (status) {
      case 'pending': return base + 'bg-yellow-100 text-yellow-800';
      case 'accepted': return base + 'bg-green-100 text-green-800';
      case 'rejected': return base + 'bg-red-100 text-red-800';
      case 'withdrawn': return base + 'bg-gray-100 text-gray-800';
      case 'expired': return base + 'bg-gray-100 text-gray-800';
      default: return base + 'bg-gray-100 text-gray-800';
    }
  }

  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}
