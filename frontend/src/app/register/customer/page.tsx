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
import { Alert } from '@/components/ui/Alert';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/Card';

const customerSchema = z.object({
  // Account Info
  email: z.string().email('Please enter a valid email address'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
  confirmPassword: z.string(),

  // Personal Info
  firstName: z.string().min(1, 'First name is required'),
  lastName: z.string().min(1, 'Last name is required'),
  phoneNumber: z.string().regex(/^(\+44|0)[0-9]{10}$/, 'Please enter a valid UK phone number'),
  companyName: z.string().optional(),

  // Address
  street: z.string().min(1, 'Street address is required'),
  city: z.string().min(1, 'City is required'),
  county: z.string().min(1, 'County is required'),
  postcode: z.string().regex(/^[A-Z]{1,2}[0-9]{1,2}[A-Z]?\s?[0-9][A-Z]{2}$/i, 'Please enter a valid UK postcode'),
  country: z.string().default('United Kingdom'),
}).refine((data) => data.password === data.confirmPassword, {
  message: "Passwords don't match",
  path: ['confirmPassword'],
});

type CustomerFormData = z.infer<typeof customerSchema>;

export default function CustomerRegistrationPage() {
  const router = useRouter();
  const [error, setError] = useState<string>('');
  const [success, setSuccess] = useState<string>('');
  const [isLoading, setIsLoading] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<CustomerFormData>({
    resolver: zodResolver(customerSchema),
    defaultValues: {
      country: 'United Kingdom',
    },
  });

  const onSubmit = async (data: CustomerFormData) => {
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
        companyName: data.companyName,
        address: {
          street: data.street,
          city: data.city,
          county: data.county,
          postcode: data.postcode,
          country: data.country,
        },
      };

      await ApiService.registerCustomer(registrationData);
      setSuccess('Registration successful! You can now login to your account.');

      setTimeout(() => {
        router.push('/login');
      }, 2000);
    } catch (err: any) {
      setError(err.message || 'Registration failed. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-primary-50 to-primary-100 py-12 px-4">
      <div className="max-w-2xl mx-auto">
        <div className="text-center mb-8">
          <Link href="/">
            <h1 className="text-3xl font-bold text-primary-900 mb-2">BeC Marketplace</h1>
          </Link>
          <p className="text-gray-600">Register as a Customer</p>
        </div>

        <Card variant="elevated">
          <CardHeader>
            <CardTitle>Customer Registration</CardTitle>
            <CardDescription>
              Create your account to start hiring drivers for your needs
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

            <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
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
                  <div className="md:col-span-2">
                    <Input
                      label="Company Name (Optional)"
                      placeholder="Acme Corp"
                      error={errors.companyName?.message}
                      {...register('companyName')}
                    />
                  </div>
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

              <div className="flex gap-4">
                <Button type="submit" fullWidth isLoading={isLoading}>
                  Create Account
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
