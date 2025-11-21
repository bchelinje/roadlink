'use client';

import { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { ApiService } from '@/services/api.service';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/Card';
import { Driver, UserRole, ApprovalStatus } from '@/types';
import { format } from 'date-fns';
import { CheckCircle, XCircle, Eye, LogOut } from 'lucide-react';

function AdminDashboardContent() {
  const { user, logout } = useAuth();
  const [pendingDrivers, setPendingDrivers] = useState<Driver[]>([]);
  const [selectedDriver, setSelectedDriver] = useState<Driver | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [success, setSuccess] = useState<string>('');
  const [actionLoading, setActionLoading] = useState<string | null>(null);

  useEffect(() => {
    loadPendingDrivers();
  }, []);

  const loadPendingDrivers = async () => {
    try {
      setIsLoading(true);
      const response = await ApiService.getPendingDrivers();
      setPendingDrivers(response.drivers || []);
    } catch (err: any) {
      setError(err.message || 'Failed to load pending drivers');
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
      setPendingDrivers(pendingDrivers.filter(d => d.id !== driverId));
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
      setPendingDrivers(pendingDrivers.filter(d => d.id !== driverId));
      setSelectedDriver(null);
      setTimeout(() => setSuccess(''), 3000);
    } catch (err: any) {
      setError(err.message || 'Failed to reject driver');
    } finally {
      setActionLoading(null);
    }
  };

  const viewDriverDetails = (driver: Driver) => {
    setSelectedDriver(driver);
  };

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

        {/* Stats */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
          <Card>
            <CardContent className="pt-6">
              <div className="text-center">
                <p className="text-3xl font-bold text-primary-600">{pendingDrivers.length}</p>
                <p className="text-sm text-gray-600 mt-1">Pending Approvals</p>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Pending Drivers List */}
        <Card>
          <CardHeader>
            <CardTitle>Pending Driver Applications</CardTitle>
            <CardDescription>
              Review and approve driver registrations
            </CardDescription>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <div className="text-center py-8">
                <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600 mx-auto"></div>
                <p className="mt-4 text-gray-600">Loading...</p>
              </div>
            ) : pendingDrivers.length === 0 ? (
              <div className="text-center py-8">
                <p className="text-gray-600">No pending driver applications</p>
              </div>
            ) : (
              <div className="space-y-4">
                {pendingDrivers.map((driver) => (
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
                          <p>License: {driver.drivingLicense.licenseNumber} (Class {driver.drivingLicense.licenseClass})</p>
                          <p>Vehicle: {driver.vehicle.make} {driver.vehicle.model} ({driver.vehicle.registrationNumber})</p>
                          <p>Registered: {format(new Date(driver.registeredAt), 'PPP')}</p>
                        </div>
                      </div>
                      <div className="flex gap-2 ml-4">
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => viewDriverDetails(driver)}
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
              </div>
            )}
          </CardContent>
        </Card>

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
