import { Component, inject, Output, EventEmitter, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MapsService, DistanceResult } from '@core/services/maps.service';
import { PricingService } from '@core/api';

export interface DistanceCalculationResult {
  pickupAddress: string;
  deliveryAddress: string;
  distance: DistanceResult;
  pricing?: any;
}

@Component({
  selector: 'app-distance-calculator',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="distance-calculator">
      <div class="space-y-4">
        <!-- Pickup Address -->
        <div class="relative">
          <label class="block text-sm font-medium text-gray-700 mb-1">
            Pickup Address <span class="text-red-500">*</span>
          </label>
          <input
            type="text"
            [(ngModel)]="pickupAddress"
            (ngModelChange)="onPickupChange($event)"
            (focus)="pickupFocused = true"
            (blur)="onPickupBlur()"
            class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            placeholder="Enter pickup address"
          />
          <!-- Pickup Autocomplete Suggestions -->
          <div
            *ngIf="pickupSuggestions.length > 0 && pickupFocused"
            class="absolute z-10 w-full mt-1 bg-white border border-gray-300 rounded-md shadow-lg max-h-60 overflow-auto"
          >
            <div
              *ngFor="let suggestion of pickupSuggestions"
              (mousedown)="selectPickupSuggestion(suggestion)"
              class="px-3 py-2 hover:bg-blue-50 cursor-pointer text-sm"
            >
              {{ suggestion }}
            </div>
          </div>
        </div>

        <!-- Delivery Address -->
        <div class="relative">
          <label class="block text-sm font-medium text-gray-700 mb-1">
            Delivery Address <span class="text-red-500">*</span>
          </label>
          <input
            type="text"
            [(ngModel)]="deliveryAddress"
            (ngModelChange)="onDeliveryChange($event)"
            (focus)="deliveryFocused = true"
            (blur)="onDeliveryBlur()"
            class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            placeholder="Enter delivery address"
          />
          <!-- Delivery Autocomplete Suggestions -->
          <div
            *ngIf="deliverySuggestions.length > 0 && deliveryFocused"
            class="absolute z-10 w-full mt-1 bg-white border border-gray-300 rounded-md shadow-lg max-h-60 overflow-auto"
          >
            <div
              *ngFor="let suggestion of deliverySuggestions"
              (mousedown)="selectDeliverySuggestion(suggestion)"
              class="px-3 py-2 hover:bg-blue-50 cursor-pointer text-sm"
            >
              {{ suggestion }}
            </div>
          </div>
        </div>

        <!-- Calculate Button -->
        <button
          (click)="calculateDistance()"
          [disabled]="!pickupAddress || !deliveryAddress || isCalculating"
          class="w-full px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-md font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {{ isCalculating ? 'Calculating...' : 'Calculate Distance & Price' }}
        </button>

        <!-- Error Message -->
        <div *ngIf="errorMessage" class="bg-red-50 border border-red-200 text-red-700 px-3 py-2 rounded-md text-sm">
          {{ errorMessage }}
        </div>

        <!-- Distance Result -->
        <div *ngIf="distanceResult" class="bg-blue-50 border border-blue-200 rounded-md p-4">
          <h3 class="text-sm font-semibold text-blue-900 mb-2">Distance & Duration</h3>
          <div class="grid grid-cols-2 gap-3 text-sm">
            <div>
              <p class="text-gray-600">Distance</p>
              <p class="font-semibold text-gray-900">{{ distanceResult.distanceText }}</p>
              <p class="text-xs text-gray-500">{{ distanceResult.distanceInMiles.toFixed(2) }} miles</p>
            </div>
            <div>
              <p class="text-gray-600">Duration</p>
              <p class="font-semibold text-gray-900">{{ distanceResult.durationText }}</p>
              <p class="text-xs text-gray-500">{{ distanceResult.durationInMinutes }} minutes</p>
            </div>
          </div>
        </div>

        <!-- Price Estimate -->
        <div *ngIf="priceEstimate && showPricing" class="bg-green-50 border border-green-200 rounded-md p-4">
          <h3 class="text-sm font-semibold text-green-900 mb-2">Price Estimate</h3>
          <div class="text-center">
            <p class="text-3xl font-bold text-green-700">\${{ priceEstimate.estimatedPrice?.toFixed(2) || '0.00' }}</p>
            <p class="text-xs text-gray-600 mt-1">
              Vehicle: {{ vehicleType || 'Standard' }}
            </p>
            <p class="text-xs text-gray-500 mt-2">
              * Final price may vary based on actual conditions
            </p>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class DistanceCalculatorComponent {
  private mapsService = inject(MapsService);
  private pricingService = inject(PricingService);

  @Input() vehicleType: string = '';
  @Input() showPricing: boolean = true;
  @Output() distanceCalculated = new EventEmitter<DistanceCalculationResult>();

  pickupAddress = '';
  deliveryAddress = '';
  pickupSuggestions: string[] = [];
  deliverySuggestions: string[] = [];
  pickupFocused = false;
  deliveryFocused = false;
  isCalculating = false;
  errorMessage = '';
  distanceResult: DistanceResult | null = null;
  priceEstimate: any = null;

  onPickupChange(value: string): void {
    if (value.length >= 3) {
      this.mapsService.autocompleteAddress(value, (suggestions) => {
        this.pickupSuggestions = suggestions;
      });
    } else {
      this.pickupSuggestions = [];
    }
  }

  onDeliveryChange(value: string): void {
    if (value.length >= 3) {
      this.mapsService.autocompleteAddress(value, (suggestions) => {
        this.deliverySuggestions = suggestions;
      });
    } else {
      this.deliverySuggestions = [];
    }
  }

  selectPickupSuggestion(suggestion: string): void {
    this.pickupAddress = suggestion;
    this.pickupSuggestions = [];
  }

  selectDeliverySuggestion(suggestion: string): void {
    this.deliveryAddress = suggestion;
    this.deliverySuggestions = [];
  }

  onPickupBlur(): void {
    setTimeout(() => {
      this.pickupFocused = false;
      this.pickupSuggestions = [];
    }, 200);
  }

  onDeliveryBlur(): void {
    setTimeout(() => {
      this.deliveryFocused = false;
      this.deliverySuggestions = [];
    }, 200);
  }

  calculateDistance(): void {
    if (!this.pickupAddress || !this.deliveryAddress) {
      this.errorMessage = 'Please enter both pickup and delivery addresses';
      return;
    }

    this.isCalculating = true;
    this.errorMessage = '';
    this.distanceResult = null;
    this.priceEstimate = null;

    this.mapsService.calculateDistance(this.pickupAddress, this.deliveryAddress).subscribe({
      next: (result) => {
        this.distanceResult = result;

        // Calculate price if requested
        if (this.showPricing && result.distanceInMiles) {
          this.calculatePrice(result.distanceInMiles);
        } else {
          this.isCalculating = false;
          this.emitResult();
        }
      },
      error: (error) => {
        console.error('Error calculating distance:', error);
        this.errorMessage = error.error?.message || 'Failed to calculate distance. Please check your addresses.';
        this.isCalculating = false;
      }
    });
  }

  private calculatePrice(distance: number): void {
    this.pricingService.apiPricingEstimateGet(
      distance,
      this.vehicleType || undefined
    ).subscribe({
      next: (estimate) => {
        this.priceEstimate = estimate;
        this.isCalculating = false;
        this.emitResult();
      },
      error: (error) => {
        console.error('Error calculating price:', error);
        // Don't show error, just finish without pricing
        this.isCalculating = false;
        this.emitResult();
      }
    });
  }

  private emitResult(): void {
    if (this.distanceResult) {
      this.distanceCalculated.emit({
        pickupAddress: this.pickupAddress,
        deliveryAddress: this.deliveryAddress,
        distance: this.distanceResult,
        pricing: this.priceEstimate
      });
    }
  }
}
