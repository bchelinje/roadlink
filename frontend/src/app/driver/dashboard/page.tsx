'use client';

import { useAuth } from '@/contexts/AuthContext';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import { Button } from '@/components/ui/Button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { UserRole, ApprovalStatus, Driver } from '@/types';
import { LogOut, AlertCircle } from 'lucide-react';
import { Alert } from '@/components/ui/Alert';

function DriverDashboardContent() {
  const { user, logout } = useAuth();
  const driver = user as Driver;

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
        {driver?.approvalStatus === ApprovalStatus.Pending && (
          <Alert variant="warning" className="mb-6">
            <AlertCircle className="w-5 h-5" />
            <div className="ml-3">
              <h3 className="font-semibold">Application Pending</h3>
              <p className="mt-1">Your driver application is currently under review. You will be notified once approved.</p>
            </div>
          </Alert>
        )}

        {driver?.approvalStatus === ApprovalStatus.Rejected && (
          <Alert variant="error" className="mb-6">
            <div>
              <h3 className="font-semibold">Application Rejected</h3>
              <p className="mt-1">Unfortunately, your driver application was not approved.</p>
              {driver.rejectionReason && (
                <p className="mt-2 text-sm">Reason: {driver.rejectionReason}</p>
              )}
            </div>
          </Alert>
        )}

        {driver?.approvalStatus === ApprovalStatus.Approved && (
          <div className="mb-6">
            <Alert variant="success">
              <div>
                <h3 className="font-semibold">Account Approved</h3>
                <p className="mt-1">Your driver account is active and you can start accepting jobs!</p>
              </div>
            </Alert>
          </div>
        )}

        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
          <Card>
            <CardContent className="pt-6">
              <div className="text-center">
                <p className="text-3xl font-bold text-primary-600">{driver?.totalJobs || 0}</p>
                <p className="text-sm text-gray-600 mt-1">Total Jobs</p>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="pt-6">
              <div className="text-center">
                <p className="text-3xl font-bold text-primary-600">{driver?.rating?.toFixed(1) || 'N/A'}</p>
                <p className="text-sm text-gray-600 mt-1">Rating</p>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="pt-6">
              <div className="text-center">
                <p className="text-3xl font-bold text-primary-600">{driver?.approvalStatus || 'Pending'}</p>
                <p className="text-sm text-gray-600 mt-1">Status</p>
              </div>
            </CardContent>
          </Card>
        </div>

        <Card>
          <CardHeader>
            <CardTitle>Getting Started</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              <p className="text-gray-600">
                This is your driver dashboard. Once your application is approved, you'll be able to:
              </p>
              <ul className="list-disc list-inside space-y-2 text-gray-700">
                <li>Browse and accept available jobs</li>
                <li>View your earnings and payment history</li>
                <li>Update your profile and vehicle information</li>
                <li>Communicate with customers</li>
                <li>Track your performance and ratings</li>
              </ul>
              <p className="text-sm text-gray-500 mt-4">
                Full job marketplace features will be available here once implemented.
              </p>
            </div>
          </CardContent>
        </Card>
      </main>
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
