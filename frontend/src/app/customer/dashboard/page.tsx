'use client';

import { useAuth } from '@/contexts/AuthContext';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import { Button } from '@/components/ui/Button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { UserRole, Customer } from '@/types';
import { LogOut } from 'lucide-react';

function CustomerDashboardContent() {
  const { user, logout } = useAuth();
  const customer = user as Customer;

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white shadow">
        <div className="container py-4">
          <div className="flex justify-between items-center">
            <div>
              <h1 className="text-2xl font-bold text-gray-900">Customer Dashboard</h1>
              <p className="text-sm text-gray-600">
                Welcome, {customer?.firstName} {customer?.lastName}
                {customer?.companyName && ` (${customer.companyName})`}
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
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
          <Card>
            <CardContent className="pt-6">
              <div className="text-center">
                <p className="text-3xl font-bold text-primary-600">0</p>
                <p className="text-sm text-gray-600 mt-1">Active Jobs</p>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="pt-6">
              <div className="text-center">
                <p className="text-3xl font-bold text-primary-600">0</p>
                <p className="text-sm text-gray-600 mt-1">Completed Jobs</p>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="pt-6">
              <div className="text-center">
                <p className="text-3xl font-bold text-primary-600">£0.00</p>
                <p className="text-sm text-gray-600 mt-1">Total Spent</p>
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
                Welcome to your customer dashboard! Here's what you can do:
              </p>
              <ul className="list-disc list-inside space-y-2 text-gray-700">
                <li>Post new job listings for drivers</li>
                <li>Browse available drivers and their ratings</li>
                <li>Manage your active and completed jobs</li>
                <li>Make secure payments through the platform</li>
                <li>Rate and review drivers</li>
                <li>Track your spending and job history</li>
              </ul>
              <div className="mt-6">
                <Button>
                  Post a New Job
                </Button>
              </div>
              <p className="text-sm text-gray-500 mt-4">
                Full job posting and management features will be available here once implemented.
              </p>
            </div>
          </CardContent>
        </Card>
      </main>
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
