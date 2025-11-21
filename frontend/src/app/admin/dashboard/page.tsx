'use client';

import { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { ApiService } from '@/services/api.service';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/Card';
import {
  Driver,
  Customer,
  UserRole,
  ApprovalStatus,
  Job,
  JobStatus,
  Payment
} from '@/types';
import { format } from 'date-fns';
import {
  CheckCircle,
  XCircle,
  Eye,
  LogOut,
  Users,
  Briefcase,
  DollarSign,
  TrendingUp,
  AlertCircle,
  Ban,
  CheckSquare,
  Search
} from 'lucide-react';

function AdminDashboardContent() {
  const { user, logout } = useAuth();
  const [activeTab, setActiveTab] = useState<'overview' | 'drivers' | 'customers' | 'jobs'>('overview');

  // State for overview statistics
  const [stats, setStats] = useState({
    totalDrivers: 0,
    activeDrivers: 0,
    pendingDrivers: 0,
    totalCustomers: 0,
    totalJobs: 0,
    activeJobs: 0,
    completedJobs: 0,
    totalRevenue: 0,
    monthlyRevenue: 0,
  });

  // State for driver management
  const [allDrivers, setAllDrivers] = useState<Driver[]>([]);
  const [pendingDrivers, setPendingDrivers] = useState<Driver[]>([]);
  const [selectedDriver, setSelectedDriver] = useState<Driver | null>(null);

  // State for customer management
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [selectedCustomer, setSelectedCustomer] = useState<Customer | null>(null);

  // State for job management
  const [jobs, setJobs] = useState<Job[]>([]);
  const [selectedJob, setSelectedJob] = useState<Job | null>(null);
  const [payments, setPayments] = useState<Payment[]>([]);

  // UI State
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [success, setSuccess] = useState<string>('');
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');

  useEffect(() => {
    loadDashboardData();
  }, []);

  const loadDashboardData = async () => {
    try {
      setIsLoading(true);
      setError('');

      const [
        driversData,
        customersData,
        jobsData,
        paymentsData,
      ] = await Promise.all([
        ApiService.getAllDrivers().catch(() => []),
        ApiService.getAllCustomers().catch(() => []),
        ApiService.getAllJobs().catch(() => []),
        ApiService.getAllPayments().catch(() => []),
      ]);

      setAllDrivers(driversData);
      setCustomers(customersData);
      setJobs(jobsData);
      setPayments(paymentsData);

      // Filter pending drivers
      const pending = driversData.filter((d: Driver) => d.approvalStatus === ApprovalStatus.Pending);
      setPendingDrivers(pending);

      // Calculate statistics
      const activeDrivers = driversData.filter(
        (d: Driver) => d.approvalStatus === ApprovalStatus.Approved
      ).length;

      const activeJobs = jobsData.filter(
        (j: Job) => j.status === JobStatus.InProgress || j.status === JobStatus.Assigned
      ).length;

      const completedJobs = jobsData.filter(
        (j: Job) => j.status === JobStatus.Completed
      ).length;

      const totalRevenue = paymentsData
        .filter((p: Payment) => p.status === 'Completed')
        .reduce((sum: number, p: Payment) => sum + (p.amount * 0.15), 0); // 15% platform fee

      // Calculate monthly revenue (last 30 days)
      const thirtyDaysAgo = new Date();
      thirtyDaysAgo.setDate(thirtyDaysAgo.getDate() - 30);
      const monthlyRevenue = paymentsData
        .filter((p: Payment) =>
          p.status === 'Completed' &&
          new Date(p.createdAt) >= thirtyDaysAgo
        )
        .reduce((sum: number, p: Payment) => sum + (p.amount * 0.15), 0);

      setStats({
        totalDrivers: driversData.length,
        activeDrivers,
        pendingDrivers: pending.length,
        totalCustomers: customersData.length,
        totalJobs: jobsData.length,
        activeJobs,
        completedJobs,
        totalRevenue,
        monthlyRevenue,
      });
    } catch (err: any) {
      setError(err.message || 'Failed to load dashboard data');
    } finally {
      setIsLoading(false);
    }
  };

  const handleApprove = async (driverId: string) => {
    try {
      setActionLoading(driverId);
      setError('');
      await ApiService.approveDriver(driverId, {
        notes: 'Approved via admin dashboard',
      });
      setSuccess('Driver approved successfully!');
      await loadDashboardData();
      setSelectedDriver(null);
      setTimeout(() => setSuccess(''), 3000);
    } catch (err: any) {
      setError(err.message || 'Failed to approve driver');
    } finally {
      setActionLoading(null);
    }
  };

  const handleReject = async (driverId: string, reason: string) => {
    try {
      setActionLoading(driverId);
      setError('');
      await ApiService.rejectDriver(driverId, {
        reason: reason || 'Application does not meet requirements',
        notes: 'Rejected via admin dashboard',
      });
      setSuccess('Driver rejected successfully!');
      await loadDashboardData();
      setSelectedDriver(null);
      setTimeout(() => setSuccess(''), 3000);
    } catch (err: any) {
      setError(err.message || 'Failed to reject driver');
    } finally {
      setActionLoading(null);
    }
  };

  const handleSuspendDriver = async (driverId: string, reason: string) => {
    try {
      setActionLoading(driverId);
      setError('');
      await ApiService.suspendDriver(driverId, {
        reason: reason || 'Suspended by administrator',
        notes: 'Suspended via admin dashboard',
      });
      setSuccess('Driver suspended successfully!');
      await loadDashboardData();
      setSelectedDriver(null);
      setTimeout(() => setSuccess(''), 3000);
    } catch (err: any) {
      setError(err.message || 'Failed to suspend driver');
    } finally {
      setActionLoading(null);
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
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const getStatusColor = (status: string) => {
    const colors: Record<string, string> = {
      Pending: 'bg-yellow-100 text-yellow-800',
      Assigned: 'bg-blue-100 text-blue-800',
      InProgress: 'bg-purple-100 text-purple-800',
      Completed: 'bg-green-100 text-green-800',
      Cancelled: 'bg-red-100 text-red-800',
      Approved: 'bg-green-100 text-green-800',
      Rejected: 'bg-red-100 text-red-800',
      Suspended: 'bg-orange-100 text-orange-800',
    };
    return colors[status] || 'bg-gray-100 text-gray-800';
  };

  const filteredDrivers = allDrivers.filter(driver =>
    `${driver.firstName} ${driver.lastName} ${driver.email}`.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const filteredCustomers = customers.filter(customer =>
    `${customer.firstName} ${customer.lastName} ${customer.email}`.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const filteredJobs = jobs.filter(job =>
    job.jobNumber?.toLowerCase().includes(searchTerm.toLowerCase()) ||
    job.jobType?.toLowerCase().includes(searchTerm.toLowerCase())
  );

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white shadow">
        <div className="container py-4">
          <div className="flex justify-between items-center">
            <div>
              <h1 className="text-2xl font-bold text-gray-900">Admin Dashboard</h1>
              <p className="text-sm text-gray-600">
                Welcome, {user?.firstName} {user?.lastName}
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
          <Alert variant="error" className="mb-6" onClose={() => setError('')}>
            {error}
          </Alert>
        )}

        {success && (
          <Alert variant="success" className="mb-6" onClose={() => setSuccess('')}>
            {success}
          </Alert>
        )}

        {isLoading ? (
          <div className="text-center py-12">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600 mx-auto"></div>
            <p className="mt-4 text-gray-600">Loading dashboard...</p>
          </div>
        ) : (
          <>
            {/* Stats Overview - Always Visible */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
              <Card>
                <CardContent className="pt-6">
                  <div className="flex items-center">
                    <Users className="w-10 h-10 text-primary-600 mr-4" />
                    <div>
                      <p className="text-2xl font-bold text-gray-900">{stats.totalDrivers}</p>
                      <p className="text-sm text-gray-600">Total Drivers</p>
                      <p className="text-xs text-green-600 mt-1">{stats.activeDrivers} active</p>
                    </div>
                  </div>
                </CardContent>
              </Card>

              <Card>
                <CardContent className="pt-6">
                  <div className="flex items-center">
                    <AlertCircle className="w-10 h-10 text-yellow-600 mr-4" />
                    <div>
                      <p className="text-2xl font-bold text-gray-900">{stats.pendingDrivers}</p>
                      <p className="text-sm text-gray-600">Pending Approvals</p>
                    </div>
                  </div>
                </CardContent>
              </Card>

              <Card>
                <CardContent className="pt-6">
                  <div className="flex items-center">
                    <Briefcase className="w-10 h-10 text-blue-600 mr-4" />
                    <div>
                      <p className="text-2xl font-bold text-gray-900">{stats.totalJobs}</p>
                      <p className="text-sm text-gray-600">Total Jobs</p>
                      <p className="text-xs text-blue-600 mt-1">{stats.activeJobs} active</p>
                    </div>
                  </div>
                </CardContent>
              </Card>

              <Card>
                <CardContent className="pt-6">
                  <div className="flex items-center">
                    <DollarSign className="w-10 h-10 text-green-600 mr-4" />
                    <div>
                      <p className="text-2xl font-bold text-gray-900">{formatCurrency(stats.totalRevenue)}</p>
                      <p className="text-sm text-gray-600">Total Revenue</p>
                      <p className="text-xs text-green-600 mt-1">{formatCurrency(stats.monthlyRevenue)} this month</p>
                    </div>
                  </div>
                </CardContent>
              </Card>

              <Card>
                <CardContent className="pt-6">
                  <div className="flex items-center">
                    <Users className="w-10 h-10 text-purple-600 mr-4" />
                    <div>
                      <p className="text-2xl font-bold text-gray-900">{stats.totalCustomers}</p>
                      <p className="text-sm text-gray-600">Total Customers</p>
                    </div>
                  </div>
                </CardContent>
              </Card>

              <Card>
                <CardContent className="pt-6">
                  <div className="flex items-center">
                    <CheckSquare className="w-10 h-10 text-green-600 mr-4" />
                    <div>
                      <p className="text-2xl font-bold text-gray-900">{stats.completedJobs}</p>
                      <p className="text-sm text-gray-600">Completed Jobs</p>
                    </div>
                  </div>
                </CardContent>
              </Card>

              <Card>
                <CardContent className="pt-6">
                  <div className="flex items-center">
                    <TrendingUp className="w-10 h-10 text-green-600 mr-4" />
                    <div>
                      <p className="text-2xl font-bold text-gray-900">
                        {stats.completedJobs > 0 ? ((stats.completedJobs / stats.totalJobs) * 100).toFixed(1) : 0}%
                      </p>
                      <p className="text-sm text-gray-600">Completion Rate</p>
                    </div>
                  </div>
                </CardContent>
              </Card>

              <Card>
                <CardContent className="pt-6">
                  <div className="flex items-center">
                    <DollarSign className="w-10 h-10 text-blue-600 mr-4" />
                    <div>
                      <p className="text-2xl font-bold text-gray-900">
                        {stats.completedJobs > 0 ? formatCurrency(stats.totalRevenue / stats.completedJobs) : formatCurrency(0)}
                      </p>
                      <p className="text-sm text-gray-600">Avg Revenue/Job</p>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </div>

            {/* Tabs */}
            <div className="mb-6">
              <div className="flex space-x-2 border-b border-gray-200">
                <button
                  onClick={() => setActiveTab('overview')}
                  className={`px-6 py-3 font-medium text-sm border-b-2 transition-colors ${
                    activeTab === 'overview'
                      ? 'border-primary-600 text-primary-600'
                      : 'border-transparent text-gray-600 hover:text-gray-900'
                  }`}
                >
                  Overview
                </button>
                <button
                  onClick={() => setActiveTab('drivers')}
                  className={`px-6 py-3 font-medium text-sm border-b-2 transition-colors ${
                    activeTab === 'drivers'
                      ? 'border-primary-600 text-primary-600'
                      : 'border-transparent text-gray-600 hover:text-gray-900'
                  }`}
                >
                  Drivers ({allDrivers.length})
                </button>
                <button
                  onClick={() => setActiveTab('customers')}
                  className={`px-6 py-3 font-medium text-sm border-b-2 transition-colors ${
                    activeTab === 'customers'
                      ? 'border-primary-600 text-primary-600'
                      : 'border-transparent text-gray-600 hover:text-gray-900'
                  }`}
                >
                  Customers ({customers.length})
                </button>
                <button
                  onClick={() => setActiveTab('jobs')}
                  className={`px-6 py-3 font-medium text-sm border-b-2 transition-colors ${
                    activeTab === 'jobs'
                      ? 'border-primary-600 text-primary-600'
                      : 'border-transparent text-gray-600 hover:text-gray-900'
                  }`}
                >
                  Jobs ({jobs.length})
                </button>
              </div>
            </div>

            {/* Tab Content */}
            {activeTab === 'overview' && (
              <div className="space-y-6">
                {/* Pending Driver Approvals */}
                <Card>
                  <CardHeader>
                    <CardTitle>Pending Driver Applications</CardTitle>
                    <CardDescription>
                      Review and approve driver registrations ({pendingDrivers.length} pending)
                    </CardDescription>
                  </CardHeader>
                  <CardContent>
                    {pendingDrivers.length === 0 ? (
                      <div className="text-center py-8">
                        <p className="text-gray-600">No pending driver applications</p>
                      </div>
                    ) : (
                      <div className="space-y-4">
                        {pendingDrivers.slice(0, 5).map((driver) => (
                          <div
                            key={driver.id}
                            className="border border-gray-200 rounded-lg p-4 hover:bg-gray-50 transition-colors"
                          >
                            <div className="flex items-start justify-between">
                              <div className="flex-1">
                                <h4 className="font-semibold text-lg">
                                  {driver.firstName} {driver.lastName}
                                </h4>
                                <div className="mt-2 space-y-1 text-sm text-gray-600">
                                  <p>Email: {driver.email}</p>
                                  <p>Phone: {driver.phoneNumber}</p>
                                  <p>License: {driver.drivingLicense.licenseNumber}</p>
                                  <p>Registered: {formatDate(driver.registeredAt)}</p>
                                </div>
                              </div>
                              <div className="flex gap-2 ml-4">
                                <Button
                                  size="sm"
                                  variant="outline"
                                  onClick={() => setSelectedDriver(driver)}
                                >
                                  <Eye className="w-4 h-4 mr-1" />
                                  View
                                </Button>
                                <Button
                                  size="sm"
                                  variant="success"
                                  onClick={() => handleApprove(driver.id)}
                                  isLoading={actionLoading === driver.id}
                                >
                                  <CheckCircle className="w-4 h-4 mr-1" />
                                  Approve
                                </Button>
                                <Button
                                  size="sm"
                                  variant="danger"
                                  onClick={() => handleReject(driver.id, 'Does not meet requirements')}
                                  isLoading={actionLoading === driver.id}
                                >
                                  <XCircle className="w-4 h-4 mr-1" />
                                  Reject
                                </Button>
                              </div>
                            </div>
                          </div>
                        ))}
                        {pendingDrivers.length > 5 && (
                          <div className="text-center pt-4">
                            <Button variant="outline" onClick={() => setActiveTab('drivers')}>
                              View All Pending Drivers ({pendingDrivers.length})
                            </Button>
                          </div>
                        )}
                      </div>
                    )}
                  </CardContent>
                </Card>

                {/* Recent Jobs */}
                <Card>
                  <CardHeader>
                    <CardTitle>Recent Jobs</CardTitle>
                    <CardDescription>Latest job activities</CardDescription>
                  </CardHeader>
                  <CardContent>
                    {jobs.length === 0 ? (
                      <div className="text-center py-8 text-gray-600">No jobs yet</div>
                    ) : (
                      <div className="overflow-x-auto">
                        <table className="w-full">
                          <thead className="bg-gray-50">
                            <tr>
                              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Job #</th>
                              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Type</th>
                              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
                              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Cost</th>
                              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Date</th>
                            </tr>
                          </thead>
                          <tbody className="bg-white divide-y divide-gray-200">
                            {jobs.slice(0, 10).map((job) => (
                              <tr key={job.id} className="hover:bg-gray-50">
                                <td className="px-4 py-4 whitespace-nowrap text-sm font-medium">{job.jobNumber}</td>
                                <td className="px-4 py-4 whitespace-nowrap text-sm">{job.jobType}</td>
                                <td className="px-4 py-4 whitespace-nowrap">
                                  <span className={`px-2 py-1 text-xs rounded-full ${getStatusColor(job.status)}`}>
                                    {job.status}
                                  </span>
                                </td>
                                <td className="px-4 py-4 whitespace-nowrap text-sm">{formatCurrency(job.estimatedCost || 0)}</td>
                                <td className="px-4 py-4 whitespace-nowrap text-sm">{formatDate(job.createdAt)}</td>
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

            {activeTab === 'drivers' && (
              <Card>
                <CardHeader>
                  <div className="flex justify-between items-center">
                    <div>
                      <CardTitle>All Drivers</CardTitle>
                      <CardDescription>Manage driver accounts</CardDescription>
                    </div>
                    <div className="relative">
                      <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
                      <input
                        type="text"
                        placeholder="Search drivers..."
                        value={searchTerm}
                        onChange={(e) => setSearchTerm(e.target.value)}
                        className="pl-10 pr-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-primary-500 focus:border-transparent"
                      />
                    </div>
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="overflow-x-auto">
                    <table className="w-full">
                      <thead className="bg-gray-50">
                        <tr>
                          <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Name</th>
                          <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Email</th>
                          <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
                          <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Jobs</th>
                          <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Rating</th>
                          <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Actions</th>
                        </tr>
                      </thead>
                      <tbody className="bg-white divide-y divide-gray-200">
                        {filteredDrivers.map((driver) => (
                          <tr key={driver.id} className="hover:bg-gray-50">
                            <td className="px-4 py-4 whitespace-nowrap">
                              <div className="font-medium text-gray-900">{driver.firstName} {driver.lastName}</div>
                            </td>
                            <td className="px-4 py-4 whitespace-nowrap text-sm text-gray-600">{driver.email}</td>
                            <td className="px-4 py-4 whitespace-nowrap">
                              <span className={`px-2 py-1 text-xs rounded-full ${getStatusColor(driver.approvalStatus)}`}>
                                {driver.approvalStatus}
                              </span>
                            </td>
                            <td className="px-4 py-4 whitespace-nowrap text-sm">{driver.totalJobs || 0}</td>
                            <td className="px-4 py-4 whitespace-nowrap text-sm">{driver.rating?.toFixed(1) || 'N/A'}</td>
                            <td className="px-4 py-4 whitespace-nowrap text-sm">
                              <div className="flex gap-2">
                                <Button size="sm" variant="outline" onClick={() => setSelectedDriver(driver)}>
                                  <Eye className="w-3 h-3" />
                                </Button>
                                {driver.approvalStatus === ApprovalStatus.Approved && (
                                  <Button
                                    size="sm"
                                    variant="danger"
                                    onClick={() => handleSuspendDriver(driver.id, 'Suspended by admin')}
                                    isLoading={actionLoading === driver.id}
                                  >
                                    <Ban className="w-3 h-3" />
                                  </Button>
                                )}
                              </div>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </CardContent>
              </Card>
            )}

            {activeTab === 'customers' && (
              <Card>
                <CardHeader>
                  <div className="flex justify-between items-center">
                    <div>
                      <CardTitle>All Customers</CardTitle>
                      <CardDescription>Manage customer accounts</CardDescription>
                    </div>
                    <div className="relative">
                      <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
                      <input
                        type="text"
                        placeholder="Search customers..."
                        value={searchTerm}
                        onChange={(e) => setSearchTerm(e.target.value)}
                        className="pl-10 pr-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-primary-500 focus:border-transparent"
                      />
                    </div>
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="overflow-x-auto">
                    <table className="w-full">
                      <thead className="bg-gray-50">
                        <tr>
                          <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Name</th>
                          <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Email</th>
                          <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Phone</th>
                          <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Jobs</th>
                          <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Registered</th>
                          <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Actions</th>
                        </tr>
                      </thead>
                      <tbody className="bg-white divide-y divide-gray-200">
                        {filteredCustomers.map((customer) => (
                          <tr key={customer.id} className="hover:bg-gray-50">
                            <td className="px-4 py-4 whitespace-nowrap">
                              <div className="font-medium text-gray-900">{customer.firstName} {customer.lastName}</div>
                            </td>
                            <td className="px-4 py-4 whitespace-nowrap text-sm text-gray-600">{customer.email}</td>
                            <td className="px-4 py-4 whitespace-nowrap text-sm">{customer.phoneNumber}</td>
                            <td className="px-4 py-4 whitespace-nowrap text-sm">{customer.totalJobs || 0}</td>
                            <td className="px-4 py-4 whitespace-nowrap text-sm">{formatDate(customer.registeredAt)}</td>
                            <td className="px-4 py-4 whitespace-nowrap text-sm">
                              <Button size="sm" variant="outline" onClick={() => setSelectedCustomer(customer)}>
                                <Eye className="w-3 h-3 mr-1" />
                                View
                              </Button>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </CardContent>
              </Card>
            )}

            {activeTab === 'jobs' && (
              <Card>
                <CardHeader>
                  <div className="flex justify-between items-center">
                    <div>
                      <CardTitle>All Jobs</CardTitle>
                      <CardDescription>Manage and monitor all jobs</CardDescription>
                    </div>
                    <div className="relative">
                      <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
                      <input
                        type="text"
                        placeholder="Search jobs..."
                        value={searchTerm}
                        onChange={(e) => setSearchTerm(e.target.value)}
                        className="pl-10 pr-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-primary-500 focus:border-transparent"
                      />
                    </div>
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="overflow-x-auto">
                    <table className="w-full">
                      <thead className="bg-gray-50">
                        <tr>
                          <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Job #</th>
                          <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Type</th>
                          <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
                          <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Customer</th>
                          <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Driver</th>
                          <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Cost</th>
                          <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Date</th>
                          <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Actions</th>
                        </tr>
                      </thead>
                      <tbody className="bg-white divide-y divide-gray-200">
                        {filteredJobs.map((job) => (
                          <tr key={job.id} className="hover:bg-gray-50">
                            <td className="px-4 py-4 whitespace-nowrap text-sm font-medium">{job.jobNumber}</td>
                            <td className="px-4 py-4 whitespace-nowrap text-sm">{job.jobType}</td>
                            <td className="px-4 py-4 whitespace-nowrap">
                              <span className={`px-2 py-1 text-xs rounded-full ${getStatusColor(job.status)}`}>
                                {job.status}
                              </span>
                            </td>
                            <td className="px-4 py-4 whitespace-nowrap text-sm">{job.customerId || 'N/A'}</td>
                            <td className="px-4 py-4 whitespace-nowrap text-sm">{job.driverId || 'Unassigned'}</td>
                            <td className="px-4 py-4 whitespace-nowrap text-sm">{formatCurrency(job.estimatedCost || 0)}</td>
                            <td className="px-4 py-4 whitespace-nowrap text-sm">{formatDate(job.scheduledDate)}</td>
                            <td className="px-4 py-4 whitespace-nowrap text-sm">
                              <Button size="sm" variant="outline" onClick={() => setSelectedJob(job)}>
                                <Eye className="w-3 h-3" />
                              </Button>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </CardContent>
              </Card>
            )}
          </>
        )}

        {/* Driver Details Modal */}
        {selectedDriver && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
            <div className="bg-white rounded-lg max-w-4xl w-full max-h-[90vh] overflow-y-auto">
              <div className="p-6">
                <div className="flex justify-between items-start mb-6">
                  <h2 className="text-2xl font-bold">Driver Details</h2>
                  <button
                    onClick={() => setSelectedDriver(null)}
                    className="text-gray-500 hover:text-gray-700"
                  >
                    <XCircle className="w-6 h-6" />
                  </button>
                </div>

                <div className="space-y-6">
                  {/* Personal Information */}
                  <div>
                    <h3 className="text-lg font-semibold mb-3">Personal Information</h3>
                    <div className="grid grid-cols-2 gap-4 text-sm">
                      <div>
                        <p className="text-gray-600">Full Name</p>
                        <p className="font-medium">{selectedDriver.firstName} {selectedDriver.lastName}</p>
                      </div>
                      <div>
                        <p className="text-gray-600">Email</p>
                        <p className="font-medium">{selectedDriver.email}</p>
                      </div>
                      <div>
                        <p className="text-gray-600">Phone</p>
                        <p className="font-medium">{selectedDriver.phoneNumber}</p>
                      </div>
                      <div>
                        <p className="text-gray-600">Date of Birth</p>
                        <p className="font-medium">{format(new Date(selectedDriver.dateOfBirth), 'PPP')}</p>
                      </div>
                      <div>
                        <p className="text-gray-600">NI Number</p>
                        <p className="font-medium">{selectedDriver.nationalInsuranceNumber}</p>
                      </div>
                      <div>
                        <p className="text-gray-600">Status</p>
                        <span className={`px-2 py-1 text-xs rounded-full ${getStatusColor(selectedDriver.approvalStatus)}`}>
                          {selectedDriver.approvalStatus}
                        </span>
                      </div>
                    </div>
                  </div>

                  {/* Address */}
                  <div>
                    <h3 className="text-lg font-semibold mb-3">Address</h3>
                    <p className="text-sm">
                      {selectedDriver.address.street}, {selectedDriver.address.city},{' '}
                      {selectedDriver.address.county}, {selectedDriver.address.postcode},{' '}
                      {selectedDriver.address.country}
                    </p>
                  </div>

                  {/* Driving License */}
                  <div>
                    <h3 className="text-lg font-semibold mb-3">Driving License</h3>
                    <div className="grid grid-cols-2 gap-4 text-sm">
                      <div>
                        <p className="text-gray-600">License Number</p>
                        <p className="font-medium">{selectedDriver.drivingLicense.licenseNumber}</p>
                      </div>
                      <div>
                        <p className="text-gray-600">License Class</p>
                        <p className="font-medium">Class {selectedDriver.drivingLicense.licenseClass}</p>
                      </div>
                      <div>
                        <p className="text-gray-600">Issue Date</p>
                        <p className="font-medium">{format(new Date(selectedDriver.drivingLicense.issueDate), 'PPP')}</p>
                      </div>
                      <div>
                        <p className="text-gray-600">Expiry Date</p>
                        <p className="font-medium">{format(new Date(selectedDriver.drivingLicense.expiryDate), 'PPP')}</p>
                      </div>
                    </div>
                  </div>

                  {/* Vehicle */}
                  <div>
                    <h3 className="text-lg font-semibold mb-3">Vehicle Information</h3>
                    <div className="grid grid-cols-2 gap-4 text-sm">
                      <div>
                        <p className="text-gray-600">Registration</p>
                        <p className="font-medium">{selectedDriver.vehicle.registrationNumber}</p>
                      </div>
                      <div>
                        <p className="text-gray-600">Vehicle</p>
                        <p className="font-medium">{selectedDriver.vehicle.make} {selectedDriver.vehicle.model} ({selectedDriver.vehicle.year})</p>
                      </div>
                      <div>
                        <p className="text-gray-600">Type</p>
                        <p className="font-medium">{selectedDriver.vehicle.vehicleType}</p>
                      </div>
                      <div>
                        <p className="text-gray-600">Insurance Policy</p>
                        <p className="font-medium">{selectedDriver.vehicle.insurancePolicyNumber}</p>
                      </div>
                      <div>
                        <p className="text-gray-600">Insurance Expiry</p>
                        <p className="font-medium">{format(new Date(selectedDriver.vehicle.insuranceExpiryDate), 'PPP')}</p>
                      </div>
                      {selectedDriver.vehicle.motExpiryDate && (
                        <div>
                          <p className="text-gray-600">MOT Expiry</p>
                          <p className="font-medium">{format(new Date(selectedDriver.vehicle.motExpiryDate), 'PPP')}</p>
                        </div>
                      )}
                    </div>
                  </div>

                  {/* Emergency Contact */}
                  <div>
                    <h3 className="text-lg font-semibold mb-3">Emergency Contact</h3>
                    <div className="grid grid-cols-2 gap-4 text-sm">
                      <div>
                        <p className="text-gray-600">Name</p>
                        <p className="font-medium">{selectedDriver.emergencyContact.name}</p>
                      </div>
                      <div>
                        <p className="text-gray-600">Relationship</p>
                        <p className="font-medium">{selectedDriver.emergencyContact.relationship}</p>
                      </div>
                      <div>
                        <p className="text-gray-600">Phone</p>
                        <p className="font-medium">{selectedDriver.emergencyContact.phoneNumber}</p>
                      </div>
                    </div>
                  </div>

                  {/* Actions */}
                  <div className="flex gap-4 pt-4 border-t">
                    {selectedDriver.approvalStatus === ApprovalStatus.Pending && (
                      <>
                        <Button
                          variant="success"
                          onClick={() => handleApprove(selectedDriver.id)}
                          isLoading={actionLoading === selectedDriver.id}
                        >
                          <CheckCircle className="w-4 h-4 mr-2" />
                          Approve Driver
                        </Button>
                        <Button
                          variant="danger"
                          onClick={() => handleReject(selectedDriver.id, 'Does not meet requirements')}
                          isLoading={actionLoading === selectedDriver.id}
                        >
                          <XCircle className="w-4 h-4 mr-2" />
                          Reject Driver
                        </Button>
                      </>
                    )}
                    {selectedDriver.approvalStatus === ApprovalStatus.Approved && (
                      <Button
                        variant="danger"
                        onClick={() => handleSuspendDriver(selectedDriver.id, 'Suspended by administrator')}
                        isLoading={actionLoading === selectedDriver.id}
                      >
                        <Ban className="w-4 h-4 mr-2" />
                        Suspend Driver
                      </Button>
                    )}
                    <Button
                      variant="outline"
                      onClick={() => setSelectedDriver(null)}
                    >
                      Close
                    </Button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Customer Details Modal */}
        {selectedCustomer && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
            <div className="bg-white rounded-lg max-w-2xl w-full max-h-[90vh] overflow-y-auto">
              <div className="p-6">
                <div className="flex justify-between items-start mb-6">
                  <h2 className="text-2xl font-bold">Customer Details</h2>
                  <button
                    onClick={() => setSelectedCustomer(null)}
                    className="text-gray-500 hover:text-gray-700"
                  >
                    <XCircle className="w-6 h-6" />
                  </button>
                </div>

                <div className="space-y-6">
                  <div>
                    <h3 className="text-lg font-semibold mb-3">Personal Information</h3>
                    <div className="grid grid-cols-2 gap-4 text-sm">
                      <div>
                        <p className="text-gray-600">Full Name</p>
                        <p className="font-medium">{selectedCustomer.firstName} {selectedCustomer.lastName}</p>
                      </div>
                      <div>
                        <p className="text-gray-600">Email</p>
                        <p className="font-medium">{selectedCustomer.email}</p>
                      </div>
                      <div>
                        <p className="text-gray-600">Phone</p>
                        <p className="font-medium">{selectedCustomer.phoneNumber}</p>
                      </div>
                      <div>
                        <p className="text-gray-600">Total Jobs</p>
                        <p className="font-medium">{selectedCustomer.totalJobs || 0}</p>
                      </div>
                      <div>
                        <p className="text-gray-600">Registered</p>
                        <p className="font-medium">{formatDate(selectedCustomer.registeredAt)}</p>
                      </div>
                    </div>
                  </div>

                  <div>
                    <h3 className="text-lg font-semibold mb-3">Address</h3>
                    <p className="text-sm">
                      {selectedCustomer.address.street}, {selectedCustomer.address.city},{' '}
                      {selectedCustomer.address.county}, {selectedCustomer.address.postcode},{' '}
                      {selectedCustomer.address.country}
                    </p>
                  </div>

                  {selectedCustomer.companyName && (
                    <div>
                      <h3 className="text-lg font-semibold mb-3">Company</h3>
                      <p className="text-sm font-medium">{selectedCustomer.companyName}</p>
                    </div>
                  )}

                  <div className="flex gap-4 pt-4 border-t">
                    <Button variant="outline" onClick={() => setSelectedCustomer(null)}>
                      Close
                    </Button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}
      </main>
    </div>
  );
}

export default function AdminDashboardPage() {
  return (
    <ProtectedRoute allowedRoles={[UserRole.Admin, UserRole.SuperAdmin]}>
      <AdminDashboardContent />
    </ProtectedRoute>
  );
}
