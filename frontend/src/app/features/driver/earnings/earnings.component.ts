// features/driver/earnings/earnings.component.ts
import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { DriversService, EarningSummaryDto, EarningDto } from '@core/api';

@Component({
  selector: 'app-driver-earnings',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './earnings.component.html',
  styleUrls: ['./earnings.component.scss']
})
export class EarningsComponent implements OnInit {
  private readonly driversService = inject(DriversService);

  summary: EarningSummaryDto | null = null;
  recentEarnings: EarningDto[] = [];
  loading = true;
  error: string | null = null;

  // Filter state
  selectedStatus: string = 'all';
  currentPage = 1;
  pageSize = 10;

  ngOnInit(): void {
    this.loadEarningsData();
  }

  private loadEarningsData(): void {
    this.loading = true;
    this.error = null;

    // Load earnings summary
    this.driversService.apiDriversMeEarningsSummaryGet().subscribe({
      next: (summary) => {
        this.summary = summary;
        this.loadEarningsList();
      },
      error: (error) => {
        console.error('Error loading earnings summary:', error);
        this.error = 'Failed to load earnings data. Please try again.';
        this.loading = false;
      }
    });
  }

  private loadEarningsList(): void {
    const status = this.selectedStatus === 'all' ? undefined : this.selectedStatus;

    this.driversService.apiDriversMeEarningsGet(status, undefined, undefined, this.currentPage, this.pageSize).subscribe({
      next: (earnings) => {
        this.recentEarnings = earnings || [];
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading earnings list:', error);
        this.error = 'Failed to load earnings history. Please try again.';
        this.loading = false;
      }
    });
  }

  onStatusFilterChange(status: string): void {
    this.selectedStatus = status;
    this.currentPage = 1;
    this.loadEarningsList();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadEarningsList();
  }

  getPaymentStatusBadgeClass(status: string | null | undefined): string {
    if (!status) return 'bg-gray-100 text-gray-800';
    const statusMap: Record<string, string> = {
      'pending': 'bg-yellow-100 text-yellow-800',
      'processing': 'bg-blue-100 text-blue-800',
      'paid': 'bg-green-100 text-green-800',
      'failed': 'bg-red-100 text-red-800'
    };
    return statusMap[status] || 'bg-gray-100 text-gray-800';
  }

  formatStatus(status: string | null | undefined): string {
    if (!status) return 'N/A';
    return status.charAt(0).toUpperCase() + status.slice(1);
  }

  formatDate(date: string | null | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  }

  formatCurrency(amount: number | null | undefined): string {
    if (amount === null || amount === undefined) return '$0.00';
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount);
  }

  calculateEarningsRate(): number {
    if (!this.summary || !this.summary.totalJobs || this.summary.totalJobs === 0) return 0;
    return ((this.summary.completedPayments || 0) / this.summary.totalJobs) * 100;
  }
}
