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
import { JobType, VehicleType, JobPriority, Location, JobItem } from '@/types';
import { Truck, Package, Clock, MapPin, Calendar, DollarSign } from 'lucide-react';

const bookJobSchema = z.object({
  // Job Details
  jobType: z.nativeEnum(JobType),
  vehicleType: z.nativeEnum(VehicleType),
  priority: z.nativeEnum(JobPriority).default(JobPriority.Normal),
  scheduledDate: z.string().min(1, 'Date is required'),
  scheduledTime: z.string().optional(),

  // Customer Details
  customerPhone: z.string().regex(/^(\+44|0)[0-9]{10}$/, 'Please enter a valid UK phone number'),
  customerEmail: z.string().email('Please enter a valid email'),

  // Pickup Location
  pickupAddress: z.string().min(1, 'Pickup address is required'),
  pickupCity: z.string().min(1, 'City is required'),
  pickupPostcode: z.string().regex(/^[A-Z]{1,2}[0-9]{1,2}[A-Z]?\s?[0-9][A-Z]{2}$/i, 'Valid UK postcode required'),
  pickupContactName: z.string().optional(),
  pickupContactPhone: z.string().optional(),
  pickupInstructions: z.string().optional(),

  // Delivery Location
  deliveryAddress: z.string().min(1, 'Delivery address is required'),
  deliveryCity: z.string().min(1, 'City is required'),
  deliveryPostcode: z.string().regex(/^[A-Z]{1,2}[0-9]{1,2}[A-Z]?\s?[0-9][A-Z]{2}$/i, 'Valid UK postcode required'),
  deliveryContactName: z.string().optional(),
  deliveryContactPhone: z.string().optional(),
  deliveryInstructions: z.string().optional(),

  // Items
  itemDescription: z.string().min(1, 'Item description is required'),
  itemQuantity: z.number().min(1, 'Quantity must be at least 1'),
  itemWeight: z.number().optional(),

  // Additional
  specialInstructions: z.string().optional(),
  distanceInMiles: z.number().min(0.1, 'Distance must be at least 0.1 miles'),
});

type BookJobFormData = z.infer<typeof bookJobSchema>;

