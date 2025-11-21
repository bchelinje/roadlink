'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import { useAuth } from '@/contexts/AuthContext';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import { ApiService } from '@/services/api.service';
import { Button } from '@/components/ui/Button';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/Card';
import { Alert } from '@/components/ui/Alert';
import { UserRole, Customer, Job, JobStatus, Payment } from '@/types';
import { LogOut, Package, Clock, CheckCircle, XCircle, Calendar, DollarSign, Bell, Star, MapPin, Truck } from 'lucide-react';
import { format } from 'date-fns';

function CustomerDashboardContent() {
  const { user, logout } = useAuth();
  const customer = user as Customer;
  const [jobs, setJobs] = useState<Job[]>([]);
  const [payments, setPayments] = useState<Payment[]>([]);
  const [selectedJob, setSelectedJob] = useState<Job | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [activeTab, setActiveTab] = useState<'all' | 'active' | 'completed'>('all');
  const [notificationCount, setNotificationCount] = useState(0);

  useEffect(() => {
    loadDashboardData();
  }, []);

  const loadDashboardData = async () => {
    try {
      setIsLoading(true);
      const [jobsData, paymentsData, notifCount] = await Promise.all([
        ApiService.getCustomerJobs(),
        ApiService.getMyPayments(),
        ApiService.getUnreadCount(),
      ]);
      setJobs(jobsData);
      setPayments(paymentsData);
      setNotificationCount(notifCount.count);
    } catch (err: any) {
      setError(err.message || 'Failed to load dashboard data');
    } finally {
      setIsLoading(false);
    }
  };

  const handleCancelJob = async (jobId: string) => {
    if (!confirm('Are you sure you want to cancel this job?')) return;

    try {
      await ApiService.cancelJob(jobId, {
        reason: 'Cancelled by customer',
      });
      setSelectedJob(null);
      loadDashboardData();
    } catch (err: any) {
      setError(err.message || 'Failed to cancel job');
    }
  };

  const getStatusColor = (status: JobStatus) => {
    switch (status) {
      case JobStatus.Pending:
        return 'text-yellow-600 bg-yellow-50';
      case JobStatus.Assigned:
        return 'text-blue-600 bg-blue-50';
      case JobStatus.InProgress:
        return 'text-purple-600 bg-purple-50';
      case JobStatus.Completed:
        return 'text-green-600 bg-green-50';
      case JobStatus.Cancelled:
        return 'text-red-600 bg-red-50';
      default:
        return 'text-gray-600 bg-gray-50';
    }
  };

  const activeJobs = jobs.filter(j => j.status === JobStatus.Assigned || j.status === JobStatus.InProgress || j.status === JobStatus.Pending);
  const completedJobs = jobs.filter(j => j.status === JobStatus.Completed);
  const totalSpent = payments.reduce((sum, p) => sum + p.amount, 0);

  const filteredJobs = activeTab === 'all' ? jobs :
    activeTab === 'active' ? activeJobs :
    completedJobs;

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white shadow sticky top-0 z-10">
        <div className="container py-4">
          <div className="flex justify-between items-center">
            <div>
              <h1 className="text-2xl font-bold text-gray-900">Customer Dashboard</h1>
              <p className="text-sm text-gray-600">
                Welcome, {customer?.firstName} {customer?.lastName}
                {customer?.companyName && ` (${customer.companyName})`}
              </p>
            </div>
            <div className="flex items-center space-x-3">
              <Link href="/customer/notifications">
                <Button variant="outline" className="relative">
                  <Bell className="w-4 h-4" />
                  {notificationCount > 0 && (
                    <span className="absolute -top-1 -right-1 bg-red-600 text-white text-xs rounded-full w-5 h-5 flex items-center justify-center">
                      {notificationCount}
                    </span>
                  )}
                </Button>
              </Link>
              <Button variant="outline" onClick={logout}>
                <LogOut className="w-4 h-4 mr-2" />
                Logout
              </Button>
            </div>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="container py-8">
        {error && (
          <Alert variant="error" className="mb-6" onClose={() => setError('')}>
            {error}
          </Alert>
        )}

        {/* Stats */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-8">
          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-3xl font-bold text-primary-600">{activeJobs.length}</p>
                  <p className="text-sm text-gray-600 mt-1">Active Jobs</p>
                </div>
                <Clock className="w-8 h-8 text-primary-600 opacity-50" />
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-3xl font-bold text-green-600">{completedJobs.length}</p>
                  <p className="text-sm text-gray-600 mt-1">Completed Jobs</p>
                </div>
                <CheckCircle className="w-8 h-8 text-green-600 opacity-50" />
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-3xl font-bold text-blue-600">£{totalSpent.toFixed(2)}</p>
                  <p className="text-sm text-gray-600 mt-1">Total Spent</p>
                </div>
                <DollarSign className="w-8 h-8 text-blue-600 opacity-50" />
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-3xl font-bold text-purple-600">{jobs.length}</p>
                  <p className="text-sm text-gray-600 mt-1">Total Jobs</p>
                </div>
                <Package className="w-8 h-8 text-purple-600 opacity-50" />
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Quick Actions */}
        <div className="mb-8">
          <Card>
            <CardContent className="p-6">
              <div className="flex flex-wrap gap-4">
                <Link href="/book">
                  <Button size="lg">
                    <Package className="w-5 h-5 mr-2" />
                    Book New Job
                  </Button>
                </Link>
                <Link href="/customer/templates">
                  <Button variant="outline" size="lg">
                    <Calendar className="w-5 h-5 mr-2" />
                    Job Templates
                  </Button>
                </Link>
                <Link href="/customer/payments">
                  <Button variant="outline" size="lg">
                    <DollarSign className="w-5 h-5 mr-2" />
                    Payment History
                  </Button>
                </Link>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Jobs List */}
        <Card>
          <CardHeader>
            <div className="flex justify-between items-center">
              <CardTitle>My Jobs</CardTitle>
              <div className="flex space-x-2">
                <Button
                  size="sm"
                  variant={activeTab === 'all' ? 'primary' : 'outline'}
                  onClick={() => setActiveTab('all')}
                >
                  All
                </Button>
                <Button
                  size="sm"
                  variant={activeTab === 'active' ? 'primary' : 'outline'}
                  onClick={() => setActiveTab('active')}
                >
                  Active
                </Button>
                <Button
                  size="sm"
                  variant={activeTab === 'completed' ? 'primary' : 'outline'}
                  onClick={() => setActiveTab('completed')}
                >
                  Completed
                </Button>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <div className="text-center py-12">
                <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600 mx-auto"></div>
                <p className="mt-4 text-gray-600">Loading jobs...</p>
              </div>
            ) : filteredJobs.length === 0 ? (
              <div className="text-center py-12">
                <Package className="w-16 h-16 text-gray-300 mx-auto mb-4" />
                <p className="text-gray-600">No jobs found</p>
                <Link href="/book">
                  <Button className="mt-4">
                    Book Your First Job
                  </Button>
                </Link>
              </div>
            ) : (
              <div className="space-y-4">
                {filteredJobs.map((job) => (
                  <div
                    key={job.id}
                    className="border border-gray-200 rounded-lg p-4 hover:bg-gray-50 transition-colors cursor-pointer"
                    onClick={() => setSelectedJob(job)}
                  >
                    <div className="flex items-start justify-between">
                      <div className="flex-1">
                        <div className="flex items-center space-x-3 mb-2">
                          <span className="font-semibold text-lg">Job #{job.jobNumber}</span>
                          <span className={`px-3 py-1 rounded-full text-xs font-medium ${getStatusColor(job.status)}`}>
                            {job.status}
                          </span>
                          <span className="text-sm text-gray-600">{job.jobType}</span>
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm text-gray-600">
                          <div className="flex items-start">
                            <MapPin className="w-4 h-4 mr-2 text-green-600 mt-0.5" />
                            <div>
                              <p className="font-medium text-gray-700">Pickup</p>
                              <p>{job.pickupLocation.address}, {job.pickupLocation.city}</p>
                            </div>
                          </div>

                          <div className="flex items-start">
                            <MapPin className="w-4 h-4 mr-2 text-red-600 mt-0.5" />
                            <div>
                              <p className="font-medium text-gray-700">Delivery</p>
                              <p>{job.deliveryLocation.address}, {job.deliveryLocation.city}</p>
                            </div>
                          </div>

                          <div className="flex items-center">
                            <Calendar className="w-4 h-4 mr-2" />
                            <span>Scheduled: {format(new Date(job.scheduledDate), 'PPP')}</span>
                          </div>

                          <div className="flex items-center">
                            <DollarSign className="w-4 h-4 mr-2" />
                            <span className="font-semibold">£{job.totalPrice.toFixed(2)}</span>
                          </div>

                          {job.driverName && (
                            <div className="flex items-center">
                              <Truck className="w-4 h-4 mr-2" />
                              <span>Driver: {job.driverName}</span>
                            </div>
                          )}

                          <div className="flex items-center">
                            <Package className="w-4 h-4 mr-2" />
                            <span>{job.items.length} item(s)</span>
                          </div>
                        </div>
                      </div>

                      <div className="ml-4">
                        <Button size="sm" variant="outline">
                          View Details
                        </Button>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      </main>

      {/* Job Details Modal */}
      {selectedJob && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-lg max-w-3xl w-full max-h-[90vh] overflow-y-auto">
            <div className="p-6">
              <div className="flex justify-between items-start mb-6">
                <div>
                  <h2 className="text-2xl font-bold">Job #{selectedJob.jobNumber}</h2>
                  <span className={`inline-block px-3 py-1 rounded-full text-xs font-medium ${getStatusColor(selectedJob.status)} mt-2`}>
                    {selectedJob.status}
                  </span>
                </div>
                <button
                  onClick={() => setSelectedJob(null)}
                  className="text-gray-500 hover:text-gray-700"
                >
                  <XCircle className="w-6 h-6" />
                </button>
              </div>

              <div className="space-y-6">
                {/* Job Info */}
                <div>
                  <h3 className="text-lg font-semibold mb-3">Job Information</h3>
                  <div className="grid grid-cols-2 gap-4 text-sm">
                    <div>
                      <p className="text-gray-600">Type</p>
                      <p className="font-medium">{selectedJob.jobType}</p>
                    </div>
                    <div>
                      <p className="text-gray-600">Vehicle</p>
                      <p className="font-medium">{selectedJob.vehicleType}</p>
                    </div>
                    <div>
                      <p className="text-gray-600">Priority</p>
                      <p className="font-medium">{selectedJob.priority}</p>
                    </div>
                    <div>
                      <p className="text-gray-600">Distance</p>
                      <p className="font-medium">{selectedJob.distanceInMiles} miles</p>
                    </div>
                    <div>
                      <p className="text-gray-600">Scheduled Date</p>
                      <p className="font-medium">{format(new Date(selectedJob.scheduledDate), 'PPP')}</p>
                    </div>
                    {selectedJob.scheduledTime && (
                      <div>
                        <p className="text-gray-600">Time</p>
                        <p className="font-medium">{selectedJob.scheduledTime}</p>
                      </div>
                    )}
                  </div>
                </div>

                {/* Locations */}
                <div>
                  <h3 className="text-lg font-semibold mb-3">Locations</h3>
                  <div className="space-y-4">
                    <div className="border-l-4 border-green-500 pl-4">
                      <p className="font-semibold text-sm text-green-700 mb-1">Pickup</p>
                      <p>{selectedJob.pickupLocation.address}</p>
                      <p>{selectedJob.pickupLocation.city}, {selectedJob.pickupLocation.postcode}</p>
                      {selectedJob.pickupLocation.instructions && (
                        <p className="text-sm text-gray-600 mt-1">Note: {selectedJob.pickupLocation.instructions}</p>
                      )}
                    </div>
                    <div className="border-l-4 border-red-500 pl-4">
                      <p className="font-semibold text-sm text-red-700 mb-1">Delivery</p>
                      <p>{selectedJob.deliveryLocation.address}</p>
                      <p>{selectedJob.deliveryLocation.city}, {selectedJob.deliveryLocation.postcode}</p>
                      {selectedJob.deliveryLocation.instructions && (
                        <p className="text-sm text-gray-600 mt-1">Note: {selectedJob.deliveryLocation.instructions}</p>
                      )}
                    </div>
                  </div>
                </div>

                {/* Items */}
                <div>
                  <h3 className="text-lg font-semibold mb-3">Items</h3>
                  <div className="space-y-2">
                    {selectedJob.items.map((item, index) => (
                      <div key={index} className="bg-gray-50 p-3 rounded">
                        <p className="font-medium">{item.description}</p>
                        <p className="text-sm text-gray-600">
                          Quantity: {item.quantity}
                          {item.weight && ` • Weight: ${item.weight}kg`}
                        </p>
                      </div>
                    ))}
                  </div>
                </div>

                {/* Driver Info */}
                {selectedJob.driverName && (
                  <div>
                    <h3 className="text-lg font-semibold mb-3">Driver</h3>
                    <div className="bg-gray-50 p-4 rounded">
                      <p className="font-medium">{selectedJob.driverName}</p>
                      {selectedJob.driverPhone && (
                        <p className="text-sm text-gray-600">{selectedJob.driverPhone}</p>
                      )}
                    </div>
                  </div>
                )}

                {/* Pricing */}
                <div>
                  <h3 className="text-lg font-semibold mb-3">Pricing</h3>
                  <div className="bg-gray-50 p-4 rounded space-y-2 text-sm">
                    <div className="flex justify-between">
                      <span>Subtotal</span>
                      <span>£{(selectedJob.totalPrice - selectedJob.platformFee).toFixed(2)}</span>
                    </div>
                    <div className="flex justify-between">
                      <span>Platform Fee</span>
                      <span>£{selectedJob.platformFee.toFixed(2)}</span>
                    </div>
                    <div className="flex justify-between font-bold text-base pt-2 border-t">
                      <span>Total</span>
                      <span>£{selectedJob.totalPrice.toFixed(2)}</span>
                    </div>
                  </div>
                </div>

                {/* Actions */}
                <div className="flex gap-4 pt-4 border-t">
                  {selectedJob.status === JobStatus.Pending && (
                    <Button
                      variant="danger"
                      onClick={() => handleCancelJob(selectedJob.id)}
                    >
                      Cancel Job
                    </Button>
                  )}
                  {selectedJob.status === JobStatus.Completed && (
                    <Link href={`/customer/review/${selectedJob.id}`}>
                      <Button variant="success">
                        <Star className="w-4 h-4 mr-2" />
                        Leave Review
                      </Button>
                    </Link>
                  )}
                  <Button
                    variant="outline"
                    onClick={() => setSelectedJob(null)}
                  >
                    Close
                  </Button>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default function CustomerDashboardPage() {
  return (
    <ProtectedRoute allowedRoles={[UserRole.Customer]}>
      <CustomerDashboardContent />
    </ProtectedRoute>
  );
}
