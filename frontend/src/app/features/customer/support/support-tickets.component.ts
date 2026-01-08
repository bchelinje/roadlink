import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { SupportService } from '@core/services/support.service';
import { SupportTicket, CreateTicketRequest, TicketCategory, TicketPriority } from '@core/models/support.models';

@Component({
  selector: 'app-support-tickets',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="container mx-auto px-4 py-8">
      <div class="flex justify-between items-center mb-6">
        <h1 class="text-3xl font-bold text-gray-800">Support Tickets</h1>
        <button
          (click)="showCreateForm = true"
          class="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700">
          Create Ticket
        </button>
      </div>

      @if (errorMessage) {
        <div class="mb-4 p-4 bg-red-100 text-red-700 rounded-lg">
          {{ errorMessage }}
        </div>
      }

      @if (successMessage) {
        <div class="mb-4 p-4 bg-green-100 text-green-700 rounded-lg">
          {{ successMessage }}
        </div>
      }

      <!-- Create Ticket Form -->
      @if (showCreateForm) {
        <div class="mb-6 bg-white rounded-lg shadow-md p-6">
          <h2 class="text-xl font-semibold mb-4">Create New Ticket</h2>
          <form (submit)="createTicket(); $event.preventDefault()">
            <div class="mb-4">
              <label class="block text-sm font-medium text-gray-700 mb-2">Subject *</label>
              <input
                [(ngModel)]="newTicket.subject"
                name="subject"
                type="text"
                required
                class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>

            <div class="mb-4">
              <label class="block text-sm font-medium text-gray-700 mb-2">Description *</label>
              <textarea
                [(ngModel)]="newTicket.description"
                name="description"
                rows="4"
                required
                class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              ></textarea>
            </div>

            <div class="grid grid-cols-2 gap-4 mb-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-2">Category *</label>
                <select
                  [(ngModel)]="newTicket.category"
                  name="category"
                  required
                  class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500">
                  <option value="general">General</option>
                  <option value="billing">Billing</option>
                  <option value="technical">Technical</option>
                  <option value="job_issue">Job Issue</option>
                  <option value="driver_issue">Driver Issue</option>
                  <option value="account">Account</option>
                  <option value="other">Other</option>
                </select>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 mb-2">Priority *</label>
                <select
                  [(ngModel)]="newTicket.priority"
                  name="priority"
                  required
                  class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500">
                  <option value="low">Low</option>
                  <option value="medium">Medium</option>
                  <option value="high">High</option>
                  <option value="urgent">Urgent</option>
                </select>
              </div>
            </div>

            <div class="flex gap-2">
              <button
                type="submit"
                [disabled]="isSubmitting"
                class="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50">
                {{ isSubmitting ? 'Creating...' : 'Create Ticket' }}
              </button>
              <button
                type="button"
                (click)="cancelCreate()"
                class="px-4 py-2 bg-gray-300 text-gray-700 rounded-lg hover:bg-gray-400">
                Cancel
              </button>
            </div>
          </form>
        </div>
      }

      <!-- Tickets List -->
      @if (isLoading) {
        <div class="flex justify-center items-center py-12">
          <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
        </div>
      } @else if (tickets.length === 0) {
        <div class="bg-white rounded-lg shadow-md p-12 text-center">
          <svg class="mx-auto h-12 w-12 text-gray-400 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
          </svg>
          <p class="text-gray-500">No support tickets yet</p>
        </div>
      } @else {
        <div class="bg-white rounded-lg shadow-md overflow-hidden">
          <table class="min-w-full divide-y divide-gray-200">
            <thead class="bg-gray-50">
              <tr>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Ticket #</th>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Subject</th>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Category</th>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Priority</th>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Created</th>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
              </tr>
            </thead>
            <tbody class="bg-white divide-y divide-gray-200">
              @for (ticket of tickets; track ticket.id) {
                <tr class="hover:bg-gray-50">
                  <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                    {{ ticket.ticketNumber }}
                  </td>
                  <td class="px-6 py-4 text-sm text-gray-900">
                    {{ ticket.subject }}
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    {{ formatCategory(ticket.category) }}
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <span [class]="getPriorityClass(ticket.priority)">
                      {{ formatPriority(ticket.priority) }}
                    </span>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <span [class]="getStatusClass(ticket.status)">
                      {{ formatStatus(ticket.status) }}
                    </span>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    {{ formatDate(ticket.createdAt) }}
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap text-sm">
                    <button
                      [routerLink]="['/customer/support', ticket.id]"
                      class="text-blue-600 hover:text-blue-900">
                      View
                    </button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `,
  styles: [`
    .status-badge {
      @apply inline-flex px-2 py-1 text-xs font-semibold rounded-full;
    }
  `]
})
export class SupportTicketsComponent implements OnInit {
  private supportService = inject(SupportService);
  private router = inject(Router);

  tickets: SupportTicket[] = [];
  isLoading = false;
  isSubmitting = false;
  errorMessage = '';
  successMessage = '';
  showCreateForm = false;

  newTicket: CreateTicketRequest = {
    subject: '',
    description: '',
    category: 'general' as TicketCategory,
    priority: 'medium' as TicketPriority
  };

  ngOnInit(): void {
    this.loadTickets();
  }

  loadTickets(): void {
    this.isLoading = true;
    this.supportService.getTickets().subscribe({
      next: (tickets) => {
        this.tickets = tickets;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading tickets:', error);
        this.errorMessage = 'Failed to load support tickets';
        this.isLoading = false;
      }
    });
  }

  createTicket(): void {
    if (!this.newTicket.subject || !this.newTicket.description) {
      this.errorMessage = 'Please fill in all required fields';
      return;
    }

    this.isSubmitting = true;
    this.supportService.createTicket(this.newTicket).subscribe({
      next: (ticket) => {
        this.successMessage = 'Support ticket created successfully!';
        this.showCreateForm = false;
        this.resetForm();
        this.loadTickets();
        this.isSubmitting = false;

        // Navigate to ticket detail after 1 second
        setTimeout(() => {
          this.router.navigate(['/customer/support', ticket.id]);
        }, 1000);
      },
      error: (error) => {
        console.error('Error creating ticket:', error);
        this.errorMessage = 'Failed to create support ticket';
        this.isSubmitting = false;
      }
    });
  }

  cancelCreate(): void {
    this.showCreateForm = false;
    this.resetForm();
  }

  resetForm(): void {
    this.newTicket = {
      subject: '',
      description: '',
      category: 'general' as TicketCategory,
      priority: 'medium' as TicketPriority
    };
  }

  formatCategory(category: TicketCategory): string {
    return category.toString().replace('_', ' ').replace(/\b\w/g, l => l.toUpperCase());
  }

  formatPriority(priority: TicketPriority): string {
    return priority.toString().charAt(0).toUpperCase() + priority.toString().slice(1);
  }

  formatStatus(status: string): string {
    return status.replace('_', ' ').replace(/\b\w/g, l => l.toUpperCase());
  }

  getPriorityClass(priority: TicketPriority): string {
    const classes = 'status-badge ';
    switch (priority) {
      case 'urgent': return classes + 'bg-red-100 text-red-800';
      case 'high': return classes + 'bg-orange-100 text-orange-800';
      case 'medium': return classes + 'bg-yellow-100 text-yellow-800';
      case 'low': return classes + 'bg-green-100 text-green-800';
      default: return classes + 'bg-gray-100 text-gray-800';
    }
  }

  getStatusClass(status: string): string {
    const classes = 'status-badge ';
    switch (status) {
      case 'open': return classes + 'bg-blue-100 text-blue-800';
      case 'in_progress': return classes + 'bg-yellow-100 text-yellow-800';
      case 'resolved': return classes + 'bg-green-100 text-green-800';
      case 'closed': return classes + 'bg-gray-100 text-gray-800';
      default: return classes + 'bg-gray-100 text-gray-800';
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
