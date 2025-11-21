'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { ApiService } from '@/services/api.service';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Select } from '@/components/ui/Select';
import { Alert } from '@/components/ui/Alert';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/Card';
import { LicenseClass, VehicleType } from '@/types';

const driverSchema = z.object({
  // Account Info
  email: z.string().email('Please enter a valid email address'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
  confirmPassword: z.string(),

  // Personal Info
  firstName: z.string().min(1, 'First name is required'),
  lastName: z.string().min(1, 'Last name is required'),
  phoneNumber: z.string().regex(/^(\+44|0)[0-9]{10}$/, 'Please enter a valid UK phone number'),
  dateOfBirth: z.string().min(1, 'Date of birth is required'),
  nationalInsuranceNumber: z.string().regex(/^[A-Z]{2}[0-9]{6}[A-Z]$/, 'Please enter a valid NI number'),

  // Address
  street: z.string().min(1, 'Street address is required'),
  city: z.string().min(1, 'City is required'),
  county: z.string().min(1, 'County is required'),
  postcode: z.string().regex(/^[A-Z]{1,2}[0-9]{1,2}[A-Z]?\s?[0-9][A-Z]{2}$/i, 'Please enter a valid UK postcode'),
  country: z.string().default('United Kingdom'),

  // Driving License
  licenseNumber: z.string().min(1, 'License number is required'),
  licenseClass: z.nativeEnum(LicenseClass),
  licenseIssueDate: z.string().min(1, 'Issue date is required'),
  licenseExpiryDate: z.string().min(1, 'Expiry date is required'),

  // Vehicle
  registrationNumber: z.string().min(1, 'Registration number is required'),
  make: z.string().min(1, 'Vehicle make is required'),
  model: z.string().min(1, 'Vehicle model is required'),
  year: z.number().min(1990).max(new Date().getFullYear() + 1),
  vehicleType: z.nativeEnum(VehicleType),
  insurancePolicyNumber: z.string().min(1, 'Insurance policy number is required'),
  insuranceExpiryDate: z.string().min(1, 'Insurance expiry date is required'),
  motExpiryDate: z.string().optional(),

  // Bank Details
  accountHolderName: z.string().min(1, 'Account holder name is required'),
  sortCode: z.string().regex(/^[0-9]{6}$/, 'Sort code must be 6 digits'),
  accountNumber: z.string().regex(/^[0-9]{8}$/, 'Account number must be 8 digits'),

  // Emergency Contact
  emergencyName: z.string().min(1, 'Emergency contact name is required'),
  emergencyRelationship: z.string().min(1, 'Relationship is required'),
  emergencyPhone: z.string().regex(/^(\+44|0)[0-9]{10}$/, 'Please enter a valid UK phone number'),
}).refine((data) => data.password === data.confirmPassword, {
  message: "Passwords don't match",
  path: ['confirmPassword'],
});

type DriverFormData = z.infer<typeof driverSchema>;

export default function DriverRegistrationPage() {
  const router = useRouter();
  const [error, setError] = useState<string>('');
  const [success, setSuccess] = useState<string>('');
  const [isLoading, setIsLoading] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<DriverFormData>({
    resolver: zodResolver(driverSchema),
    defaultValues: {
      country: 'United Kingdom',
    },
  });

  const onSubmit = async (data: DriverFormData) => {
    try {
      setIsLoading(true);
      setError('');

      const registrationData = {
        email: data.email,
        password: data.password,
        confirmPassword: data.confirmPassword,
        firstName: data.firstName,
        lastName: data.lastName,
        phoneNumber: data.phoneNumber,
        dateOfBirth: data.dateOfBirth,
        nationalInsuranceNumber: data.nationalInsuranceNumber,
        address: {
          street: data.street,
          city: data.city,
          county: data.county,
          postcode: data.postcode,
          country: data.country,
        },
        drivingLicense: {
          licenseNumber: data.licenseNumber,
          licenseClass: data.licenseClass,
          issueDate: data.licenseIssueDate,
          expiryDate: data.licenseExpiryDate,
        },
        vehicle: {
          registrationNumber: data.registrationNumber,
          make: data.make,
          model: data.model,
          year: data.year,
          vehicleType: data.vehicleType,
          insurancePolicyNumber: data.insurancePolicyNumber,
          insuranceExpiryDate: data.insuranceExpiryDate,
          motExpiryDate: data.motExpiryDate,
        },
        bankDetails: {
          accountHolderName: data.accountHolderName,
          sortCode: data.sortCode,
          accountNumber: data.accountNumber,
        },
        emergencyContact: {
          name: data.emergencyName,
          relationship: data.emergencyRelationship,
          phoneNumber: data.emergencyPhone,
        },
      };

      await ApiService.registerDriver(registrationData);
      setSuccess('Registration successful! Your application is pending approval. You will receive an email once approved.');

      setTimeout(() => {
        router.push('/login');
      }, 3000);
    } catch (err: any) {
      setError(err.message || 'Registration failed. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-primary-50 to-primary-100 py-12 px-4">
      <div className="max-w-4xl mx-auto">
        <div className="text-center mb-8">
          <Link href="/">
            <h1 className="text-3xl font-bold text-primary-900 mb-2">BeC Marketplace</h1>
          </Link>
          <p className="text-gray-600">Register as a Driver</p>
        </div>

        <Card variant="elevated">
          <CardHeader>
            <CardTitle>Driver Registration</CardTitle>
            <CardDescription>
              Complete all sections below to register as a driver. Your application will be reviewed before approval.
            </CardDescription>
          </CardHeader>

          <CardContent>
            {error && (
              <Alert variant="error" className="mb-6" onClose={() => setError('')}>
                {error}
              </Alert>
            )}

            {success && (
              <Alert variant="success" className="mb-6">
                {success}
              </Alert>
            )}

            <form onSubmit={handleSubmit(onSubmit)} className="space-y-8">
              {/* Account Information */}
              <div>
                <h3 className="text-lg font-semibold mb-4">Account Information</h3>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <Input
                    label="Email Address"
                    type="email"
                    placeholder="you@example.com"
                    error={errors.email?.message}
                    {...register('email')}
                  />
                  <Input
                    label="Phone Number"
                    type="tel"
                    placeholder="07123456789"
                    error={errors.phoneNumber?.message}
                    {...register('phoneNumber')}
                  />
                  <Input
                    label="Password"
                    type="password"
                    placeholder="Min 8 characters"
                    error={errors.password?.message}
                    {...register('password')}
                  />
                  <Input
                    label="Confirm Password"
                    type="password"
                    placeholder="Re-enter password"
                    error={errors.confirmPassword?.message}
                    {...register('confirmPassword')}
                  />
                </div>
              </div>

              {/* Personal Information */}
              <div>
                <h3 className="text-lg font-semibold mb-4">Personal Information</h3>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <Input
                    label="First Name"
                    placeholder="John"
                    error={errors.firstName?.message}
                    {...register('firstName')}
                  />
                  <Input
                    label="Last Name"
                    placeholder="Smith"
                    error={errors.lastName?.message}
                    {...register('lastName')}
                  />
                  <Input
                    label="Date of Birth"
                    type="date"
                    error={errors.dateOfBirth?.message}
                    {...register('dateOfBirth')}
                  />
                  <Input
                    label="National Insurance Number"
                    placeholder="AB123456C"
                    error={errors.nationalInsuranceNumber?.message}
                    {...register('nationalInsuranceNumber')}
                  />
                </div>
              </div>

              {/* Address */}
              <div>
                <h3 className="text-lg font-semibold mb-4">Address</h3>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="md:col-span-2">
                    <Input
                      label="Street Address"
                      placeholder="123 High Street"
                      error={errors.street?.message}
                      {...register('street')}
                    />
                  </div>
                  <Input
                    label="City"
                    placeholder="London"
                    error={errors.city?.message}
                    {...register('city')}
                  />
                  <Input
                    label="County"
                    placeholder="Greater London"
                    error={errors.county?.message}
                    {...register('county')}
                  />
                  <Input
                    label="Postcode"
                    placeholder="SW1A 1AA"
                    error={errors.postcode?.message}
                    {...register('postcode')}
                  />
                  <Input
                    label="Country"
                    value="United Kingdom"
                    disabled
                    {...register('country')}
                  />
                </div>
              </div>

              {/* Driving License */}
              <div>
                <h3 className="text-lg font-semibold mb-4">Driving License</h3>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <Input
                    label="License Number"
                    placeholder="SMITH751234AB9CD"
                    error={errors.licenseNumber?.message}
                    {...register('licenseNumber')}
                  />
                  <Select
                    label="License Class"
                    options={Object.values(LicenseClass).map(c => ({ value: c, label: `Class ${c}` }))}
                    error={errors.licenseClass?.message}
                    {...register('licenseClass')}
                  />
                  <Input
                    label="Issue Date"
                    type="date"
                    error={errors.licenseIssueDate?.message}
                    {...register('licenseIssueDate')}
                  />
                  <Input
                    label="Expiry Date"
                    type="date"
                    error={errors.licenseExpiryDate?.message}
                    {...register('licenseExpiryDate')}
                  />
                </div>
              </div>

              {/* Vehicle Information */}
              <div>
                <h3 className="text-lg font-semibold mb-4">Vehicle Information</h3>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <Input
                    label="Registration Number"
                    placeholder="AB12 CDE"
                    error={errors.registrationNumber?.message}
                    {...register('registrationNumber')}
                  />
                  <Select
                    label="Vehicle Type"
                    options={Object.values(VehicleType).map(v => ({ value: v, label: v }))}
                    error={errors.vehicleType?.message}
                    {...register('vehicleType')}
                  />
                  <Input
                    label="Make"
                    placeholder="Ford"
                    error={errors.make?.message}
                    {...register('make')}
                  />
                  <Input
                    label="Model"
                    placeholder="Transit"
                    error={errors.model?.message}
                    {...register('model')}
                  />
                  <Input
                    label="Year"
                    type="number"
                    placeholder="2020"
                    error={errors.year?.message}
                    {...register('year', { valueAsNumber: true })}
                  />
                  <Input
                    label="Insurance Policy Number"
                    placeholder="POL123456"
                    error={errors.insurancePolicyNumber?.message}
                    {...register('insurancePolicyNumber')}
                  />
                  <Input
                    label="Insurance Expiry Date"
                    type="date"
                    error={errors.insuranceExpiryDate?.message}
                    {...register('insuranceExpiryDate')}
                  />
                  <Input
                    label="MOT Expiry Date (if applicable)"
                    type="date"
                    error={errors.motExpiryDate?.message}
                    {...register('motExpiryDate')}
                  />
                </div>
              </div>

              {/* Bank Details */}
              <div>
                <h3 className="text-lg font-semibold mb-4">Bank Details</h3>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="md:col-span-2">
                    <Input
                      label="Account Holder Name"
                      placeholder="John Smith"
                      error={errors.accountHolderName?.message}
                      {...register('accountHolderName')}
                    />
                  </div>
                  <Input
                    label="Sort Code"
                    placeholder="123456"
                    maxLength={6}
                    error={errors.sortCode?.message}
                    {...register('sortCode')}
                  />
                  <Input
                    label="Account Number"
                    placeholder="12345678"
                    maxLength={8}
                    error={errors.accountNumber?.message}
                    {...register('accountNumber')}
                  />
                </div>
              </div>

              {/* Emergency Contact */}
              <div>
                <h3 className="text-lg font-semibold mb-4">Emergency Contact</h3>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <Input
                    label="Contact Name"
                    placeholder="Jane Smith"
                    error={errors.emergencyName?.message}
                    {...register('emergencyName')}
                  />
                  <Input
                    label="Relationship"
                    placeholder="Spouse"
                    error={errors.emergencyRelationship?.message}
                    {...register('emergencyRelationship')}
                  />
                  <Input
                    label="Phone Number"
                    type="tel"
                    placeholder="07123456789"
                    error={errors.emergencyPhone?.message}
                    {...register('emergencyPhone')}
                  />
                </div>
              </div>

              <div className="flex gap-4">
                <Button type="submit" fullWidth isLoading={isLoading}>
                  Submit Registration
                </Button>
                <Link href="/" className="flex-1">
                  <Button type="button" variant="outline" fullWidth>
                    Cancel
                  </Button>
                </Link>
              </div>
            </form>
          </CardContent>
        </Card>

        <div className="mt-6 text-center text-sm text-gray-600">
          <p>
            Already have an account?{' '}
            <Link href="/login" className="text-primary-600 font-medium hover:underline">
              Login here
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}
