import { Component, EventEmitter, Input, OnInit, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { SavedAddress, CreateSavedAddressDto } from '@core/api';

@Component({
  selector: 'app-address-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <form [formGroup]="addressForm" (ngSubmit)="onSubmit()" class="space-y-4">
      <!-- Label -->
      <div>
        <label class="block text-sm font-medium text-gray-700 mb-1">
          Address Label <span class="text-red-500">*</span>
        </label>
        <input
          type="text"
          formControlName="label"
          placeholder="e.g., Home, Office, Warehouse"
          class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          [class.border-red-500]="isFieldInvalid('label')"
        />
        <p *ngIf="isFieldInvalid('label')" class="mt-1 text-sm text-red-600">
          Label is required
        </p>
      </div>

      <!-- Street -->
      <div>
        <label class="block text-sm font-medium text-gray-700 mb-1">
          Street Address <span class="text-red-500">*</span>
        </label>
        <input
          type="text"
          formControlName="addressLine1"
          placeholder="123 High Street"
          class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          [class.border-red-500]="isFieldInvalid('addressLine1')"
        />
        <p *ngIf="isFieldInvalid('addressLine1')" class="mt-1 text-sm text-red-600">
          Street address is required
        </p>
      </div>

      <!-- Apartment/Suite -->
      <div>
        <label class="block text-sm font-medium text-gray-700 mb-1">
          Flat/Unit (Optional)
        </label>
        <input
          type="text"
          formControlName="addressLine2"
          placeholder="Flat 4B, Unit 200, Floor 3"
          class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
        />
      </div>

      <!-- City, County, Postal Code in a row -->
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <!-- City -->
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">
            City <span class="text-red-500">*</span>
          </label>
          <input
            type="text"
            formControlName="city"
            placeholder="London"
            class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            [class.border-red-500]="isFieldInvalid('city')"
          />
          <p *ngIf="isFieldInvalid('city')" class="mt-1 text-sm text-red-600">
            Required
          </p>
        </div>

        <!-- County -->
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">
            County
          </label>
          <input
            type="text"
            formControlName="county"
            placeholder="Greater London"
            class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          />
        </div>

        <!-- Postcode -->
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">
            Postcode <span class="text-red-500">*</span>
          </label>
          <input
            type="text"
            formControlName="postalCode"
            placeholder="SW1A 1AA"
            class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            [class.border-red-500]="isFieldInvalid('postalCode')"
          />
          <p *ngIf="isFieldInvalid('postalCode')" class="mt-1 text-sm text-red-600">
            Required
          </p>
        </div>
      </div>

      <!-- Country -->
      <div>
        <label class="block text-sm font-medium text-gray-700 mb-1">
          Country
        </label>
        <input
          type="text"
          formControlName="country"
          placeholder="United Kingdom"
          class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
        />
      </div>

      <!-- Special Instructions -->
      <div>
        <label class="block text-sm font-medium text-gray-700 mb-1">
          Delivery Instructions (Optional)
        </label>
        <textarea
          formControlName="specialInstructions"
          rows="3"
          placeholder="Special delivery instructions, gate codes, etc."
          class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
        ></textarea>
      </div>

      <!-- Set as Default -->
      <div class="flex items-center">
        <input
          type="checkbox"
          formControlName="isDefault"
          id="isDefault"
          class="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
        />
        <label for="isDefault" class="ml-2 text-sm text-gray-700">
          Set as my default address
        </label>
      </div>

      <!-- Form Actions -->
      <div class="flex justify-end gap-3 pt-4 border-t border-gray-200">
        <button
          type="button"
          (click)="onCancel()"
          class="px-4 py-2 text-gray-700 hover:text-gray-900 font-medium"
        >
          Cancel
        </button>
        <button
          type="submit"
          [disabled]="isSubmitting"
          class="px-6 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {{ isSubmitting ? 'Saving...' : 'Save Address' }}
        </button>
      </div>

      <!-- Error Message -->
      <div *ngIf="errorMessage" class="bg-red-50 border border-red-200 rounded-lg p-3">
        <p class="text-sm text-red-800">{{ errorMessage }}</p>
      </div>
    </form>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class AddressFormComponent implements OnInit {
  @Input() address: SavedAddress | null = null;
  @Output() save = new EventEmitter<CreateSavedAddressDto>();
  @Output() cancel = new EventEmitter<void>();

  private fb = inject(FormBuilder);

  addressForm!: FormGroup;
  isSubmitting = false;
  errorMessage = '';

  ngOnInit(): void {
    this.addressForm = this.fb.group({
      label: [this.address?.label || '', Validators.required],
      addressLine1: [this.address?.addressLine1 || '', Validators.required],
      addressLine2: [this.address?.addressLine2 || ''],
      city: [this.address?.city || '', Validators.required],
      county: [this.address?.county || ''],
      postalCode: [this.address?.postalCode || '', Validators.required],
      country: [this.address?.country || 'United Kingdom'],
      specialInstructions: [this.address?.specialInstructions || ''],
      isDefault: [this.address?.isDefault || false]
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.addressForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  onSubmit(): void {
    if (this.addressForm.invalid) {
      Object.keys(this.addressForm.controls).forEach(key => {
        this.addressForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.isSubmitting = false;
    this.errorMessage = '';

    const addressData: CreateSavedAddressDto = this.addressForm.value;
    this.save.emit(addressData);

    // Reset submitting state after delay
    setTimeout(() => {
      this.isSubmitting = false;
    }, 1000);
  }

  onCancel(): void {
    this.cancel.emit();
  }
}
