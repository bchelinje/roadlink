import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

interface CustomerRegistrationRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phone: string;
  customerType: string;
  companyName?: string;
  acceptedTerms: boolean;
  acceptedPrivacyPolicy: boolean;
}

interface RegistrationResponse {
  userId: string;
  email: string;
  customerId?: string;
  approvalStatus: string;
  message: string;
  emailVerificationRequired: boolean;
}

@Component({
  selector: 'app-register-customer',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './register-customer.component.html',
  styleUrls: ['./register-customer.component.scss']
})
export class RegisterCustomerComponent {
  registrationForm: FormGroup;
  isSubmitting = false;
  errorMessage = '';
  successMessage = '';

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private router: Router
  ) {
    this.registrationForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      lastName: ['', [Validators.required, Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.required, Validators.pattern(/^(\+44|0)[1-9]\d{9,10}$/)]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', Validators.required],
      customerType: ['individual', Validators.required],
      companyName: [''],
      acceptedTerms: [false, Validators.requiredTrue],
      acceptedPrivacyPolicy: [false, Validators.requiredTrue]
    }, { validators: this.passwordMatchValidator });

    // Watch customer type
    this.registrationForm.get('customerType')?.valueChanges.subscribe(type => {
      const companyNameControl = this.registrationForm.get('companyName');
      if (type === 'business') {
        companyNameControl?.setValidators([Validators.required]);
      } else {
        companyNameControl?.clearValidators();
      }
      companyNameControl?.updateValueAndValidity();
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

  onSubmit(): void {
    if (this.registrationForm.invalid) {
      this.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    const formValue = this.registrationForm.value;
    const request: CustomerRegistrationRequest = {
      email: formValue.email,
      password: formValue.password,
      firstName: formValue.firstName,
      lastName: formValue.lastName,
      phone: formValue.phone,
      customerType: formValue.customerType,
      companyName: formValue.companyName || undefined,
      acceptedTerms: formValue.acceptedTerms,
      acceptedPrivacyPolicy: formValue.acceptedPrivacyPolicy
    };

    this.http.post<RegistrationResponse>(`${environment.apiBaseUrl}/api/registration/customer`, request)
      .subscribe({
        next: (response) => {
          this.successMessage = response.message;
          this.isSubmitting = false;

          // Redirect to login after 3 seconds
          setTimeout(() => {
            this.router.navigate(['/login'], {
              queryParams: { registered: 'true', email: response.email }
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

  private markAllAsTouched(): void {
    Object.keys(this.registrationForm.controls).forEach(key => {
      const control = this.registrationForm.get(key);
      control?.markAsTouched();
    });
  }

  get f() {
    return this.registrationForm.controls;
  }
}