export default function BookJobPage() {
  const router = useRouter();
  const [error, setError] = useState<string>('');
  const [success, setSuccess] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [estimatedPrice, setEstimatedPrice] = useState<number | null>(null);
  const [jobNumber, setJobNumber] = useState<string>('');

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<BookJobFormData>({
    resolver: zodResolver(bookJobSchema),
    defaultValues: {
      priority: JobPriority.Normal,
      itemQuantity: 1,
    },
  });

  const watchedDistance = watch('distanceInMiles');
  const watchedVehicleType = watch('vehicleType');

  // Auto-calculate price estimate when distance/vehicle changes
  useState(() => {
    if (watchedDistance && watchedVehicleType) {
      ApiService.estimatePrice(watchedDistance, watchedVehicleType)
        .then((result) => setEstimatedPrice(result.estimatedPrice))
        .catch(() => setEstimatedPrice(null));
    }
  });

  const onSubmit = async (data: BookJobFormData) => {
    try {
      setIsLoading(true);
      setError('');

      const pickupLocation: Location = {
        address: data.pickupAddress,
        city: data.pickupCity,
        postcode: data.pickupPostcode,
        contactName: data.pickupContactName,
        contactPhone: data.pickupContactPhone,
        instructions: data.pickupInstructions,
      };

      const deliveryLocation: Location = {
        address: data.deliveryAddress,
        city: data.deliveryCity,
        postcode: data.deliveryPostcode,
        contactName: data.deliveryContactName,
        contactPhone: data.deliveryContactPhone,
        instructions: data.deliveryInstructions,
      };

      const items: JobItem[] = [{
        description: data.itemDescription,
        quantity: data.itemQuantity,
        weight: data.itemWeight,
      }];

      const response = await ApiService.bookJob({
        jobType: data.jobType,
        vehicleType: data.vehicleType,
        priority: data.priority,
        scheduledDate: data.scheduledDate,
        scheduledTime: data.scheduledTime,
        customerPhone: data.customerPhone,
        customerEmail: data.customerEmail,
        pickupLocation,
        deliveryLocation,
        distanceInMiles: data.distanceInMiles,
        items,
        specialInstructions: data.specialInstructions,
      });

      setJobNumber(response.jobNumber);
      setSuccess(true);

      // TODO: Redirect to Stripe payment page with clientSecret
      // For now, show success message
    } catch (err: any) {
      setError(err.message || 'Failed to book job. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  if (success) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-primary-50 to-primary-100 flex items-center justify-center p-4">
        <Card variant="elevated" className="max-w-2xl w-full">
          <CardHeader>
            <CardTitle className="text-2xl text-center text-green-600">
              Job Booked Successfully!
            </CardTitle>
            <CardDescription className="text-center">
              Your job has been created and assigned number: <strong>{jobNumber}</strong>
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <Alert variant="success">
              <div>
                <h3 className="font-semibold">What's Next?</h3>
                <ul className="mt-2 space-y-1 text-sm">
                  <li>• We'll match you with the best available driver</li>
                  <li>• You'll receive an email confirmation shortly</li>
                  <li>• The driver will contact you before pickup</li>
                  <li>• Payment will be processed after job completion</li>
                </ul>
              </div>
            </Alert>

            <div className="flex gap-4">
              <Link href="/" className="flex-1">
                <Button fullWidth variant="outline">
                  Book Another Job
                </Button>
              </Link>
              <Link href="/register/customer" className="flex-1">
                <Button fullWidth>
                  Create Account to Track Jobs
                </Button>
              </Link>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-primary-50 to-primary-100 py-12 px-4">
      <div className="max-w-5xl mx-auto">
        {/* Header */}
        <div className="text-center mb-8">
          <Link href="/">
            <h1 className="text-3xl font-bold text-primary-900 mb-2">BeC Marketplace</h1>
          </Link>
          <p className="text-gray-600">Book your delivery or moving job in minutes</p>
        </div>

        <div className="grid lg:grid-cols-3 gap-8">
          {/* Booking Form */}
          <div className="lg:col-span-2">
            <Card variant="elevated">
              <CardHeader>
                <CardTitle>Book a Job</CardTitle>
                <CardDescription>
                  No account required - book instantly and pay after completion
                </CardDescription>
              </CardHeader>

              <CardContent>
                {error && (
                  <Alert variant="error" className="mb-6" onClose={() => setError('')}>
                    {error}
                  </Alert>
                )}

                <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
                  {/* Job Details */}
                  <div>
                    <h3 className="text-lg font-semibold mb-4 flex items-center">
                      <Package className="w-5 h-5 mr-2" />
                      Job Details
                    </h3>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                      <Select
                        label="Job Type"
                        options={Object.values(JobType).map(t => ({ value: t, label: t }))}
                        error={errors.jobType?.message}
                        {...register('jobType')}
                      />
                      <Select
                        label="Vehicle Type"
                        options={Object.values(VehicleType).map(v => ({ value: v, label: v }))}
                        error={errors.vehicleType?.message}
                        {...register('vehicleType')}
                      />
                      <Select
                        label="Priority"
                        options={Object.values(JobPriority).map(p => ({ value: p, label: p }))}
                        error={errors.priority?.message}
                        {...register('priority')}
                      />
                      <Input
                        label="Distance (miles)"
                        type="number"
                        step="0.1"
                        placeholder="10.5"
                        helperText="Approximate distance"
                        error={errors.distanceInMiles?.message}
                        {...register('distanceInMiles', { valueAsNumber: true })}
                      />
                    </div>
                  </div>

                  {/* Schedule */}
                  <div>
                    <h3 className="text-lg font-semibold mb-4 flex items-center">
                      <Calendar className="w-5 h-5 mr-2" />
                      Schedule
                    </h3>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                      <Input
                        label="Date"
                        type="date"
                        error={errors.scheduledDate?.message}
                        {...register('scheduledDate')}
                      />
                      <Input
                        label="Time (Optional)"
                        type="time"
                        error={errors.scheduledTime?.message}
                        {...register('scheduledTime')}
                      />
                    </div>
                  </div>

                  {/* Customer Contact */}
                  <div>
                    <h3 className="text-lg font-semibold mb-4">Your Contact Details</h3>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                      <Input
                        label="Phone Number"
                        type="tel"
                        placeholder="07123456789"
                        error={errors.customerPhone?.message}
                        {...register('customerPhone')}
                      />
                      <Input
                        label="Email Address"
                        type="email"
                        placeholder="you@example.com"
                        error={errors.customerEmail?.message}
                        {...register('customerEmail')}
                      />
                    </div>
                  </div>

                  {/* Pickup Location */}
                  <div>
                    <h3 className="text-lg font-semibold mb-4 flex items-center">
                      <MapPin className="w-5 h-5 mr-2 text-green-600" />
                      Pickup Location
                    </h3>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                      <div className="md:col-span-2">
                        <Input
                          label="Address"
                          placeholder="123 High Street"
                          error={errors.pickupAddress?.message}
                          {...register('pickupAddress')}
                        />
                      </div>
                      <Input
                        label="City"
                        placeholder="London"
                        error={errors.pickupCity?.message}
                        {...register('pickupCity')}
                      />
                      <Input
                        label="Postcode"
                        placeholder="SW1A 1AA"
                        error={errors.pickupPostcode?.message}
                        {...register('pickupPostcode')}
                      />
                      <Input
                        label="Contact Name (Optional)"
                        placeholder="John Smith"
                        error={errors.pickupContactName?.message}
                        {...register('pickupContactName')}
                      />
                      <Input
                        label="Contact Phone (Optional)"
                        type="tel"
                        placeholder="07123456789"
                        error={errors.pickupContactPhone?.message}
                        {...register('pickupContactPhone')}
                      />
                      <div className="md:col-span-2">
                        <Input
                          label="Special Instructions (Optional)"
                          placeholder="Ring doorbell, entrance at back"
                          error={errors.pickupInstructions?.message}
                          {...register('pickupInstructions')}
                        />
                      </div>
                    </div>
                  </div>

                  {/* Delivery Location */}
                  <div>
                    <h3 className="text-lg font-semibold mb-4 flex items-center">
                      <MapPin className="w-5 h-5 mr-2 text-red-600" />
                      Delivery Location
                    </h3>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                      <div className="md:col-span-2">
                        <Input
                          label="Address"
                          placeholder="456 Park Avenue"
                          error={errors.deliveryAddress?.message}
                          {...register('deliveryAddress')}
                        />
                      </div>
                      <Input
                        label="City"
                        placeholder="Manchester"
                        error={errors.deliveryCity?.message}
                        {...register('deliveryCity')}
                      />
                      <Input
                        label="Postcode"
                        placeholder="M1 1AE"
                        error={errors.deliveryPostcode?.message}
                        {...register('deliveryPostcode')}
                      />
                      <Input
                        label="Contact Name (Optional)"
                        placeholder="Jane Doe"
                        error={errors.deliveryContactName?.message}
                        {...register('deliveryContactName')}
                      />
                      <Input
                        label="Contact Phone (Optional)"
                        type="tel"
                        placeholder="07987654321"
                        error={errors.deliveryContactPhone?.message}
                        {...register('deliveryContactPhone')}
                      />
                      <div className="md:col-span-2">
                        <Input
                          label="Special Instructions (Optional)"
                          placeholder="Leave with neighbor if not home"
                          error={errors.deliveryInstructions?.message}
                          {...register('deliveryInstructions')}
                        />
                      </div>
                    </div>
                  </div>

                  {/* Items */}
                  <div>
                    <h3 className="text-lg font-semibold mb-4 flex items-center">
                      <Package className="w-5 h-5 mr-2" />
                      Items to Transport
                    </h3>
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                      <div className="md:col-span-3">
                        <Input
                          label="Description"
                          placeholder="Furniture, boxes, etc."
                          error={errors.itemDescription?.message}
                          {...register('itemDescription')}
                        />
                      </div>
                      <Input
                        label="Quantity"
                        type="number"
                        min="1"
                        error={errors.itemQuantity?.message}
                        {...register('itemQuantity', { valueAsNumber: true })}
                      />
                      <Input
                        label="Total Weight (kg) - Optional"
                        type="number"
                        step="0.1"
                        placeholder="25.5"
                        error={errors.itemWeight?.message}
                        {...register('itemWeight', { valueAsNumber: true })}
                      />
                      <div className="md:col-span-3">
                        <Input
                          label="Additional Notes (Optional)"
                          placeholder="Fragile items, heavy lifting required, etc."
                          error={errors.specialInstructions?.message}
                          {...register('specialInstructions')}
                        />
                      </div>
                    </div>
                  </div>

                  <Button type="submit" fullWidth size="lg" isLoading={isLoading}>
                    Book Job Now
                  </Button>
                </form>
              </CardContent>
            </Card>
          </div>

          {/* Sidebar */}
          <div className="lg:col-span-1 space-y-6">
            {/* Price Estimate */}
            {estimatedPrice && (
              <Card variant="bordered">
                <CardHeader>
                  <CardTitle className="flex items-center">
                    <DollarSign className="w-5 h-5 mr-2" />
                    Estimated Price
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="text-center">
                    <p className="text-4xl font-bold text-primary-600">
                      £{estimatedPrice.toFixed(2)}
                    </p>
                    <p className="text-sm text-gray-600 mt-2">
                      Final price calculated at booking
                    </p>
                  </div>
                </CardContent>
              </Card>
            )}

            {/* How It Works */}
            <Card variant="bordered">
              <CardHeader>
                <CardTitle className="flex items-center">
                  <Clock className="w-5 h-5 mr-2" />
                  How It Works
                </CardTitle>
              </CardHeader>
              <CardContent>
                <ol className="space-y-3 text-sm">
                  <li className="flex items-start">
                    <span className="bg-primary-600 text-white rounded-full w-6 h-6 flex items-center justify-center text-xs mr-3 flex-shrink-0">1</span>
                    <span>Fill out the booking form with job details</span>
                  </li>
                  <li className="flex items-start">
                    <span className="bg-primary-600 text-white rounded-full w-6 h-6 flex items-center justify-center text-xs mr-3 flex-shrink-0">2</span>
                    <span>We match you with a vetted driver</span>
                  </li>
                  <li className="flex items-start">
                    <span className="bg-primary-600 text-white rounded-full w-6 h-6 flex items-center justify-center text-xs mr-3 flex-shrink-0">3</span>
                    <span>Driver contacts you to confirm details</span>
                  </li>
                  <li className="flex items-start">
                    <span className="bg-primary-600 text-white rounded-full w-6 h-6 flex items-center justify-center text-xs mr-3 flex-shrink-0">4</span>
                    <span>Job completed & payment processed</span>
                  </li>
                  <li className="flex items-start">
                    <span className="bg-primary-600 text-white rounded-full w-6 h-6 flex items-center justify-center text-xs mr-3 flex-shrink-0">5</span>
                    <span>Rate your experience</span>
                  </li>
                </ol>
              </CardContent>
            </Card>

            {/* Why Choose Us */}
            <Card variant="bordered">
              <CardHeader>
                <CardTitle>Why Choose BeC?</CardTitle>
              </CardHeader>
              <CardContent>
                <ul className="space-y-2 text-sm text-gray-700">
                  <li className="flex items-center">
                    <span className="text-green-600 mr-2">✓</span>
                    Verified & insured drivers
                  </li>
                  <li className="flex items-center">
                    <span className="text-green-600 mr-2">✓</span>
                    Transparent pricing
                  </li>
                  <li className="flex items-center">
                    <span className="text-green-600 mr-2">✓</span>
                    Real-time tracking
                  </li>
                  <li className="flex items-center">
                    <span className="text-green-600 mr-2">✓</span>
                    24/7 customer support
                  </li>
                  <li className="flex items-center">
                    <span className="text-green-600 mr-2">✓</span>
                    Instant booking confirmation
                  </li>
                </ul>
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    </div>
  );
}
