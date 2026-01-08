import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PricingService, PricingRule } from '@core/api';

@Component({
  selector: 'app-pricing-rules',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="min-h-screen bg-gray-50 py-8">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        
        <!-- Page Header -->
        <div class="page-header">
          <div class="flex items-center justify-between w-full">
            <div>
              <h1>Pricing Rules</h1>
              <p class="subtitle">Manage pricing configuration and rules</p>
            </div>
            <button
              (click)="openCreateModal()"
              class="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 font-medium transition-colors flex items-center gap-2"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
              </svg>
              Create New Rule
            </button>
          </div>
        </div>

      <!-- Loading State -->
      <div *ngIf="isLoading" class="text-center py-12">
        <div class="inline-block w-12 h-12 border-4 border-gray-300 border-t-blue-600 rounded-full animate-spin"></div>
        <p class="mt-4 text-gray-600">Loading pricing rules...</p>
      </div>

      <!-- Success Message -->
      <div *ngIf="successMessage" class="mb-6 bg-green-50 border border-green-200 rounded-lg p-4">
        <p class="text-green-800">{{ successMessage }}</p>
      </div>

      <!-- Error State -->
      <div *ngIf="errorMessage && !isLoading" class="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
        <p class="text-red-800">{{ errorMessage }}</p>
      </div>

      <!-- Filter -->
      <div *ngIf="!isLoading" class="mb-6 flex items-center gap-4">
        <button
          (click)="filterRules(null)"
          [class]="activeFilter === null ? 'bg-blue-600 text-white' : 'bg-white text-gray-700'"
          class="px-4 py-2 border border-gray-300 rounded-lg font-medium transition-colors"
        >
          All ({{ pricingRules.length }})
        </button>
        <button
          (click)="filterRules(true)"
          [class]="activeFilter === true ? 'bg-green-600 text-white' : 'bg-white text-gray-700'"
          class="px-4 py-2 border border-gray-300 rounded-lg font-medium transition-colors"
        >
          Active ({{ countActiveRules() }})
        </button>
        <button
          (click)="filterRules(false)"
          [class]="activeFilter === false ? 'bg-gray-600 text-white' : 'bg-white text-gray-700'"
          class="px-4 py-2 border border-gray-300 rounded-lg font-medium transition-colors"
        >
          Inactive ({{ countInactiveRules() }})
        </button>
      </div>

      <!-- Empty State -->
      <div *ngIf="!isLoading && pricingRules.length === 0 && !errorMessage" class="text-center py-12">
        <svg class="w-24 h-24 mx-auto text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <h3 class="mt-4 text-lg font-medium text-gray-900">No pricing rules</h3>
        <p class="mt-2 text-gray-600">Create pricing rules to customize your pricing structure</p>
      </div>

      <!-- Pricing Rules List -->
      <div *ngIf="!isLoading && filteredRules.length > 0" class="space-y-4">
        <div
          *ngFor="let rule of filteredRules"
          class="bg-white border border-gray-200 rounded-lg p-6 hover:shadow-md transition-shadow"
          [class.opacity-60]="!rule.isActive"
        >
          <!-- Rule Header -->
          <div class="flex items-start justify-between mb-4">
            <div class="flex-1">
              <div class="flex items-center gap-3 mb-2">
                <h3 class="text-lg font-semibold text-gray-900">{{ rule.name }}</h3>
                <span
                  [class]="getRuleTypeClass(rule.type)"
                  class="px-2 py-0.5 text-xs font-medium rounded"
                >
                  {{ rule.type }}
                </span>
                <span
                  [class]="rule.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'"
                  class="px-2 py-0.5 text-xs font-medium rounded"
                >
                  {{ rule.isActive ? 'Active' : 'Inactive' }}
                </span>
              </div>
              <p *ngIf="rule.description" class="text-sm text-gray-600">{{ rule.description }}</p>
            </div>

            <!-- Quick Toggle -->
            <div class="flex items-center gap-2">
              <button
                (click)="toggleRule(rule)"
                [disabled]="isToggling"
                class="px-3 py-1.5 text-sm font-medium rounded-lg transition-colors"
                [class]="rule.isActive ? 'text-orange-600 hover:text-orange-700' : 'text-green-600 hover:text-green-700'"
              >
                {{ rule.isActive ? 'Deactivate' : 'Activate' }}
              </button>
            </div>
          </div>

          <!-- Rule Details Grid -->
          <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
            <!-- Vehicle Type -->
            <div *ngIf="rule.vehicleType">
              <p class="text-xs text-gray-500 mb-1">Vehicle Type</p>
              <p class="font-medium text-gray-900">{{ rule.vehicleType }}</p>
            </div>

            <!-- Distance Range -->
            <div *ngIf="rule.minDistance || rule.maxDistance">
              <p class="text-xs text-gray-500 mb-1">Distance Range</p>
              <p class="font-medium text-gray-900">
                {{ rule.minDistance || 0 }} - {{ rule.maxDistance || '∞' }} miles
              </p>
            </div>

            <!-- Time Range -->
            <div *ngIf="rule.startTime || rule.endTime">
              <p class="text-xs text-gray-500 mb-1">Time Range</p>
              <p class="font-medium text-gray-900">
                {{ rule.startTime || '00:00' }} - {{ rule.endTime || '23:59' }}
              </p>
            </div>

            <!-- Fixed Amount -->
            <div *ngIf="rule.fixedAmount">
              <p class="text-xs text-gray-500 mb-1">Fixed Amount</p>
              <p class="font-medium text-green-600">\${{ rule.fixedAmount.toFixed(2) }}</p>
            </div>

            <!-- Per Mile Rate -->
            <div *ngIf="rule.perMileRate">
              <p class="text-xs text-gray-500 mb-1">Per Mile Rate</p>
              <p class="font-medium text-green-600">\${{ rule.perMileRate.toFixed(2) }}</p>
            </div>

            <!-- Multiplier -->
            <div *ngIf="rule.multiplierPercentage">
              <p class="text-xs text-gray-500 mb-1">Multiplier</p>
              <p class="font-medium text-orange-600">{{ rule.multiplierPercentage }}%</p>
            </div>

            <!-- Priority -->
            <div>
              <p class="text-xs text-gray-500 mb-1">Priority</p>
              <p class="font-medium text-gray-900">{{ rule.priority || 0 }}</p>
            </div>
          </div>

          <!-- Day Filters -->
          <div *ngIf="rule.weekendOnly || rule.weekdayOnly" class="mb-4 flex items-center gap-2">
            <span *ngIf="rule.weekendOnly" class="px-2 py-1 bg-purple-100 text-purple-800 text-xs rounded">
              Weekends Only
            </span>
            <span *ngIf="rule.weekdayOnly" class="px-2 py-1 bg-blue-100 text-blue-800 text-xs rounded">
              Weekdays Only
            </span>
          </div>

          <!-- Metadata -->
          <div class="pt-4 border-t border-gray-200 flex items-center justify-between text-xs text-gray-500">
            <span>Created {{ formatDate(rule.createdAt) }}</span>
            <span *ngIf="rule.createdBy">by {{ rule.createdBy }}</span>
          </div>

          <!-- Actions -->
          <div class="flex items-center gap-2 mt-4 pt-4 border-t border-gray-200">
            <button
              (click)="openEditModal(rule)"
              class="px-4 py-2 text-blue-600 hover:text-blue-700 rounded-lg font-medium text-sm transition-colors"
            >
              Edit
            </button>
            <button
              (click)="deleteRule(rule)"
              class="px-4 py-2 text-red-600 hover:text-red-700 rounded-lg font-medium text-sm transition-colors"
            >
              Delete
            </button>
          </div>
        </div>
      </div>

      <!-- Create/Edit Modal -->
      <div *ngIf="showModal" class="modal-overlay" (click)="closeModal()">
        <div class="modal-content" (click)="$event.stopPropagation()">
          <!-- Modal Header -->
          <div class="modal-header">
            <h2 class="text-xl font-bold text-gray-900">
              {{ isEditMode ? 'Edit Pricing Rule' : 'Create New Pricing Rule' }}
            </h2>
            <button (click)="closeModal()" class="btn-close">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <!-- Modal Body -->
          <form (ngSubmit)="saveRule()" class="modal-body">
            <!-- Basic Information -->
            <div class="form-section">
              <h3 class="form-section-title">Basic Information</h3>

              <div class="form-group">
                <label class="form-label required">Rule Name</label>
                <input
                  type="text"
                  [(ngModel)]="currentRule.name"
                  name="name"
                  class="form-control"
                  placeholder="e.g., Base Fare - Standard Van"
                  required>
              </div>

              <div class="form-group">
                <label class="form-label">Description</label>
                <textarea
                  [(ngModel)]="currentRule.description"
                  name="description"
                  class="form-control"
                  rows="2"
                  placeholder="Optional description"></textarea>
              </div>

              <div class="grid grid-cols-2 gap-4">
                <div class="form-group">
                  <label class="form-label required">Rule Type</label>
                  <select [(ngModel)]="currentRule.type" name="type" class="form-control" required>
                    <option value="">Select type...</option>
                    <option value="BaseFare">Base Fare</option>
                    <option value="DistanceBased">Distance Based</option>
                    <option value="TimeBased">Time Based</option>
                    <option value="Surge">Surge Pricing</option>
                    <option value="ServiceAddon">Service Addon</option>
                  </select>
                </div>

                <div class="form-group">
                  <label class="form-label">Vehicle Type</label>
                  <select [(ngModel)]="currentRule.vehicleType" name="vehicleType" class="form-control">
                    <option value="">All Vehicles</option>
                    <option value="van">Van</option>
                    <option value="cargo_van">Cargo Van</option>
                    <option value="small_truck">Small Truck</option>
                    <option value="large_truck">Large Truck</option>
                    <option value="box_truck">Box Truck</option>
                  </select>
                </div>
              </div>
            </div>

            <!-- Pricing Configuration -->
            <div class="form-section">
              <h3 class="form-section-title">Pricing Configuration</h3>

              <div class="grid grid-cols-3 gap-4">
                <div class="form-group">
                  <label class="form-label">Fixed Amount ($)</label>
                  <input
                    type="number"
                    [(ngModel)]="currentRule.fixedAmount"
                    name="fixedAmount"
                    class="form-control"
                    step="0.01"
                    min="0"
                    placeholder="0.00">
                </div>

                <div class="form-group">
                  <label class="form-label">Per Mile Rate ($)</label>
                  <input
                    type="number"
                    [(ngModel)]="currentRule.perMileRate"
                    name="perMileRate"
                    class="form-control"
                    step="0.01"
                    min="0"
                    placeholder="0.00">
                </div>

                <div class="form-group">
                  <label class="form-label">Multiplier (%)</label>
                  <input
                    type="number"
                    [(ngModel)]="currentRule.multiplierPercentage"
                    name="multiplierPercentage"
                    class="form-control"
                    step="1"
                    min="0"
                    placeholder="0">
                </div>
              </div>
            </div>

            <!-- Distance Range -->
            <div class="form-section">
              <h3 class="form-section-title">Distance Range (Optional)</h3>

              <div class="grid grid-cols-2 gap-4">
                <div class="form-group">
                  <label class="form-label">Min Distance (miles)</label>
                  <input
                    type="number"
                    [(ngModel)]="currentRule.minDistance"
                    name="minDistance"
                    class="form-control"
                    step="0.1"
                    min="0"
                    placeholder="0">
                </div>

                <div class="form-group">
                  <label class="form-label">Max Distance (miles)</label>
                  <input
                    type="number"
                    [(ngModel)]="currentRule.maxDistance"
                    name="maxDistance"
                    class="form-control"
                    step="0.1"
                    min="0"
                    placeholder="Unlimited">
                </div>
              </div>
            </div>

            <!-- Time Range -->
            <div class="form-section">
              <h3 class="form-section-title">Time Range (Optional)</h3>

              <div class="grid grid-cols-2 gap-4">
                <div class="form-group">
                  <label class="form-label">Start Time</label>
                  <input
                    type="time"
                    [(ngModel)]="currentRule.startTime"
                    name="startTime"
                    class="form-control">
                </div>

                <div class="form-group">
                  <label class="form-label">End Time</label>
                  <input
                    type="time"
                    [(ngModel)]="currentRule.endTime"
                    name="endTime"
                    class="form-control">
                </div>
              </div>
            </div>

            <!-- Day Filters -->
            <div class="form-section">
              <h3 class="form-section-title">Day Filters</h3>

              <div class="flex gap-4">
                <label class="flex items-center gap-2">
                  <input
                    type="checkbox"
                    [(ngModel)]="currentRule.weekendOnly"
                    name="weekendOnly"
                    class="rounded">
                  <span class="text-sm text-gray-700">Weekend Only</span>
                </label>

                <label class="flex items-center gap-2">
                  <input
                    type="checkbox"
                    [(ngModel)]="currentRule.weekdayOnly"
                    name="weekdayOnly"
                    class="rounded">
                  <span class="text-sm text-gray-700">Weekday Only</span>
                </label>
              </div>
            </div>

            <!-- Priority and Status -->
            <div class="form-section">
              <h3 class="form-section-title">Priority & Status</h3>

              <div class="grid grid-cols-2 gap-4">
                <div class="form-group">
                  <label class="form-label">Priority</label>
                  <input
                    type="number"
                    [(ngModel)]="currentRule.priority"
                    name="priority"
                    class="form-control"
                    placeholder="0"
                    min="0">
                  <p class="text-xs text-gray-500 mt-1">Higher priority rules are applied first</p>
                </div>

                <div class="form-group">
                  <label class="flex items-center gap-2 mt-6">
                    <input
                      type="checkbox"
                      [(ngModel)]="currentRule.isActive"
                      name="isActive"
                      class="rounded">
                    <span class="text-sm font-medium text-gray-700">Active</span>
                  </label>
                </div>
              </div>
            </div>

            <!-- Modal Footer -->
            <div class="modal-footer">
              <button
                type="button"
                (click)="closeModal()"
                class="btn btn-outline"
                [disabled]="isSaving">
                Cancel
              </button>
              <button
                type="submit"
                class="btn btn-primary"
                [disabled]="isSaving">
                {{ isSaving ? 'Saving...' : (isEditMode ? 'Update Rule' : 'Create Rule') }}
              </button>
            </div>
          </form>
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
      margin-bottom: 2rem;
    }

    .page-header h1 {
      font-size: 2rem;
      font-weight: 700;
      color: #111827;
      margin: 0;
    }

    .subtitle {
      color: #6b7280;
      margin-top: 0.25rem;
    }

    /* Modal Styles */
    .modal-overlay {
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.5);
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
      max-width: 48rem;
      width: 100%;
      max-height: 90vh;
      overflow-y: auto;
      box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
    }

    .modal-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 1.5rem;
      border-bottom: 1px solid #e5e7eb;
      position: sticky;
      top: 0;
      background: white;
      z-index: 10;
    }

    .btn-close {
      background: none;
      border: none;
      color: #6b7280;
      cursor: pointer;
      padding: 0.25rem;
    }

    .btn-close:hover {
      color: #111827;
    }

    .modal-body {
      padding: 1.5rem;
    }

    .modal-footer {
      display: flex;
      gap: 0.75rem;
      justify-content: flex-end;
      padding: 1.5rem;
      border-top: 1px solid #e5e7eb;
      background: #f9fafb;
    }

    .form-section {
      margin-bottom: 2rem;
      padding-bottom: 1.5rem;
      border-bottom: 1px solid #e5e7eb;
    }

    .form-section:last-child {
      border-bottom: none;
    }

    .form-section-title {
      font-size: 1rem;
      font-weight: 600;
      color: #111827;
      margin-bottom: 1rem;
    }

    .form-group {
      margin-bottom: 1rem;
    }

    .form-label {
      display: block;
      font-size: 0.875rem;
      font-weight: 500;
      color: #374151;
      margin-bottom: 0.5rem;
    }

    .form-label.required::after {
      content: " *";
      color: #ef4444;
    }

    .form-control {
      width: 100%;
      padding: 0.5rem 0.75rem;
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

    .btn {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      padding: 0.5rem 1rem;
      border-radius: 0.5rem;
      font-weight: 500;
      font-size: 0.875rem;
      transition: all 0.2s;
      cursor: pointer;
      border: none;
    }

    .btn:disabled {
      opacity: 0.5;
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
  `]
})
export class PricingRulesComponent implements OnInit {
  private pricingService = inject(PricingService);

  pricingRules: PricingRule[] = [];
  filteredRules: PricingRule[] = [];
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  isToggling = false;
  activeFilter: boolean | null = null;

  // Modal state
  showModal = false;
  isEditMode = false;
  isSaving = false;
  currentRule: PricingRule = this.getEmptyRule();

  ngOnInit(): void {
    this.loadPricingRules();
  }

  getEmptyRule(): PricingRule {
    return {
      name: '',
      type: '',
      description: null,
      vehicleType: null,
      minDistance: null,
      maxDistance: null,
      startTime: null,
      endTime: null,
      weekendOnly: false,
      weekdayOnly: false,
      fixedAmount: null,
      perMileRate: null,
      perMinuteRate: null,
      multiplierPercentage: null,
      serviceAddonType: null,
      priority: 0,
      isActive: true
    };
  }

  loadPricingRules(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.pricingService.apiPricingRulesGet().subscribe({
      next: (rules: PricingRule[]) => {
        this.pricingRules = rules || [];
        this.applyFilter();
        this.isLoading = false;
      },
      error: (error: any) => {
        console.error('Error loading pricing rules:', error);
        this.errorMessage = 'Failed to load pricing rules. Please try again.';
        this.isLoading = false;
      }
    });
  }

  filterRules(isActive: boolean | null): void {
    this.activeFilter = isActive;
    this.applyFilter();
  }

  applyFilter(): void {
    if (this.activeFilter === null) {
      this.filteredRules = this.pricingRules;
    } else {
      this.filteredRules = this.pricingRules.filter(r => r.isActive === this.activeFilter);
    }
  }

  countActiveRules(): number {
    return this.pricingRules.filter(r => r.isActive).length;
  }

  countInactiveRules(): number {
    return this.pricingRules.filter(r => !r.isActive).length;
  }

  toggleRule(rule: PricingRule): void {
    if (!rule.id) return;

    this.isToggling = true;
    this.errorMessage = '';

    this.pricingService.apiPricingRulesIdTogglePatch(rule.id).subscribe({
      next: () => {
        this.successMessage = `Rule "${rule.name}" has been ${rule.isActive ? 'deactivated' : 'activated'}`;
        this.loadPricingRules();
        this.isToggling = false;
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (error: any) => {
        console.error('Error toggling rule:', error);
        this.errorMessage = 'Failed to toggle rule. Please try again.';
        this.isToggling = false;
      }
    });
  }

  deleteRule(rule: PricingRule): void {
    if (!confirm(`Delete pricing rule "${rule.name}"? This cannot be undone.`)) {
      return;
    }

    if (!rule.id) return;

    this.pricingService.apiPricingRulesIdDelete(rule.id).subscribe({
      next: () => {
        this.successMessage = `Rule "${rule.name}" deleted successfully`;
        this.loadPricingRules();
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (error: any) => {
        console.error('Error deleting rule:', error);
        this.errorMessage = 'Failed to delete rule. Please try again.';
      }
    });
  }

  getRuleTypeClass(type: string | null | undefined): string {
    switch (type) {
      case 'BaseFare':
        return 'bg-blue-100 text-blue-800';
      case 'DistanceBased':
        return 'bg-green-100 text-green-800';
      case 'TimeBased':
        return 'bg-purple-100 text-purple-800';
      case 'Surge':
        return 'bg-orange-100 text-orange-800';
      case 'ServiceAddon':
        return 'bg-pink-100 text-pink-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  formatDate(dateString: string | undefined): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }

  openCreateModal(): void {
    this.isEditMode = false;
    this.currentRule = this.getEmptyRule();
    this.showModal = true;
  }

  openEditModal(rule: PricingRule): void {
    this.isEditMode = true;
    this.currentRule = { ...rule }; // Create a copy
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.currentRule = this.getEmptyRule();
    this.errorMessage = '';
  }

  saveRule(): void {
    this.isSaving = true;
    this.errorMessage = '';

    const observable = this.isEditMode && this.currentRule.id
      ? this.pricingService.apiPricingRulesIdPut(this.currentRule.id, this.currentRule)
      : this.pricingService.apiPricingRulesPost(this.currentRule);

    observable.subscribe({
      next: () => {
        this.successMessage = `Pricing rule ${this.isEditMode ? 'updated' : 'created'} successfully`;
        this.isSaving = false;
        this.closeModal();
        this.loadPricingRules();
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (error: any) => {
        console.error('Error saving pricing rule:', error);
        this.errorMessage = error.error?.title || `Failed to ${this.isEditMode ? 'update' : 'create'} pricing rule. Please try again.`;
        this.isSaving = false;
      }
    });
  }
}
