import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { PaymentsService, Payment } from '@core/api';
import { environment } from '@environments/environment';

@Component({
  selector: 'app-payment-history',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="container mx-auto px-4 py-8">
      <div class="page-header">
        <div>
          <h1>Payment History</h1>
          <p class="subtitle">View all your past payments and transactions</p>
        </div>
      </div>

      <!-- Loading State -->
      <div *ngIf="isLoading" class="text-center py-12">
        <div class="inline-block w-12 h-12 border-4 border-gray-300 border-t-blue-600 rounded-full animate-spin"></div>
        <p class="mt-4 text-gray-600">Loading payments...</p>
      </div>

      <!-- Error State -->
      <div *ngIf="errorMessage && !isLoading" class="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
        <p class="text-red-800">{{ errorMessage }}</p>
      </div>

      <!-- Empty State -->
      <div *ngIf="!isLoading && payments.length === 0 && !errorMessage" class="text-center py-12">
        <svg class="w-24 h-24 mx-auto text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2m2 4h10a2 2 0 002-2v-6a2 2 0 00-2-2H9a2 2 0 00-2 2v6a2 2 0 002 2zm7-5a2 2 0 11-4 0 2 2 0 014 0z" />
        </svg>
        <h3 class="mt-4 text-lg font-medium text-gray-900">No payments yet</h3>
        <p class="mt-2 text-gray-600">Your payment history will appear here</p>
      </div>

      <!-- Payments List -->
      <div *ngIf="!isLoading && payments.length > 0" class="space-y-4">
        <div
          *ngFor="let payment of payments"
          class="bg-white border border-gray-200 rounded-lg p-6 hover:shadow-md transition-shadow"
        >
          <div class="flex items-start justify-between">
            <div class="flex-1">
              <!-- Payment Header -->
              <div class="flex items-center gap-3 mb-3">
                <div
                  class="w-12 h-12 rounded-full flex items-center justify-center"
                  [ngClass]="{
                    'bg-green-100 text-green-600': payment.status === 'Completed' || payment.status === 'Succeeded',
                    'bg-yellow-100 text-yellow-600': payment.status === 'Pending' || payment.status === 'Processing',
                    'bg-red-100 text-red-600': payment.status === 'Failed' || payment.status === 'Cancelled',
                    'bg-blue-100 text-blue-600': payment.status === 'Refunded'
                  }"
                >
                  <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                          d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2m2 4h10a2 2 0 002-2v-6a2 2 0 00-2-2H9a2 2 0 00-2 2v6a2 2 0 002 2zm7-5a2 2 0 11-4 0 2 2 0 014 0z" />
                  </svg>
                </div>
                <div>
                  <h3 class="text-lg font-semibold text-gray-900">
                    {{ formatCurrency(payment.amount) }}
                  </h3>
                  <p class="text-sm text-gray-600">
                    {{ formatDate(payment.createdAt) }}
                  </p>
                </div>
              </div>

              <!-- Payment Details -->
              <div class="grid grid-cols-1 md:grid-cols-3 gap-4 text-sm">
                <div>
                  <span class="text-gray-500">Job #:</span>
                  <span class="ml-2 font-medium text-gray-900">{{ payment.jobId || 'N/A' }}</span>
                </div>
                <div>
                  <span class="text-gray-500">Payment Method:</span>
                  <span class="ml-2 font-medium text-gray-900">{{ payment.paymentMethod || 'Card' }}</span>
                </div>
                <div>
                  <span class="text-gray-500">Transaction ID:</span>
                  <span class="ml-2 font-mono text-xs text-gray-900">{{ payment.paymentNumber || payment.id }}</span>
                </div>
              </div>

              <!-- Description -->
              <p *ngIf="payment.description" class="mt-3 text-sm text-gray-600">
                {{ payment.description }}
              </p>
            </div>

            <!-- Status Badge -->
            <span
              class="px-3 py-1 text-xs font-semibold rounded-full"
              [ngClass]="getStatusClass(payment.status)"
            >
              {{ payment.status }}
            </span>
          </div>

          <!-- Refund Info -->
          <div *ngIf="payment.status === 'Refunded' && payment.refundedAt" class="mt-4 pt-4 border-t border-gray-200">
            <p class="text-sm text-gray-600">
              <span class="font-medium">Refunded:</span> {{ formatDate(payment.refundedAt) }}
            </p>
          </div>

          <!-- Actions -->
          <div class="mt-4 flex items-center gap-3">
            <button
              *ngIf="payment.jobId"
              [routerLink]="['/customer/jobs', payment.jobId]"
              class="text-sm text-blue-600 hover:text-blue-700 font-medium"
            >
              View Job
            </button>
            <button
              (click)="downloadReceipt(payment)"
              class="text-sm text-gray-600 hover:text-gray-900 font-medium"
            >
              Download Receipt
            </button>
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
export class PaymentHistoryComponent implements OnInit {
  private paymentsService = inject(PaymentsService);
  private http = inject(HttpClient);

  payments: Payment[] = [];
  isLoading = false;
  errorMessage = '';

  ngOnInit(): void {
    this.loadPayments();
  }

  loadPayments(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.paymentsService.apiCustomersMePaymentsGet().subscribe({
      next: (payments: Payment[]) => {
        this.payments = payments || [];
        this.isLoading = false;
      },
      error: (error: any) => {
        console.error('Error loading payments:', error);
        this.errorMessage = 'Failed to load payment history. Please try again.';
        this.isLoading = false;
      }
    });
  }

  getStatusClass(status: string | null | undefined): string {
    if (!status) return 'bg-gray-100 text-gray-800';

    const statusLower = status.toLowerCase();
    if (statusLower.includes('complete') || statusLower.includes('succeed')) {
      return 'bg-green-100 text-green-800';
    }
    if (statusLower.includes('pending') || statusLower.includes('processing')) {
      return 'bg-yellow-100 text-yellow-800';
    }
    if (statusLower.includes('fail') || statusLower.includes('cancel')) {
      return 'bg-red-100 text-red-800';
    }
    if (statusLower.includes('refund')) {
      return 'bg-blue-100 text-blue-800';
    }
    return 'bg-gray-100 text-gray-800';
  }

  formatCurrency(amount: number | null | undefined): string {
    if (amount === null || amount === undefined) return '$0.00';
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount);
  }

  formatDate(date: string | null | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: 'numeric',
      minute: '2-digit'
    });
  }

  downloadReceipt(payment: Payment): void {
    if (!payment.id) {
      this.errorMessage = 'Payment ID not available';
      return;
    }

    const apiUrl = environment.apiBaseUrl || 'https://localhost:7172/api';
    const receiptUrl = `${apiUrl}/payments/${payment.id}/receipt`;

    this.http.get(receiptUrl, { responseType: 'blob', observe: 'response' }).subscribe({
      next: (response: any) => {
        // Get filename from Content-Disposition header or use default
        const contentDisposition = response.headers.get('Content-Disposition');
        let filename = `Receipt-${payment.paymentNumber || payment.id}.txt`;

        if (contentDisposition) {
          const matches = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/.exec(contentDisposition);
          if (matches != null && matches[1]) {
            filename = matches[1].replace(/['"]/g, '');
          }
        }

        // Create blob and download
        const blob = response.body;
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = filename;
        link.click();
        window.URL.revokeObjectURL(url);
      },
      error: (error: any) => {
        console.error('Error downloading receipt:', error);
        this.errorMessage = 'Failed to download receipt. Please try again.';
      }
    });
  }
}
