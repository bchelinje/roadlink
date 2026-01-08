import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CustomerJobsService, BookJobDto, BookJobResponseDto } from '@core/api';
import { ToastService } from '@core/services/toast.service';
import { loadStripe, Stripe, StripeElements, StripeCardElement } from '@stripe/stripe-js';
import { DistanceCalculatorComponent, DistanceCalculationResult } from '@shared/components/distance-calculator/distance-calculator.component';
import { MapsService } from '@core/services/maps.service';

@Component({
  selector: 'app-book-job',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DistanceCalculatorComponent],
  templateUrl: './book-job.component.html',
  styleUrls: ['./book-job.component.scss']
})
export class BookJobComponent implements OnInit {
  private fb = inject(FormBuilder);
  private customerJobsService = inject(CustomerJobsService);
  private router = inject(Router);
  private toastService = inject(ToastService);
  private mapsService = inject(MapsService);

  jobForm: FormGroup;
  isSubmitting = false;
  isProcessingPayment = false;
  errorMessage = '';

  // Stripe
  stripe: Stripe | null = null;
  elements: StripeElements | null = null;
  cardElement: StripeCardElement | null = null;

  // Booking response
  bookingResponse: BookJobResponseDto | null = null;
  showPaymentForm = false;

  // Distance calculation
  distanceCalculated = false;
  calculatedDistance: number | null = null;
  estimatedPrice: number | null = null;

  jobTypes = [
    { value: 'local_move', label: 'Local Move' },
    { value: 'long_distance', label: 'Long Distance Move' },
    { value: 'commercial', label: 'Commercial Delivery' },
    { value: 'furniture', label: 'Furniture Transport' },
    { value: 'appliance', label: 'Appliance Delivery' },
    { value: 'other', label: 'Other' }
  ];

  vehicleTypes = [
    { value: 'van', label: 'Van' },
    { value: 'cargo_van', label: 'Cargo Van' },
    { value: 'small_truck', label: 'Small Truck (10-14 ft)' },
    { value: 'large_truck', label: 'Large Truck (16-26 ft)' },
    { value: 'box_truck', label: 'Box Truck' }
  ];

  priorities = [
    { value: 'low', label: 'Flexible - No Rush' },
    { value: 'normal', label: 'Normal Priority' },
    { value: 'high', label: 'High Priority' },
    { value: 'urgent', label: 'Urgent - ASAP' }
  ];

  serviceAddonOptions = [
    { value: 'helpers', label: 'Additional Helpers' },
    { value: 'packing', label: 'Packing Service' },
    { value: 'assembly', label: 'Furniture Assembly' },
    { value: 'storage', label: 'Temporary Storage' }
  ];

  constructor() {
    const today = new Date().toISOString().split('T')[0];

    this.jobForm = this.fb.group({
      // Customer Information
      customerPhone: ['', [Validators.required, Validators.pattern(/^\+?[\d\s\-()]+$/)]],
      customerEmail: ['', [Validators.email]],

      // Job Details
      jobType: ['local_move', Validators.required],
      vehicleTypeRequired: ['van'],
      priority: ['normal'],
      scheduledDate: [today, Validators.required],
      scheduledTime: ['09:00', Validators.required],
      estimatedDuration: [120], // No minimum - let Google Maps decide

      // Locations
      pickupLocation: ['', Validators.required],
      pickupLatitude: [null],
      pickupLongitude: [null],

      deliveryLocation: ['', Validators.required],
      deliveryLatitude: [null],
      deliveryLongitude: [null],

      distance: [null, [Validators.required, Validators.min(0.1)]],

      // Items - will be converted to array
      itemDescription: ['', [Validators.required, Validators.minLength(10)]],

      // Service Add-ons
      serviceAddons: [[]],
      numberOfHelpers: [null, [Validators.min(0)]],

      // Floor and Stairs Information
      pickupFloorNumber: [null, [Validators.min(0)]],
      deliveryFloorNumber: [null, [Validators.min(0)]],
      pickupHasElevator: [false],
      deliveryHasElevator: [false],
      numberOfStairFlights: [null, [Validators.min(0)]],

      // Waiting Time
      waitingTimeMinutes: [null, [Validators.min(0)]],

      // Additional Info
      specialInstructions: ['']
    });
  }

