'use client';

import { useAuth } from '@/contexts/AuthContext';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import { Button } from '@/components/ui/Button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import {
  UserRole,
  ApprovalStatus,
  Driver,
  Job,
  JobStatus,
  JobBid,
  Payout,
  Payment
} from '@/types';
import {
  LogOut,
  AlertCircle,
  Briefcase,
  DollarSign,
  TrendingUp,
  MapPin,
  Calendar,
  Package,
  Check,
  X,
  Clock,
  Eye
} from 'lucide-react';
import { Alert } from '@/components/ui/Alert';
import { useEffect, useState } from 'react';
import { ApiService } from '@/services/api.service';

function DriverDashboardContent() {
  const { user, logout } = useAuth();
  const driver = user as Driver;

  const [activeTab, setActiveTab] = useState<'available' | 'my-jobs' | 'earnings'>('available');
  const [availableJobs, setAvailableJobs] = useState<Job[]>([]);
  const [myJobs, setMyJobs] = useState<Job[]>([]);
  const [myBids, setMyBids] = useState<JobBid[]>([]);
  const [payouts, setPayouts] = useState<Payout[]>([]);
  const [payments, setPayments] = useState<Payment[]>([]);
  const [selectedJob, setSelectedJob] = useState<Job | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [bidAmount, setBidAmount] = useState<string>('');
  const [bidNotes, setBidNotes] = useState<string>('');

  // Statistics
  const [stats, setStats] = useState({
    totalJobs: 0,
    completedJobs: 0,
    totalEarnings: 0,
    pendingPayouts: 0,
    rating: 0,
  });

  useEffect(() => {
    if (driver?.approvalStatus === ApprovalStatus.Approved) {
      loadDashboardData();
    }
  }, [driver?.approvalStatus]);

  const loadDashboardData = async () => {
    try {
      setIsLoading(true);
      setError(null);

      const [
        availableJobsData,
        myJobsData,
        myBidsData,
        payoutsData,
        paymentsData,
      ] = await Promise.all([
        ApiService.getAvailableJobs().catch(() => []),
        ApiService.getDriverJobs().catch(() => []),
        ApiService.getMyBids().catch(() => []),
        ApiService.getMyPayouts().catch(() => []),
        ApiService.getMyPayments().catch(() => []),
      ]);

      setAvailableJobs(availableJobsData);
      setMyJobs(myJobsData);
      setMyBids(myBidsData);
      setPayouts(payoutsData);
      setPayments(paymentsData);

      // Calculate statistics
      const completedJobs = myJobsData.filter((j: Job) => j.status === JobStatus.Completed);
      const totalEarnings = paymentsData
        .filter((p: Payment) => p.status === 'Completed')
        .reduce((sum: number, p: Payment) => sum + p.amount, 0);
      const pendingPayouts = payoutsData
        .filter((p: Payout) => p.status === 'Pending')
        .reduce((sum: number, p: Payout) => sum + p.amount, 0);

      setStats({
        totalJobs: myJobsData.length,
        completedJobs: completedJobs.length,
        totalEarnings,
        pendingPayouts,
        rating: driver?.rating || 0,
      });
    } catch (err: any) {
      setError(err.message || 'Failed to load dashboard data');
    } finally {
      setIsLoading(false);
    }
  };

  const handleAcceptJob = async (jobId: string) => {
    try {
      await ApiService.acceptJob(jobId);
      await loadDashboardData();
      setSelectedJob(null);
      alert('Job accepted successfully!');
    } catch (err: any) {
      alert(err.message || 'Failed to accept job');
    }
  };

  const handlePlaceBid = async (jobId: string) => {
    if (!bidAmount || parseFloat(bidAmount) <= 0) {
      alert('Please enter a valid bid amount');
      return;
    }

    try {
      await ApiService.createBid({
        jobId,
        amount: parseFloat(bidAmount),
        notes: bidNotes || undefined,
      });
      await loadDashboardData();
      setSelectedJob(null);
      setBidAmount('');
      setBidNotes('');
      alert('Bid placed successfully!');
    } catch (err: any) {
      alert(err.message || 'Failed to place bid');
    }
  };

  const handleCompleteJob = async (jobId: string) => {
    try {
      await ApiService.completeJob(jobId);
      await loadDashboardData();
      alert('Job marked as completed!');
    } catch (err: any) {
      alert(err.message || 'Failed to complete job');
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-GB', {
      style: 'currency',
      currency: 'GBP',
    }).format(amount);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-GB', {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
    });
  };

  const getStatusColor = (status: JobStatus) => {
    const colors = {
      [JobStatus.Pending]: 'bg-yellow-100 text-yellow-800',
      [JobStatus.Assigned]: 'bg-blue-100 text-blue-800',
      [JobStatus.InProgress]: 'bg-purple-100 text-purple-800',
      [JobStatus.Completed]: 'bg-green-100 text-green-800',
      [JobStatus.Cancelled]: 'bg-red-100 text-red-800',
    };
    return colors[status] || 'bg-gray-100 text-gray-800';
  };

  if (driver?.approvalStatus === ApprovalStatus.Pending) {
    return (
      <div className="min-h-screen bg-gray-50">
        <header className="bg-white shadow">
          <div className="container py-4">
            <div className="flex justify-between items-center">
              <div>
                <h1 className="text-2xl font-bold text-gray-900">Driver Dashboard</h1>
                <p className="text-sm text-gray-600">
                  Welcome, {driver?.firstName} {driver?.lastName}
                </p>
              </div>
              <Button variant="outline" onClick={logout}>
                <LogOut className="w-4 h-4 mr-2" />
                Logout
              </Button>
            </div>
          </div>
        </header>

        <main className="container py-8">
          <Alert variant="warning" className="mb-6">
            <AlertCircle className="w-5 h-5" />
            <div className="ml-3">
              <h3 className="font-semibold">Application Pending</h3>
              <p className="mt-1">Your driver application is currently under review. You will be notified once approved.</p>
            </div>
          </Alert>
        </main>
      </div>
    );
  }

  if (driver?.approvalStatus === ApprovalStatus.Rejected) {
    return (
      <div className="min-h-screen bg-gray-50">
        <header className="bg-white shadow">
          <div className="container py-4">
            <div className="flex justify-between items-center">
              <div>
                <h1 className="text-2xl font-bold text-gray-900">Driver Dashboard</h1>
                <p className="text-sm text-gray-600">
                  Welcome, {driver?.firstName} {driver?.lastName}
                </p>
              </div>
              <Button variant="outline" onClick={logout}>
                <LogOut className="w-4 h-4 mr-2" />
                Logout
              </Button>
            </div>
          </div>
        </header>

        <main className="container py-8">
          <Alert variant="error" className="mb-6">
            <div>
              <h3 className="font-semibold">Application Rejected</h3>
              <p className="mt-1">Unfortunately, your driver application was not approved.</p>
              {driver.rejectionReason && (
                <p className="mt-2 text-sm">Reason: {driver.rejectionReason}</p>
              )}
            </div>
          </Alert>
        </main>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white shadow">
        <div className="container py-4">
          <div className="flex justify-between items-center">
            <div>
              <h1 className="text-2xl font-bold text-gray-900">Driver Dashboard</h1>
              <p className="text-sm text-gray-600">
                Welcome, {driver?.firstName} {driver?.lastName}
              </p>
            </div>
            <Button variant="outline" onClick={logout}>
              <LogOut className="w-4 h-4 mr-2" />
              Logout
            </Button>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="container py-8">
        {error && (
          <Alert variant="error" className="mb-6">
            <AlertCircle className="w-5 h-5" />
            <div className="ml-3">
              <p>{error}</p>
            </div>
          </Alert>
        )}

        {/* Statistics Cards */}
        <div className="grid grid-cols-1 md:grid-cols-5 gap-6 mb-8">
          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center">
                <Briefcase className="w-8 h-8 text-primary-600 mr-3" />
                <div>
                  <p className="text-2xl font-bold text-gray-900">{stats.totalJobs}</p>
                  <p className="text-sm text-gray-600">Total Jobs</p>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center">
                <Check className="w-8 h-8 text-green-600 mr-3" />
                <div>
                  <p className="text-2xl font-bold text-gray-900">{stats.completedJobs}</p>
                  <p className="text-sm text-gray-600">Completed</p>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center">
                <DollarSign className="w-8 h-8 text-green-600 mr-3" />
                <div>
                  <p className="text-2xl font-bold text-gray-900">{formatCurrency(stats.totalEarnings)}</p>
                  <p className="text-sm text-gray-600">Total Earned</p>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center">
                <Clock className="w-8 h-8 text-yellow-600 mr-3" />
                <div>
                  <p className="text-2xl font-bold text-gray-900">{formatCurrency(stats.pendingPayouts)}</p>
                  <p className="text-sm text-gray-600">Pending Payouts</p>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center">
                <TrendingUp className="w-8 h-8 text-primary-600 mr-3" />
                <div>
                  <p className="text-2xl font-bold text-gray-900">{stats.rating.toFixed(1)}</p>
                  <p className="text-sm text-gray-600">Rating</p>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Tabs */}
        <div className="mb-6">
          <div className="flex space-x-2 border-b border-gray-200">
            <button
              onClick={() => setActiveTab('available')}
              className={`px-6 py-3 font-medium text-sm border-b-2 transition-colors ${
                activeTab === 'available'
                  ? 'border-primary-600 text-primary-600'
                  : 'border-transparent text-gray-600 hover:text-gray-900'
              }`}
            >
              Available Jobs ({availableJobs.length})
            </button>
            <button
              onClick={() => setActiveTab('my-jobs')}
              className={`px-6 py-3 font-medium text-sm border-b-2 transition-colors ${
                activeTab === 'my-jobs'
                  ? 'border-primary-600 text-primary-600'
                  : 'border-transparent text-gray-600 hover:text-gray-900'
              }`}
            >
              My Jobs ({myJobs.length})
            </button>
            <button
              onClick={() => setActiveTab('earnings')}
              className={`px-6 py-3 font-medium text-sm border-b-2 transition-colors ${
                activeTab === 'earnings'
                  ? 'border-primary-600 text-primary-600'
                  : 'border-transparent text-gray-600 hover:text-gray-900'
              }`}
            >
              Earnings & Payouts
            </button>
          </div>
        </div>

        {/* Tab Content */}
        {isLoading ? (
          <div className="text-center py-12">
            <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600"></div>
            <p className="mt-4 text-gray-600">Loading...</p>
          </div>
        ) : (
          <>
            {/* Available Jobs Tab */}
            {activeTab === 'available' && (
              <div className="space-y-4">
                {availableJobs.length === 0 ? (
                  <Card>
                    <CardContent className="py-12 text-center">
                      <Briefcase className="w-12 h-12 text-gray-400 mx-auto mb-4" />
                      <p className="text-gray-600">No available jobs at the moment</p>
                      <p className="text-sm text-gray-500 mt-2">Check back later for new opportunities</p>
                    </CardContent>
                  </Card>
                ) : (
                  availableJobs.map((job) => (
                    <Card key={job.id}>
                      <CardContent className="pt-6">
                        <div className="flex justify-between items-start">
                          <div className="flex-1">
                            <div className="flex items-center gap-3 mb-3">
                              <h3 className="text-lg font-semibold text-gray-900">
                                {job.jobType} - {job.vehicleType}
                              </h3>
                              <span className={`px-3 py-1 rounded-full text-xs font-medium ${getStatusColor(job.status)}`}>
                                {job.status}
                              </span>
                            </div>

                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
                              <div className="flex items-start gap-2">
                                <MapPin className="w-5 h-5 text-gray-400 mt-0.5" />
                                <div>
                                  <p className="text-sm font-medium text-gray-700">Pickup</p>
                                  <p className="text-sm text-gray-600">
                                    {job.pickupLocation.address}, {job.pickupLocation.postcode}
                                  </p>
                                </div>
                              </div>

                              <div className="flex items-start gap-2">
                                <MapPin className="w-5 h-5 text-gray-400 mt-0.5" />
                                <div>
                                  <p className="text-sm font-medium text-gray-700">Delivery</p>
                                  <p className="text-sm text-gray-600">
                                    {job.deliveryLocation.address}, {job.deliveryLocation.postcode}
                                  </p>
                                </div>
                              </div>
                            </div>

                            <div className="flex items-center gap-4 text-sm text-gray-600">
                              <div className="flex items-center gap-1">
                                <Calendar className="w-4 h-4" />
                                <span>{formatDate(job.scheduledDate)}</span>
                              </div>
                              <div className="flex items-center gap-1">
                                <Package className="w-4 h-4" />
                                <span>{job.items?.length || 0} items</span>
                              </div>
                              {job.estimatedDistance && (
                                <span>{job.estimatedDistance.toFixed(1)} miles</span>
                              )}
                            </div>

                            {job.specialInstructions && (
                              <p className="mt-3 text-sm text-gray-600 bg-gray-50 p-3 rounded">
                                <span className="font-medium">Instructions:</span> {job.specialInstructions}
                              </p>
                            )}
                          </div>

                          <div className="ml-6 text-right">
                            <p className="text-2xl font-bold text-primary-600">
                              {formatCurrency(job.estimatedCost || 0)}
                            </p>
                            <Button
                              size="sm"
                              className="mt-3"
                              onClick={() => setSelectedJob(job)}
                            >
                              <Eye className="w-4 h-4 mr-2" />
                              View Details
                            </Button>
                          </div>
                        </div>
                      </CardContent>
                    </Card>
                  ))
                )}
              </div>
            )}

            {/* My Jobs Tab */}
            {activeTab === 'my-jobs' && (
              <div className="space-y-4">
                {myJobs.length === 0 ? (
                  <Card>
                    <CardContent className="py-12 text-center">
                      <Briefcase className="w-12 h-12 text-gray-400 mx-auto mb-4" />
                      <p className="text-gray-600">You haven't accepted any jobs yet</p>
                      <p className="text-sm text-gray-500 mt-2">Browse available jobs to get started</p>
                    </CardContent>
                  </Card>
                ) : (
                  myJobs.map((job) => (
                    <Card key={job.id}>
                      <CardContent className="pt-6">
                        <div className="flex justify-between items-start">
                          <div className="flex-1">
                            <div className="flex items-center gap-3 mb-3">
                              <h3 className="text-lg font-semibold text-gray-900">
                                Job #{job.jobNumber} - {job.jobType}
                              </h3>
                              <span className={`px-3 py-1 rounded-full text-xs font-medium ${getStatusColor(job.status)}`}>
                                {job.status}
                              </span>
                            </div>

                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
                              <div className="flex items-start gap-2">
                                <MapPin className="w-5 h-5 text-gray-400 mt-0.5" />
                                <div>
                                  <p className="text-sm font-medium text-gray-700">Pickup</p>
                                  <p className="text-sm text-gray-600">
                                    {job.pickupLocation.address}, {job.pickupLocation.postcode}
                                  </p>
                                </div>
                              </div>

                              <div className="flex items-start gap-2">
                                <MapPin className="w-5 h-5 text-gray-400 mt-0.5" />
                                <div>
                                  <p className="text-sm font-medium text-gray-700">Delivery</p>
                                  <p className="text-sm text-gray-600">
                                    {job.deliveryLocation.address}, {job.deliveryLocation.postcode}
                                  </p>
                                </div>
                              </div>
                            </div>

                            <div className="flex items-center gap-4 text-sm text-gray-600">
                              <div className="flex items-center gap-1">
                                <Calendar className="w-4 h-4" />
                                <span>{formatDate(job.scheduledDate)}</span>
                              </div>
                            </div>
                          </div>

                          <div className="ml-6 text-right">
                            <p className="text-2xl font-bold text-primary-600">
                              {formatCurrency(job.finalCost || job.estimatedCost || 0)}
                            </p>
                            {job.status === JobStatus.InProgress && (
                              <Button
                                size="sm"
                                variant="success"
                                className="mt-3"
                                onClick={() => handleCompleteJob(job.id)}
                              >
                                <Check className="w-4 h-4 mr-2" />
                                Mark Complete
                              </Button>
                            )}
                          </div>
                        </div>
                      </CardContent>
                    </Card>
                  ))
                )}
              </div>
            )}

            {/* Earnings Tab */}
            {activeTab === 'earnings' && (
              <div className="space-y-6">
                {/* Summary */}
                <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                  <Card>
                    <CardContent className="pt-6">
                      <div className="text-center">
                        <p className="text-3xl font-bold text-green-600">
                          {formatCurrency(stats.totalEarnings)}
                        </p>
                        <p className="text-sm text-gray-600 mt-1">Total Earnings</p>
                      </div>
                    </CardContent>
                  </Card>

                  <Card>
                    <CardContent className="pt-6">
                      <div className="text-center">
                        <p className="text-3xl font-bold text-yellow-600">
                          {formatCurrency(stats.pendingPayouts)}
                        </p>
                        <p className="text-sm text-gray-600 mt-1">Pending Payouts</p>
                      </div>
                    </CardContent>
                  </Card>

                  <Card>
                    <CardContent className="pt-6">
                      <div className="text-center">
                        <p className="text-3xl font-bold text-primary-600">
                          {payouts.filter(p => p.status === 'Completed').length}
                        </p>
                        <p className="text-sm text-gray-600 mt-1">Completed Payouts</p>
                      </div>
                    </CardContent>
                  </Card>
                </div>

                {/* Payouts List */}
                <Card>
                  <CardHeader>
                    <CardTitle>Payout History</CardTitle>
                  </CardHeader>
                  <CardContent>
                    {payouts.length === 0 ? (
                      <div className="py-8 text-center text-gray-600">
                        No payouts yet
                      </div>
                    ) : (
                      <div className="overflow-x-auto">
                        <table className="w-full">
                          <thead className="bg-gray-50">
                            <tr>
                              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                Date
                              </th>
                              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                Amount
                              </th>
                              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                Status
                              </th>
                              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                Method
                              </th>
                            </tr>
                          </thead>
                          <tbody className="bg-white divide-y divide-gray-200">
                            {payouts.map((payout) => (
                              <tr key={payout.id}>
                                <td className="px-4 py-4 whitespace-nowrap text-sm text-gray-900">
                                  {formatDate(payout.createdAt)}
                                </td>
                                <td className="px-4 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                                  {formatCurrency(payout.amount)}
                                </td>
                                <td className="px-4 py-4 whitespace-nowrap">
                                  <span className={`px-2 py-1 inline-flex text-xs leading-5 font-semibold rounded-full ${
                                    payout.status === 'Completed'
                                      ? 'bg-green-100 text-green-800'
                                      : payout.status === 'Pending'
                                      ? 'bg-yellow-100 text-yellow-800'
                                      : 'bg-red-100 text-red-800'
                                  }`}>
                                    {payout.status}
                                  </span>
                                </td>
                                <td className="px-4 py-4 whitespace-nowrap text-sm text-gray-500">
                                  {payout.payoutMethod || 'Bank Transfer'}
                                </td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    )}
                  </CardContent>
                </Card>
              </div>
            )}
          </>
        )}
      </main>

      {/* Job Details Modal */}
      {selectedJob && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-lg max-w-2xl w-full max-h-[90vh] overflow-y-auto">
            <div className="p-6">
              <div className="flex justify-between items-start mb-6">
                <div>
                  <h2 className="text-2xl font-bold text-gray-900">Job Details</h2>
                  <p className="text-sm text-gray-600 mt-1">#{selectedJob.jobNumber}</p>
                </div>
                <button
                  onClick={() => {
                    setSelectedJob(null);
                    setBidAmount('');
                    setBidNotes('');
                  }}
                  className="text-gray-400 hover:text-gray-600"
                >
                  <X className="w-6 h-6" />
                </button>
              </div>

              <div className="space-y-6">
                {/* Job Info */}
                <div>
                  <h3 className="font-semibold text-gray-900 mb-3">Job Information</h3>
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
                      <p className="text-gray-600">Scheduled Date</p>
                      <p className="font-medium">{formatDate(selectedJob.scheduledDate)}</p>
                    </div>
                    <div>
                      <p className="text-gray-600">Distance</p>
                      <p className="font-medium">
                        {selectedJob.estimatedDistance?.toFixed(1) || 'N/A'} miles
                      </p>
                    </div>
                  </div>
                </div>

                {/* Locations */}
                <div>
                  <h3 className="font-semibold text-gray-900 mb-3">Locations</h3>
                  <div className="space-y-3">
                    <div className="bg-gray-50 p-4 rounded">
                      <p className="text-sm font-medium text-gray-700 mb-1">Pickup</p>
                      <p className="text-sm text-gray-900">{selectedJob.pickupLocation.address}</p>
                      <p className="text-sm text-gray-600">{selectedJob.pickupLocation.postcode}</p>
                    </div>
                    <div className="bg-gray-50 p-4 rounded">
                      <p className="text-sm font-medium text-gray-700 mb-1">Delivery</p>
                      <p className="text-sm text-gray-900">{selectedJob.deliveryLocation.address}</p>
                      <p className="text-sm text-gray-600">{selectedJob.deliveryLocation.postcode}</p>
                    </div>
                  </div>
                </div>

                {/* Items */}
                {selectedJob.items && selectedJob.items.length > 0 && (
                  <div>
                    <h3 className="font-semibold text-gray-900 mb-3">Items ({selectedJob.items.length})</h3>
                    <div className="space-y-2">
                      {selectedJob.items.map((item, index) => (
                        <div key={index} className="flex justify-between items-center bg-gray-50 p-3 rounded">
                          <div>
                            <p className="font-medium text-gray-900">{item.description}</p>
                            <p className="text-sm text-gray-600">
                              {item.quantity} x {item.length}" x {item.width}" x {item.height}" - {item.weight}kg
                            </p>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                {/* Special Instructions */}
                {selectedJob.specialInstructions && (
                  <div>
                    <h3 className="font-semibold text-gray-900 mb-2">Special Instructions</h3>
                    <p className="text-sm text-gray-700 bg-gray-50 p-4 rounded">
                      {selectedJob.specialInstructions}
                    </p>
                  </div>
                )}

                {/* Estimated Cost */}
                <div className="border-t pt-4">
                  <div className="flex justify-between items-center">
                    <span className="text-lg font-semibold text-gray-900">Estimated Cost</span>
                    <span className="text-2xl font-bold text-primary-600">
                      {formatCurrency(selectedJob.estimatedCost || 0)}
                    </span>
                  </div>
                </div>

                {/* Bidding Section */}
                {selectedJob.status === JobStatus.Pending && (
                  <div className="border-t pt-4">
                    <h3 className="font-semibold text-gray-900 mb-3">Place Your Bid</h3>
                    <div className="space-y-4">
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-2">
                          Bid Amount (£)
                        </label>
                        <input
                          type="number"
                          step="0.01"
                          min="0"
                          value={bidAmount}
                          onChange={(e) => setBidAmount(e.target.value)}
                          className="w-full px-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-primary-500 focus:border-transparent"
                          placeholder="Enter your bid amount"
                        />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-2">
                          Notes (optional)
                        </label>
                        <textarea
                          value={bidNotes}
                          onChange={(e) => setBidNotes(e.target.value)}
                          rows={3}
                          className="w-full px-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-primary-500 focus:border-transparent"
                          placeholder="Add any notes about your bid..."
                        />
                      </div>
                      <div className="flex gap-3">
                        <Button
                          onClick={() => handlePlaceBid(selectedJob.id)}
                          className="flex-1"
                        >
                          Place Bid
                        </Button>
                        <Button
                          onClick={() => handleAcceptJob(selectedJob.id)}
                          variant="success"
                          className="flex-1"
                        >
                          Accept at Listed Price
                        </Button>
                      </div>
                    </div>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default function DriverDashboardPage() {
  return (
    <ProtectedRoute allowedRoles={[UserRole.Driver]}>
      <DriverDashboardContent />
    </ProtectedRoute>
  );
}
