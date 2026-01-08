import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PricingService } from '@core/api';

@Component({
  selector: 'app-price-calculator',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container mx-auto px-4 py-8 max-w-3xl">
      <div class="page-header">
        <div>
          <h1>Price Calculator</h1>
          <p class="subtitle">Get an instant price estimate for your delivery</p>
        </div>
      </div>

      <!-- Calculator Form -->
      <div class="bg-white border border-gray-200 rounded-lg p-6 mb-6">
        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">
              Distance (miles)
            </label>
            <input
              type="number"
              [(ngModel)]="distance"
              min="0"
              step="0.1"
              placeholder="e.g., 5.5"
              class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">
              Vehicle Type
            </label>
            <select
              [(ngModel)]="vehicleType"
              class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="">Select vehicle type</option>
              <option value="Car">Car</option>
              <option value="Van">Van</option>
              <option value="Truck">Truck</option>
              <option value="Motorcycle">Motorcycle</option>
            </select>
          </div>

          <button
            (click)="calculatePrice()"
            [disabled]="!distance || isCalculating"
            class="w-full px-6 py-3 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {{ isCalculating ? 'Calculating...' : 'Calculate Price' }}
          </button>
        </div>
      </div>

      <!-- Error Message -->
      <div *ngIf="errorMessage" class="mb-6 bg-red-50 border border-red-200 rounded-lg p-4">
        <p class="text-red-800">{{ errorMessage }}</p>
      </div>

      <!-- Price Estimate Result -->
      <div *ngIf="priceEstimate" class="bg-gradient-to-r from-blue-500 to-purple-600 rounded-lg p-8 text-white">
        <div class="text-center">
          <p class="text-sm opacity-90 mb-2">Estimated Price</p>
          <p class="text-5xl font-bold mb-4">\${{ priceEstimate.basePrice?.toFixed(2) || '0.00' }}</p>

          <div class="grid grid-cols-2 gap-4 mt-6 pt-6 border-t border-white/20">
            <div>
              <p class="text-xs opacity-75 mb-1">Base Rate</p>
              <p class="text-lg font-semibold">\${{ priceEstimate.baseFare?.toFixed(2) || '0.00' }}</p>
            </div>

            <div>
              <p class="text-xs opacity-75 mb-1">Per Mile</p>
              <p class="text-lg font-semibold">\${{ priceEstimate.perMileRate?.toFixed(2) || '0.00' }}</p>
            </div>

            <div *ngIf="priceEstimate.surgeMultiplier && priceEstimate.surgeMultiplier > 1" class="col-span-2">
              <div class="bg-orange-500/30 rounded-lg p-3">
                <p class="text-xs mb-1">Surge Pricing Active</p>
                <p class="text-lg font-semibold">{{ priceEstimate.surgeMultiplier }}x multiplier</p>
              </div>
            </div>
          </div>

          <p class="text-xs mt-6 opacity-75">
            * This is an estimate. Final price may vary based on actual route and conditions.
          </p>
        </div>
      </div>

      <!-- Pricing Info -->
      <div class="mt-8 bg-gray-50 rounded-lg p-6">
        <h3 class="text-sm font-semibold text-gray-900 mb-3">How pricing works</h3>
        <ul class="space-y-2 text-sm text-gray-600">
          <li class="flex items-start gap-2">
            <svg class="w-5 h-5 text-blue-600 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
            </svg>
            <span>Base fare + distance-based pricing</span>
          </li>
          <li class="flex items-start gap-2">
            <svg class="w-5 h-5 text-blue-600 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
            </svg>
            <span>Vehicle type affects pricing</span>
          </li>
          <li class="flex items-start gap-2">
            <svg class="w-5 h-5 text-blue-600 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
            </svg>
            <span>Surge pricing may apply during peak hours</span>
          </li>
          <li class="flex items-start gap-2">
            <svg class="w-5 h-5 text-blue-600 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
            </svg>
            <span>Additional charges may apply for special requirements</span>
          </li>
        </ul>
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
export class PriceCalculatorComponent {
  private pricingService = inject(PricingService);

  distance: number | null = null;
  vehicleType = '';
  priceEstimate: any = null;
  isCalculating = false;
  errorMessage = '';

  calculatePrice(): void {
    if (!this.distance) {
      this.errorMessage = 'Please enter a distance';
      return;
    }

    this.isCalculating = true;
    this.errorMessage = '';
    this.priceEstimate = null;

    this.pricingService.apiPricingEstimateGet(
      this.distance,
      this.vehicleType || undefined
    ).subscribe({
      next: (estimate: any) => {
        this.priceEstimate = estimate;
        this.isCalculating = false;
      },
      error: (error: any) => {
        console.error('Error calculating price:', error);
        this.errorMessage = 'Failed to calculate price. Please try again.';
        this.isCalculating = false;
      }
    });
  }
}