  async ngOnInit(): Promise<void> {
    // Stripe will be initialized when needed
  }

  async onSubmit(): Promise<void> {
    if (this.jobForm.invalid) {
      this.markFormGroupTouched(this.jobForm);

      // Debug: Show which fields are invalid
      const invalidFields = Object.keys(this.jobForm.controls)
        .filter(key => this.jobForm.get(key)?.invalid)
        .map(key => {
          const control = this.jobForm.get(key);
          const errors = control?.errors;
          return `${key}: ${JSON.stringify(errors)}`;
        });

      console.error('Invalid fields:', invalidFields);
      this.errorMessage = `Please fill in all required fields correctly. Invalid fields: ${invalidFields.join(', ')}`;
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    const formValue = this.jobForm.value;

    // Build items array
    const items = [{
      description: formValue.itemDescription,
      quantity: 1
    }];

    const bookJobDto: BookJobDto = {
      customerPhone: formValue.customerPhone,
      customerEmail: formValue.customerEmail || null,
      jobType: formValue.jobType,
      vehicleTypeRequired: formValue.vehicleTypeRequired || null,
      priority: formValue.priority || null,
      scheduledDate: formValue.scheduledDate,
      scheduledTime: formValue.scheduledTime,
      estimatedDuration: formValue.estimatedDuration || null,
      pickupLocation: formValue.pickupLocation,
      pickupLatitude: formValue.pickupLatitude || null,
      pickupLongitude: formValue.pickupLongitude || null,
      deliveryLocation: formValue.deliveryLocation,
      deliveryLatitude: formValue.deliveryLatitude || null,
      deliveryLongitude: formValue.deliveryLongitude || null,
      distance: formValue.distance || null,
      items: items,
      specialInstructions: formValue.specialInstructions || null
    };

    this.customerJobsService.apiCustomersJobsBookPost(bookJobDto).subscribe({
      next: async (response: BookJobResponseDto) => {
        console.log('📊 Booking Response:', response);
        console.log('💰 Amount:', response.amount);
        console.log('🏦 Platform Fee:', response.platformFee);
        console.log('🚚 Driver Earnings:', response.driverEarnings);

        this.bookingResponse = response;
        this.showPaymentForm = true;
        this.isSubmitting = false;

        // Initialize Stripe with the publishable key from response
        if (response.publishableKey && response.clientSecret) {
          await this.initializeStripe(response.publishableKey);
        } else {
          this.errorMessage = 'Payment initialization failed. Please contact support.';
        }
      },
      error: (error) => {
        console.error('❌ Error booking job:', error);
        console.error('Error details:', error.error);

        // Show detailed error message
        if (error.error?.details) {
          this.errorMessage = `${error.error.error}: ${error.error.details}`;
        } else {
          this.errorMessage = error.error?.title || error.error?.error || 'Failed to book job. Please try again.';
        }
        this.isSubmitting = false;
      }
    });
  }

  private async initializeStripe(publishableKey: string): Promise<void> {
    try {
      this.stripe = await loadStripe(publishableKey);

      if (!this.stripe) {
        this.errorMessage = 'Failed to load payment system.';
        return;
      }

      this.elements = this.stripe.elements();

      // Wait for the DOM to be ready
      setTimeout(() => {
        const cardElementContainer = document.getElementById('card-element');
        if (cardElementContainer && this.elements) {
          this.cardElement = this.elements.create('card', {
            style: {
              base: {
                fontSize: '16px',
                color: '#32325d',
                fontFamily: '-apple-system, BlinkMacSystemFont, Segoe UI, Roboto, sans-serif',
                '::placeholder': {
                  color: '#aab7c4'
                }
              },
              invalid: {
                color: '#fa755a',
                iconColor: '#fa755a'
              }
            }
          });
          this.cardElement.mount('#card-element');
        }
      }, 100);
    } catch (error) {
      console.error('Stripe initialization error:', error);
      this.errorMessage = 'Failed to initialize payment system.';
    }
  }

  async confirmPayment(): Promise<void> {
    if (!this.stripe || !this.cardElement || !this.bookingResponse?.clientSecret) {
      this.errorMessage = 'Payment system not ready.';
      return;
    }

    this.isProcessingPayment = true;
    this.errorMessage = '';

    try {
      const { error, paymentIntent } = await this.stripe.confirmCardPayment(
        this.bookingResponse.clientSecret,
        {
          payment_method: {
            card: this.cardElement
          }
        }
      );

      if (error) {
        this.errorMessage = error.message || 'Payment failed. Please try again.';
        this.isProcessingPayment = false;
      } else if (paymentIntent && paymentIntent.status === 'succeeded') {
        this.toastService.success('Payment Successful', 'Your job has been booked successfully!');
        setTimeout(() => {
          this.router.navigate(['/customer/jobs', this.bookingResponse?.jobId]);
        }, 2000);
      }
    } catch (error: any) {
      console.error('Payment error:', error);
      this.errorMessage = 'Payment processing failed. Please try again.';
      this.isProcessingPayment = false;
    }
  }

  cancelPayment(): void {
    this.showPaymentForm = false;
    this.bookingResponse = null;
    this.cardElement?.destroy();
    this.cardElement = null;
    this.elements = null;
    this.stripe = null;
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.jobForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.jobForm.get(fieldName);
    if (field?.hasError('required')) return 'This field is required';
    if (field?.hasError('email')) return 'Please enter a valid email';
    if (field?.hasError('pattern')) return 'Please enter a valid phone number';
    if (field?.hasError('minLength')) return 'Please provide more details';
    if (field?.hasError('min')) {
      if (fieldName === 'distance') return 'Distance must be at least 0.1 miles';
      return 'Value must be 0 or greater';
    }
    return '';
  }

  toggleServiceAddon(addon: string): void {
    const currentAddons = this.jobForm.get('serviceAddons')?.value || [];
    const index = currentAddons.indexOf(addon);

    if (index > -1) {
      currentAddons.splice(index, 1);
    } else {
      currentAddons.push(addon);
    }

    this.jobForm.patchValue({ serviceAddons: currentAddons });
  }

  isServiceAddonSelected(addon: string): boolean {
    const currentAddons = this.jobForm.get('serviceAddons')?.value || [];
    return currentAddons.includes(addon);
  }

  /**
   * Handle distance calculation result from distance calculator component
   */
  onDistanceCalculated(result: DistanceCalculationResult): void {
    console.log('Distance calculated:', result);

    // Update form with calculated values
    this.jobForm.patchValue({
      pickupLocation: result.pickupAddress,
      deliveryLocation: result.deliveryAddress,
      distance: result.distance.distanceInMiles,
      estimatedDuration: result.distance.durationInMinutes
    });

    // Geocode addresses to get coordinates
    this.geocodeAddresses(result.pickupAddress, result.deliveryAddress);

    this.distanceCalculated = true;
    this.calculatedDistance = result.distance.distanceInMiles;

    if (result.pricing?.estimatedPrice) {
      this.estimatedPrice = result.pricing.estimatedPrice;
    }

    this.toastService.success('Distance Calculated', `${result.distance.distanceText} - ${result.distance.durationText}`);
  }

  /**
   * Geocode addresses to populate latitude/longitude
   */
  private geocodeAddresses(pickupAddress: string, deliveryAddress: string): void {
    // Geocode pickup
    this.mapsService.geocodeAddress(pickupAddress).subscribe({
      next: (result) => {
        this.jobForm.patchValue({
          pickupLatitude: result.latitude,
          pickupLongitude: result.longitude
        });
      },
      error: (error) => console.error('Error geocoding pickup:', error)
    });

    // Geocode delivery
    this.mapsService.geocodeAddress(deliveryAddress).subscribe({
      next: (result) => {
        this.jobForm.patchValue({
          deliveryLatitude: result.latitude,
          deliveryLongitude: result.longitude
        });
      },
      error: (error) => console.error('Error geocoding delivery:', error)
    });
  }

  /**
   * Get selected vehicle type for pricing
   */
  getSelectedVehicleType(): string {
    return this.jobForm.get('vehicleTypeRequired')?.value || 'van';
  }

  /**
   * Get currency code from booking response (GBP for UK, USD for US)
   */
  getCurrencyCode(): string {
    return this.bookingResponse?.currency?.toUpperCase() || 'GBP';
  }
}
