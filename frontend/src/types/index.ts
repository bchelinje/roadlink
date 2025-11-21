// User Types
export enum UserRole {
  SuperAdmin = 'SuperAdmin',
  Admin = 'Admin',
  Driver = 'Driver',
  Customer = 'Customer',
}

export enum ApprovalStatus {
  Pending = 'Pending',
  Approved = 'Approved',
  Rejected = 'Rejected',
  Suspended = 'Suspended',
}

export enum VehicleType {
  Motorcycle = 'Motorcycle',
  Car = 'Car',
  Van = 'Van',
  Truck = 'Truck',
}

export enum LicenseClass {
  A = 'A',
  B = 'B',
  C = 'C',
  D = 'D',
  BE = 'BE',
  CE = 'CE',
  DE = 'DE',
}

// Auth Types
export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  refreshToken: string;
  expiresIn: number;
  userId: string;
  email: string;
  role: UserRole;
  fullName: string;
}

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole;
  phoneNumber?: string;
  createdAt: string;
}

// Driver Registration Types
export interface DriverRegistrationRequest {
  email: string;
  password: string;
  confirmPassword: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  dateOfBirth: string;
  nationalInsuranceNumber: string;
  address: {
    street: string;
    city: string;
    county: string;
    postcode: string;
    country: string;
  };
  drivingLicense: {
    licenseNumber: string;
    licenseClass: LicenseClass;
    issueDate: string;
    expiryDate: string;
  };
  vehicle: {
    registrationNumber: string;
    make: string;
    model: string;
    year: number;
    vehicleType: VehicleType;
    insurancePolicyNumber: string;
    insuranceExpiryDate: string;
    motExpiryDate?: string;
  };
  bankDetails: {
    accountHolderName: string;
    sortCode: string;
    accountNumber: string;
  };
  emergencyContact: {
    name: string;
    relationship: string;
    phoneNumber: string;
  };
}

// Customer Registration Types
export interface CustomerRegistrationRequest {
  email: string;
  password: string;
  confirmPassword: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  companyName?: string;
  address: {
    street: string;
    city: string;
    county: string;
    postcode: string;
    country: string;
  };
}

// Driver Details (for admin dashboard)
export interface Driver {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  dateOfBirth: string;
  nationalInsuranceNumber: string;
  approvalStatus: ApprovalStatus;
  registeredAt: string;
  approvedAt?: string;
  rejectedAt?: string;
  rejectionReason?: string;
  address: Address;
  drivingLicense: DrivingLicense;
  vehicle: Vehicle;
  bankDetails: BankDetails;
  emergencyContact: EmergencyContact;
  documents: Document[];
  backgroundCheckStatus?: string;
  rating?: number;
  totalJobs?: number;
}

export interface Address {
  street: string;
  city: string;
  county: string;
  postcode: string;
  country: string;
}

export interface DrivingLicense {
  licenseNumber: string;
  licenseClass: LicenseClass;
  issueDate: string;
  expiryDate: string;
  verified: boolean;
  verifiedAt?: string;
}

export interface Vehicle {
  registrationNumber: string;
  make: string;
  model: string;
  year: number;
  vehicleType: VehicleType;
  insurancePolicyNumber: string;
  insuranceExpiryDate: string;
  motExpiryDate?: string;
  verified: boolean;
  verifiedAt?: string;
}

export interface BankDetails {
  accountHolderName: string;
  sortCode: string;
  accountNumber: string;
  verified: boolean;
}

export interface EmergencyContact {
  name: string;
  relationship: string;
  phoneNumber: string;
}

export interface Document {
  id: string;
  documentType: string;
  fileName: string;
  uploadedAt: string;
  verified: boolean;
  verifiedAt?: string;
}

// Customer Details
export interface Customer {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  companyName?: string;
  approvalStatus: ApprovalStatus;
  registeredAt: string;
  address: Address;
}

// Vetting Types
export interface PendingDriversResponse {
  drivers: Driver[];
  totalCount: number;
  pendingCount: number;
}

export interface ApproveDriverRequest {
  notes?: string;
}

export interface RejectDriverRequest {
  reason: string;
  notes?: string;
}

export interface SuspendDriverRequest {
  reason: string;
  notes?: string;
}

// API Response Types
export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
}

export interface ApiError {
  message: string;
  errors?: Record<string, string[]>;
  statusCode: number;
}

// Pagination
export interface PaginatedRequest {
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
