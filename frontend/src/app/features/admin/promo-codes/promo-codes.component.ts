import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PromoCodesService } from '@core/services/promo-codes.service';
import { PromoCode, CreatePromoCodeRequest, DiscountType } from '@core/models/promo-code.models';

@Component({
  selector: 'app-promo-codes',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container mx-auto px-4 py-8">
      <div class="flex justify-between items-center mb-6">
        <h1 class="text-3xl font-bold text-gray-800">Promo Codes</h1>
        <button
          (click)="showCreateForm = !showCreateForm"
          class="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700">
          Create Promo Code
        </button>
      </div>

      @if (showCreateForm) {
        <div class="mb-6 bg-white rounded-lg shadow-md p-6">
          <h2 class="text-xl font-semibold mb-4">Create New Promo Code</h2>
          <form (submit)="createPromoCode(); $event.preventDefault()" class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-2">Code *</label>
              <input
                [(ngModel)]="newPromoCode.code"
                name="code"
                type="text"
                required
                class="w-full px-3 py-2 border border-gray-300 rounded-md"
              />
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-2">Discount Type *</label>
              <select
                [(ngModel)]="newPromoCode.discountType"
                name="discountType"
                class="w-full px-3 py-2 border border-gray-300 rounded-md">
                <option value="percentage">Percentage</option>
                <option value="fixed_amount">Fixed Amount</option>
              </select>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-2">Discount Value *</label>
              <input
                [(ngModel)]="newPromoCode.discountValue"
                name="discountValue"
                type="number"
                step="0.01"
                required
                class="w-full px-3 py-2 border border-gray-300 rounded-md"
              />
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-2">Valid From *</label>
              <input
                [(ngModel)]="newPromoCode.validFrom"
                name="validFrom"
                type="datetime-local"
                required
                class="w-full px-3 py-2 border border-gray-300 rounded-md"
              />
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-2">Valid Until *</label>
              <input
                [(ngModel)]="newPromoCode.validUntil"
                name="validUntil"
                type="datetime-local"
                required
                class="w-full px-3 py-2 border border-gray-300 rounded-md"
              />
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-2">Usage Limit</label>
              <input
                [(ngModel)]="newPromoCode.usageLimit"
                name="usageLimit"
                type="number"
                class="w-full px-3 py-2 border border-gray-300 rounded-md"
              />
            </div>

            <div class="col-span-2">
              <label class="block text-sm font-medium text-gray-700 mb-2">Description</label>
              <textarea
                [(ngModel)]="newPromoCode.description"
                name="description"
                rows="3"
                class="w-full px-3 py-2 border border-gray-300 rounded-md"
              ></textarea>
            </div>

            <div class="col-span-2 flex gap-2">
              <button
                type="submit"
                class="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700">
                Create
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
          <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
        </div>
      } @else {
        <div class="bg-white rounded-lg shadow-md overflow-hidden">
          <table class="min-w-full divide-y divide-gray-200">
            <thead class="bg-gray-50">
              <tr>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Code</th>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Discount</th>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Usage</th>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Valid Until</th>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200">
              @for (promo of promoCodes; track promo.id) {
                <tr>
                  <td class="px-6 py-4 whitespace-nowrap font-mono font-semibold">{{ promo.code }}</td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    @if (promo.discountType === 'percentage') {
                      {{ promo.discountValue }}%
                    } @else {
                      \${{ promo.discountValue }}
                    }
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    {{ promo.usageCount }}{{ promo.usageLimit ? ' / ' + promo.usageLimit : '' }}
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap text-sm">
                    {{ formatDate(promo.validUntil) }}
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <span [class]="promo.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'"
                      class="inline-flex px-2 py-1 text-xs font-semibold rounded-full">
                      {{ promo.isActive ? 'Active' : 'Inactive' }}
                    </span>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap text-sm">
                    @if (promo.isActive) {
                      <button
                        (click)="deactivatePromoCode(promo.id)"
                        class="text-red-600 hover:text-red-900">
                        Deactivate
                      </button>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `
})
export class PromoCodesComponent implements OnInit {
  private promoCodesService = inject(PromoCodesService);

  promoCodes: PromoCode[] = [];
  isLoading = false;
  showCreateForm = false;

  newPromoCode: CreatePromoCodeRequest = {
    code: '',
    discountType: 'percentage' as DiscountType,
    discountValue: 0,
    validFrom: new Date(),
    validUntil: new Date()
  };

  ngOnInit(): void {
    this.loadPromoCodes();
  }

  loadPromoCodes(): void {
    this.isLoading = true;
    this.promoCodesService.getPromoCodes().subscribe({
      next: (codes) => {
        this.promoCodes = codes;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading promo codes:', error);
        this.isLoading = false;
      }
    });
  }

  createPromoCode(): void {
    this.promoCodesService.createPromoCode(this.newPromoCode).subscribe({
      next: () => {
        this.showCreateForm = false;
        this.loadPromoCodes();
      },
      error: (error) => {
        console.error('Error creating promo code:', error);
      }
    });
  }

  deactivatePromoCode(id: string): void {
    if (!confirm('Are you sure you want to deactivate this promo code?')) {
      return;
    }

    this.promoCodesService.deactivatePromoCode(id).subscribe({
      next: () => {
        this.loadPromoCodes();
      },
      error: (error) => {
        console.error('Error deactivating promo code:', error);
      }
    });
  }

  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  }
}
