import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { CustomersService, SavedAddress, CreateSavedAddressDto } from '@core/api';
import { AddressFormComponent } from './address-form.component';

@Component({
  selector: 'app-addresses-list',
  standalone: true,
  imports: [CommonModule, RouterModule, AddressFormComponent],
  template: `
    <div class="container mx-auto px-4 py-8">
      <div class="page-header">
        <div>
          <h1>Saved Addresses</h1>
          <p class="subtitle">Manage your frequently used addresses for faster booking</p>
        </div>
        <button
          (click)="showAddressForm = true"
          class="btn-primary"
        >
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
          </svg>
          Add New Address
        </button>
      </div>

      <!-- Loading State -->
      <div *ngIf="isLoading" class="text-center py-12">
        <div class="inline-block w-12 h-12 border-4 border-gray-300 border-t-blue-600 rounded-full animate-spin"></div>
        <p class="mt-4 text-gray-600">Loading addresses...</p>
      </div>

      <!-- Error State -->
      <div *ngIf="errorMessage && !isLoading" class="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
        <p class="text-red-800">{{ errorMessage }}</p>
      </div>

      <!-- Empty State -->
      <div *ngIf="!isLoading && addresses.length === 0 && !errorMessage" class="text-center py-12">
        <svg class="w-24 h-24 mx-auto text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
        </svg>
        <h3 class="mt-4 text-lg font-medium text-gray-900">No saved addresses</h3>
        <p class="mt-2 text-gray-600">Add an address to save time on future bookings</p>
        <button
          (click)="showAddressForm = true"
          class="mt-4 bg-blue-600 hover:bg-blue-700 text-white px-6 py-2 rounded-lg font-medium transition-colors"
        >
          Add Your First Address
        </button>
      </div>

      <!-- Addresses Grid -->
      <div *ngIf="!isLoading && addresses.length > 0" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        <div
          *ngFor="let address of addresses"
          class="bg-white border border-gray-200 rounded-lg p-6 hover:shadow-lg transition-shadow relative"
        >
          <!-- Default Badge -->
          <div
            *ngIf="address.isDefault"
            class="absolute top-4 right-4 bg-green-100 text-green-800 text-xs font-semibold px-2 py-1 rounded"
          >
            Default
          </div>

          <!-- Address Label -->
          <h3 class="text-lg font-semibold text-gray-900 mb-3">{{ address.label }}</h3>

          <!-- Address Details -->
          <div class="text-gray-600 space-y-1 mb-4">
            <p>{{ address.addressLine1 }}</p>
            <p *ngIf="address.addressLine2">{{ address.addressLine2 }}</p>
            <p>{{ address.city }}, {{ address.county }} {{ address.postalCode }}</p>
            <p *ngIf="address.country">{{ address.country }}</p>
          </div>

          <!-- Notes -->
          <p *ngIf="address.specialInstructions" class="text-sm text-gray-500 italic mb-4">
            "{{ address.specialInstructions }}"
          </p>

          <!-- Actions -->
          <div class="flex items-center gap-2">
            <button
              *ngIf="!address.isDefault"
              (click)="setAsDefault(address.id!)"
              class="text-sm text-blue-600 hover:text-blue-700 font-medium"
            >
              Set as Default
            </button>
            <button
              (click)="editAddress(address)"
              class="text-sm text-gray-600 hover:text-gray-900 font-medium"
            >
              Edit
            </button>
            <button
              (click)="deleteAddress(address.id!)"
              class="text-sm text-red-600 hover:text-red-700 font-medium ml-auto"
            >
              Delete
            </button>
          </div>
        </div>
      </div>

      <!-- Address Form Modal -->
      <div
        *ngIf="showAddressForm"
        class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50"
        (click)="closeForm()"
      >
        <div
          class="bg-white rounded-lg max-w-2xl w-full max-h-[90vh] overflow-y-auto"
          (click)="$event.stopPropagation()"
        >
          <div class="p-6">
            <div class="flex justify-between items-center mb-6">
              <h2 class="text-xl font-bold text-gray-900">
                {{ editingAddress ? 'Edit Address' : 'Add New Address' }}
              </h2>
              <button
                (click)="closeForm()"
                class="text-gray-400 hover:text-gray-600"
              >
                <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            <app-address-form
              [address]="editingAddress"
              (save)="handleSaveAddress($event)"
              (cancel)="closeForm()"
            ></app-address-form>
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
    .page-header .btn-primary {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.875rem 1.5rem;
      background: white;
      color: #003d82;
      border: none;
      border-radius: 0.5rem;
      font-weight: 600;
      text-decoration: none;
      transition: all 0.2s;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
      cursor: pointer;
    }
    .page-header .btn-primary svg {
      width: 20px;
      height: 20px;
    }
    .page-header .btn-primary:hover {
      transform: translateY(-2px);
      box-shadow: 0 6px 16px rgba(0, 0, 0, 0.2);
    }
    @media (max-width: 768px) {
      .page-header {
        flex-direction: column;
        align-items: flex-start;
        gap: 1.5rem;
      }
      .page-header .btn-primary {
        width: 100%;
        justify-content: center;
      }
    }
  `]
})
export class AddressesListComponent implements OnInit {
  private customersService = inject(CustomersService);

  addresses: SavedAddress[] = [];
  isLoading = false;
  errorMessage = '';
  showAddressForm = false;
  editingAddress: SavedAddress | null = null;

  ngOnInit(): void {
    this.loadAddresses();
  }

  loadAddresses(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.customersService.apiCustomersMeAddressesGet().subscribe({
      next: (addresses) => {
        this.addresses = (addresses as any) || [];
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading addresses:', error);
        this.errorMessage = 'Failed to load addresses. Please try again.';
        this.isLoading = false;
      }
    });
  }

  setAsDefault(addressId: string): void {
    this.customersService.apiCustomersMeAddressesIdSetDefaultPatch(addressId).subscribe({
      next: () => {
        this.loadAddresses();
      },
      error: (error) => {
        console.error('Error setting default address:', error);
        this.errorMessage = 'Failed to set default address. Please try again.';
      }
    });
  }

  editAddress(address: SavedAddress): void {
    this.editingAddress = { ...address };
    this.showAddressForm = true;
  }

  deleteAddress(addressId: string): void {
    if (!confirm('Are you sure you want to delete this address?')) {
      return;
    }

    this.customersService.apiCustomersMeAddressesIdDelete(addressId).subscribe({
      next: () => {
        this.loadAddresses();
      },
      error: (error) => {
        console.error('Error deleting address:', error);
        this.errorMessage = 'Failed to delete address. Please try again.';
      }
    });
  }

  handleSaveAddress(address: CreateSavedAddressDto): void {
    if (this.editingAddress?.id) {
      // Update existing
      this.customersService.apiCustomersMeAddressesIdPut(this.editingAddress.id, address).subscribe({
        next: () => {
          this.closeForm();
          this.loadAddresses();
        },
        error: (error) => {
          console.error('Error updating address:', error);
          this.errorMessage = 'Failed to update address. Please try again.';
        }
      });
    } else {
      // Create new
      this.customersService.apiCustomersMeAddressesPost(address).subscribe({
        next: () => {
          this.closeForm();
          this.loadAddresses();
        },
        error: (error) => {
          console.error('Error creating address:', error);
          this.errorMessage = 'Failed to create address. Please try again.';
        }
      });
    }
  }

  closeForm(): void {
    this.showAddressForm = false;
    this.editingAddress = null;
  }
}
