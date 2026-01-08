import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

interface DriverRegistrationRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phone: string;
  licenseNumber: string;
  licenseExpiry: string;
  drivingLicenseType?: string;
  nationalInsuranceNumber?: string;
  rightToWorkExpiry?: string;
  address: {
    addressLine1: string;
    addressLine2?: string;
    city: string;
    county?: string;
    postalCode: string;
    country: string;
  };
  emergencyContact?: {
    name: string;
    relationship: string;
    phone: string;
    email?: string;
  };
  acceptedTerms: boolean;
  acceptedPrivacyPolicy: boolean;
  consentBackgroundCheck: boolean;
}

@Component({
  selector: 'app-register-driver',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './register-driver.component.html',
  styleUrls: ['./register-driver.component.scss']
})
export class RegisterDriverComponent {
  currentStep = 1;
  totalSteps = 4;

  // Forms for each step
  personalInfoForm: FormGroup;
  licenseInfoForm: FormGroup;
  addressForm: FormGroup;
  emergencyContactForm: FormGroup;

  isSubmitting = false;
  errorMessage = '';
  successMessage = '';

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private router: Router
  ) {
    // Step 1: Personal Information
    this.personalInfoForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      lastName: ['', [Validators.required, Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.required, Validators.pattern(/^(\+44|0)[1-9]\d{9,10}$/)]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', Validators.required]
    }, { validators: this.passwordMatchValidator });

    // Step 2: License Information
    this.licenseInfoForm = this.fb.group({
      licenseNumber: ['', [Validators.required, Validators.maxLength(50)]],
      licenseExpiry: ['', Validators.required],
      drivingLicenseType: ['Full UK'],
      nationalInsuranceNumber: ['', Validators.maxLength(20)],
      rightToWorkExpiry: ['']
    });

    // Step 3: Address
    this.addressForm = this.fb.group({
      addressLine1: ['', Validators.required],
      addressLine2: [''],
      city: ['', Validators.required],
      county: [''],
      postalCode: ['', [Validators.required, Validators.pattern(/^[A-Z]{1,2}\d{1,2}[A-Z]?\s?\d[A-Z]{2}$/i)]],
      country: ['United Kingdom']
    });

    // Step 4: Emergency Contact
    this.emergencyContactForm = this.fb.group({
      name: ['', Validators.required],
      relationship: ['', Validators.required],
      phone: ['', [Validators.required, Validators.pattern(/^(\+44|0)[1-9]\d{9,10}$/)]],
      email: ['', Validators.email],
      acceptedTerms: [false, Validators.requiredTrue],
      acceptedPrivacyPolicy: [false, Validators.requiredTrue],
      consentBackgroundCheck: [false, Validators.requiredTrue]
    });
  }

  passwordMatchValidator(form: FormGroup) {
    const password = form.get('password');
    const confirmPassword = form.get('confirmPassword');

    if (password && confirmPassword && password.value !== confirmPassword.value) {
      confirmPassword.setErrors({ passwordMismatch: true });
      return { passwordMismatch: true };
    }
    return null;
  }

  nextStep(): void {
    const currentForm = this.getCurrentForm();
    if (currentForm.invalid) {
      this.markFormAsTouched(currentForm);
      return;
    }

    if (this.currentStep < this.totalSteps) {
      this.currentStep++;
    }
  }

  previousStep(): void {
    if (this.currentStep > 1) {
      this.currentStep--;
    }
  }

  getCurrentForm(): FormGroup {
    switch (this.currentStep) {
      case 1: return this.personalInfoForm;
      case 2: return this.licenseInfoForm;
      case 3: return this.addressForm;
      case 4: return this.emergencyContactForm;
      default: return this.personalInfoForm;
    }
  }

  onSubmit(): void {
    if (!this.isAllFormsValid()) {
      this.errorMessage = 'Please fill in all required fields';
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    const request: DriverRegistrationRequest = {
      ...this.personalInfoForm.value,
      ...this.licenseInfoForm.value,
      address: this.addressForm.value,
      emergencyContact: {
        name: this.emergencyContactForm.value.name,
        relationship: this.emergencyContactForm.value.relationship,
        phone: this.emergencyContactForm.value.phone,
        email: this.emergencyContactForm.value.email
      },
      acceptedTerms: this.emergencyContactForm.value.acceptedTerms,
      acceptedPrivacyPolicy: this.emergencyContactForm.value.acceptedPrivacyPolicy,
      consentBackgroundCheck: this.emergencyContactForm.value.consentBackgroundCheck
    };

    // Remove confirmPassword
    delete (request as any).confirmPassword;

    this.http.post(`${environment.apiBaseUrl}/api/registration/driver`, request)
      .subscribe({
        next: (response: any) => {
          this.successMessage = response.message;
          this.isSubmitting = false;

          // Redirect to login after 3 seconds
          setTimeout(() => {
            this.router.navigate(['/login'], {
              queryParams: { registered: 'true', email: request.email }
            });
          }, 3000);
        },
        error: (error) => {
          console.error('Registration error:', error);
          this.errorMessage = error.error?.message || 'Registration failed. Please try again.';
          if (error.error?.errors) {
            this.errorMessage += ' ' + error.error.errors.join(', ');
          }
          this.isSubmitting = false;
        }
      });
  }

  private isAllFormsValid(): boolean {
    return this.personalInfoForm.valid &&
           this.licenseInfoForm.valid &&
           this.addressForm.valid &&
           this.emergencyContactForm.valid;
  }

  private markFormAsTouched(form: FormGroup): void {
    Object.keys(form.controls).forEach(key => {
      const control = form.get(key);
      control?.markAsTouched();
    });
  }

  getProgress(): number {
    return (this.currentStep / this.totalSteps) * 100;
  }
}
