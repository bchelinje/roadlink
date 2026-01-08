import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ComplaintsService } from '@core/services/complaints.service';
import { Complaint, CreateComplaintRequest, ComplaintCategory, ComplaintSeverity } from '@core/models/complaint.models';

@Component({
  selector: 'app-complaints-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="container mx-auto px-4 py-8">
      <div class="flex justify-between items-center mb-6">
        <h1 class="text-3xl font-bold text-gray-800">My Complaints</h1>
        <button
          (click)="showCreateForm = !showCreateForm"
          class="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700">
          File Complaint
        </button>
      </div>

      @if (showCreateForm) {
        <div class="mb-6 bg-white rounded-lg shadow-md p-6">
          <h2 class="text-xl font-semibold mb-4">File New Complaint</h2>
          <form (submit)="createComplaint(); $event.preventDefault()">
            <div class="mb-4">
              <label class="block text-sm font-medium text-gray-700 mb-2">Subject *</label>
              <input
                [(ngModel)]="newComplaint.subject"
                name="subject"
                type="text"
                required
                class="w-full px-3 py-2 border border-gray-300 rounded-md"
              />
            </div>

            <div class="mb-4">
              <label class="block text-sm font-medium text-gray-700 mb-2">Description *</label>
              <textarea
                [(ngModel)]="newComplaint.description"
                name="description"
                rows="4"
                required
                class="w-full px-3 py-2 border border-gray-300 rounded-md"
              ></textarea>
            </div>

            <div class="grid grid-cols-2 gap-4 mb-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-2">Category *</label>
                <select
                  [(ngModel)]="newComplaint.category"
                  name="category"
                  class="w-full px-3 py-2 border border-gray-300 rounded-md">
                  <option value="service">Service</option>
                  <option value="billing">Billing</option>
                  <option value="safety">Safety</option>
                  <option value="conduct">Conduct</option>
                  <option value="damage">Damage</option>
                  <option value="other">Other</option>
                </select>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 mb-2">Severity *</label>
                <select
                  [(ngModel)]="newComplaint.severity"
                  name="severity"
                  class="w-full px-3 py-2 border border-gray-300 rounded-md">
                  <option value="low">Low</option>
                  <option value="medium">Medium</option>
                  <option value="high">High</option>
                  <option value="critical">Critical</option>
                </select>
              </div>
            </div>

            <div class="flex gap-2">
              <button
                type="submit"
                [disabled]="isSubmitting"
                class="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700">
                Submit Complaint
              </button>
              <button
                type="button"
                (click)="showCreateForm = false"
                class="px-4 py-2 bg-gray-300 text-gray-700 rounded-lg">
                Cancel
              </button>
            </div>
          </form>
        </div>
      }

      @if (isLoading) {
        <div class="flex justify-center py-12">
          <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-red-600"></div>
        </div>
      } @else if (complaints.length === 0) {
        <div class="bg-white rounded-lg shadow-md p-12 text-center">
          <p class="text-gray-500">No complaints filed</p>
        </div>
      } @else {
        <div class="space-y-4">
          @for (complaint of complaints; track complaint.id) {
            <div class="bg-white rounded-lg shadow-md p-6">
              <div class="flex justify-between items-start mb-4">
                <div>
                  <h3 class="text-lg font-semibold text-gray-800">{{ complaint.subject }}</h3>
                  <p class="text-sm text-gray-500">Complaint #{{ complaint.complaintNumber }}</p>
                </div>
                <span [class]="getStatusClass(complaint.status)">
                  {{ formatStatus(complaint.status) }}
                </span>
              </div>
              <p class="text-gray-600 mb-4">{{ complaint.description }}</p>
              <div class="flex gap-4 text-sm text-gray-500">
                <span>Category: {{ formatCategory(complaint.category) }}</span>
                <span>Severity: {{ formatSeverity(complaint.severity) }}</span>
                <span>Filed: {{ formatDate(complaint.createdAt) }}</span>
              </div>
            </div>
          }
        </div>
      }
    </div>
  `
})
export class ComplaintsListComponent implements OnInit {
  private complaintsService = inject(ComplaintsService);

  complaints: Complaint[] = [];
  isLoading = false;
  isSubmitting = false;
  showCreateForm = false;

  newComplaint: CreateComplaintRequest = {
    subject: '',
    description: '',
    category: 'service' as ComplaintCategory,
    severity: 'medium' as ComplaintSeverity
  };

  ngOnInit(): void {
    this.loadComplaints();
  }

  loadComplaints(): void {
    this.isLoading = true;
    this.complaintsService.getComplaints().subscribe({
      next: (complaints) => {
        this.complaints = complaints;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading complaints:', error);
        this.isLoading = false;
      }
    });
  }

  createComplaint(): void {
    this.isSubmitting = true;
    this.complaintsService.createComplaint(this.newComplaint).subscribe({
      next: () => {
        this.showCreateForm = false;
        this.newComplaint = {
          subject: '',
          description: '',
          category: 'service' as ComplaintCategory,
          severity: 'medium' as ComplaintSeverity
        };
        this.loadComplaints();
        this.isSubmitting = false;
      },
      error: (error) => {
        console.error('Error creating complaint:', error);
        this.isSubmitting = false;
      }
    });
  }

  formatStatus(status: string): string {
    return status.replace('_', ' ').replace(/\b\w/g, l => l.toUpperCase());
  }

  formatCategory(category: ComplaintCategory): string {
    return category.toString().charAt(0).toUpperCase() + category.toString().slice(1);
  }

  formatSeverity(severity: ComplaintSeverity): string {
    return severity.toString().charAt(0).toUpperCase() + severity.toString().slice(1);
  }

  getStatusClass(status: string): string {
    const base = 'inline-flex px-2 py-1 text-xs font-semibold rounded-full ';
    switch (status) {
      case 'pending': return base + 'bg-yellow-100 text-yellow-800';
      case 'under_investigation': return base + 'bg-blue-100 text-blue-800';
      case 'resolved': return base + 'bg-green-100 text-green-800';
      case 'escalated': return base + 'bg-red-100 text-red-800';
      default: return base + 'bg-gray-100 text-gray-800';
    }
  }

  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  }
}
