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

// ==================== JOB TYPES ====================

export enum JobType {
  Delivery = 'Delivery',
  Moving = 'Moving',
  Courier = 'Courier',
  Removal = 'Removal',
  Errands = 'Errands',
  Other = 'Other',
}

export enum JobStatus {
  Pending = 'Pending',
  Assigned = 'Assigned',
  InProgress = 'InProgress',
  Completed = 'Completed',
  Cancelled = 'Cancelled',
}

export enum JobPriority {
  Low = 'Low',
  Normal = 'Normal',
  High = 'High',
  Urgent = 'Urgent',
}

export interface Location {
  address: string;
  city: string;
  county?: string;
  postcode: string;
  latitude?: number;
  longitude?: number;
  contactName?: string;
  contactPhone?: string;
  instructions?: string;
}

export interface JobItem {
  description: string;
  quantity: number;
  weight?: number;
  dimensions?: string;
}

export interface JobStop {
  id: string;
  jobId: string;
  sequence: number;
  location: Location;
  status: 'Pending' | 'InProgress' | 'Completed';
  arrivalTime?: string;
  departureTime?: string;
  photos?: string[];
  notes?: string;
}

export interface Job {
  id: string;
  jobNumber: string;
  jobType: JobType;
  status: JobStatus;
  priority: JobPriority;
  vehicleType: VehicleType;
  customerId: string;
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  driverId?: string;
  driverName?: string;
  driverPhone?: string;
  pickupLocation: Location;
  deliveryLocation: Location;
  scheduledDate: string;
  scheduledTime?: string;
  actualStartTime?: string;
  actualEndTime?: string;
  distanceInMiles: number;
  estimatedDurationMinutes: number;
  items: JobItem[];
  specialInstructions?: string;
  totalPrice: number;
  platformFee: number;
  driverEarnings: number;
  paymentStatus: string;
  photos?: string[];
  stops?: JobStop[];
  createdAt: string;
  updatedAt: string;
  cancelledAt?: string;
  cancellationReason?: string;
}

export interface CreateJobDto {
  customerName: string;
  customerPhone: string;
  customerEmail: string;
  jobType: JobType;
  vehicleType: VehicleType;
  priority: JobPriority;
  scheduledDate: string;
  scheduledTime?: string;
  pickupLocation: Location;
  deliveryLocation: Location;
  distanceInMiles?: number;
  items: JobItem[];
  specialInstructions?: string;
}

export interface BookJobDto {
  jobType: JobType;
  vehicleType: VehicleType;
  priority: JobPriority;
  scheduledDate: string;
  scheduledTime?: string;
  customerPhone: string;
  customerEmail: string;
  pickupLocation: Location;
  deliveryLocation: Location;
  distanceInMiles: number;
  items: JobItem[];
  specialInstructions?: string;
}

export interface BookJobResponse {
  jobId: string;
  jobNumber: string;
  amount: number;
  platformFee: number;
  driverEarnings: number;
  paymentId: string;
  clientSecret: string;
  pricingBreakdown: PricingBreakdown;
}

export interface PricingBreakdown {
  baseFare: number;
  distanceCharge: number;
  timeCharge: number;
  vehicleTypeCharge: number;
  priorityCharge: number;
  surgeMultiplier: number;
  subtotal: number;
  platformFee: number;
  totalPrice: number;
  driverEarnings: number;
}

export interface UpdateJobStatusDto {
  status: JobStatus;
  actualStartTime?: string;
  actualEndTime?: string;
  notes?: string;
}

export interface RescheduleJobDto {
  newScheduledDate: string;
  newScheduledTime?: string;
  reason?: string;
}

export interface CancelJobDto {
  reason: string;
  notes?: string;
}

// ==================== JOB BIDDING TYPES ====================

export enum BidStatus {
  Pending = 'Pending',
  Accepted = 'Accepted',
  Rejected = 'Rejected',
  Withdrawn = 'Withdrawn',
}

export interface JobBid {
  id: string;
  jobId: string;
  driverId: string;
  driverName: string;
  driverRating: number;
  bidAmount: number;
  message?: string;
  estimatedDuration: number;
  proposedPickupTime: string;
  status: BidStatus;
  createdAt: string;
  expiresAt?: string;
  respondedAt?: string;
}

export interface CreateJobBidDto {
  jobId: string;
  bidAmount: number;
  message?: string;
  estimatedDuration: number;
  proposedPickupTime: string;
  expiresAt?: string;
}

// ==================== PAYMENT TYPES ====================

export enum PaymentStatus {
  Pending = 'Pending',
  Completed = 'Completed',
  Failed = 'Failed',
  Refunded = 'Refunded',
}

export enum PaymentMethod {
  Card = 'Card',
  BankTransfer = 'BankTransfer',
  Cash = 'Cash',
}

export interface Payment {
  id: string;
  customerId: string;
  driverId?: string;
  jobId: string;
  amount: number;
  tipAmount: number;
  currency: string;
  paymentMethod: PaymentMethod;
  status: PaymentStatus;
  stripePaymentIntentId?: string;
  description?: string;
  createdAt: string;
  paidAt?: string;
  refundedAt?: string;
  refundAmount?: number;
}

export interface CreatePaymentDto {
  customerId: string;
  driverId?: string;
  jobId: string;
  amount: number;
  tipAmount?: number;
  currency?: string;
  paymentMethod: PaymentMethod;
  description?: string;
}

export interface ProcessRefundDto {
  amount: number;
  reason: string;
  notes?: string;
}

export interface Payout {
  id: string;
  driverId: string;
  amount: number;
  currency: string;
  status: string;
  periodStart: string;
  periodEnd: string;
  createdAt: string;
  paidAt?: string;
}

// ==================== REVIEW TYPES ====================

export enum RevieweeType {
  Customer = 'Customer',
  Driver = 'Driver',
}

export interface Review {
  id: string;
  reviewerId: string;
  reviewerName: string;
  revieweeId: string;
  revieweeName: string;
  revieweeType: RevieweeType;
  jobId: string;
  rating: number;
  comment?: string;
  photos?: string[];
  response?: string;
  responseDate?: string;
  isAnonymous: boolean;
  isVerified: boolean;
  helpfulCount: number;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateReviewDto {
  revieweeId: string;
  revieweeName: string;
  revieweeType: RevieweeType;
  jobId: string;
  rating: number;
  comment?: string;
  photos?: string[];
  isAnonymous?: boolean;
}

export interface UpdateReviewDto {
  rating: number;
  comment?: string;
  photos?: string[];
}

// ==================== NOTIFICATION TYPES ====================

export enum NotificationType {
  JobAssigned = 'JobAssigned',
  JobAccepted = 'JobAccepted',
  JobCompleted = 'JobCompleted',
  JobCancelled = 'JobCancelled',
  PaymentReceived = 'PaymentReceived',
  ReviewReceived = 'ReviewReceived',
  DocumentVerified = 'DocumentVerified',
  DocumentRejected = 'DocumentRejected',
  ApprovalGranted = 'ApprovalGranted',
  ApprovalRejected = 'ApprovalRejected',
  System = 'System',
}

export enum NotificationPriority {
  Low = 'Low',
  Normal = 'Normal',
  High = 'High',
  Urgent = 'Urgent',
}

export interface Notification {
  id: string;
  userId: string;
  type: NotificationType;
  title: string;
  message: string;
  priority: NotificationPriority;
  isRead: boolean;
  readAt?: string;
  actionUrl?: string;
  metadata?: Record<string, any>;
  createdAt: string;
  expiresAt?: string;
}

export interface NotificationPreferences {
  emailJobAssigned: boolean;
  emailJobCompleted: boolean;
  emailPaymentReceived: boolean;
  emailReviewReceived: boolean;
  pushJobAssigned: boolean;
  pushJobCompleted: boolean;
  pushPaymentReceived: boolean;
  pushReviewReceived: boolean;
  smsJobAssigned: boolean;
  smsJobCompleted: boolean;
  quietHoursStart?: string;
  quietHoursEnd?: string;
}

// ==================== DOCUMENT TYPES ====================

export enum DocumentType {
  DrivingLicense = 'DrivingLicense',
  Insurance = 'Insurance',
  MOT = 'MOT',
  VehicleRegistration = 'VehicleRegistration',
  ProofOfAddress = 'ProofOfAddress',
  Other = 'Other',
}

export enum DocumentStatus {
  Pending = 'Pending',
  Verified = 'Verified',
  Rejected = 'Rejected',
  Expired = 'Expired',
}

export interface DocumentDetails {
  id: string;
  driverId: string;
  documentType: DocumentType;
  fileName: string;
  fileUrl: string;
  status: DocumentStatus;
  uploadedAt: string;
  verifiedAt?: string;
  rejectedAt?: string;
  rejectionReason?: string;
  expiryDate?: string;
}

export interface UploadDocumentDto {
  documentType: DocumentType;
  file: File;
  expiryDate?: string;
}

// ==================== PRICING TYPES ====================

export interface PricingCalculationRequest {
  pickupAddress: string;
  deliveryAddress: string;
  vehicleType: VehicleType;
  scheduledDate: string;
  distanceInMiles?: number;
  estimatedDurationMinutes?: number;
}

export interface PricingCalculationResult {
  baseFare: number;
  distanceCharge: number;
  timeCharge: number;
  vehicleTypeCharge: number;
  priorityCharge: number;
  surgeMultiplier: number;
  subtotal: number;
  platformFee: number;
  totalPrice: number;
  driverEarnings: number;
  breakdown: PricingBreakdown;
}

// ==================== JOB TEMPLATE TYPES ====================

export interface JobTemplate {
  id: string;
  customerId: string;
  templateName: string;
  jobType: JobType;
  vehicleType: VehicleType;
  pickupLocation: Location;
  deliveryLocation: Location;
  estimatedDistance: number;
  items: JobItem[];
  specialInstructions?: string;
  isDefault: boolean;
  createdAt: string;
}

export interface CreateJobTemplateDto {
  templateName: string;
  jobType: JobType;
  vehicleType: VehicleType;
  pickupLocation: Location;
  deliveryLocation: Location;
  estimatedDistance: number;
  items: JobItem[];
  specialInstructions?: string;
  isDefault?: boolean;
}

// ==================== RECURRING JOB TYPES ====================

export enum RecurringFrequency {
  Daily = 'Daily',
  Weekly = 'Weekly',
  BiWeekly = 'BiWeekly',
  Monthly = 'Monthly',
}

export enum RecurringJobStatus {
  Active = 'Active',
  Paused = 'Paused',
  Cancelled = 'Cancelled',
  Completed = 'Completed',
}

export interface RecurringJob {
  id: string;
  customerId: string;
  name: string;
  jobType: JobType;
  frequency: RecurringFrequency;
  startDate: string;
  endDate?: string;
  occurrenceCount?: number;
  preferredTime: string;
  status: RecurringJobStatus;
  nextOccurrence?: string;
  jobsCreated: number;
  createdAt: string;
}

export interface CreateRecurringJobDto {
  name: string;
  jobType: JobType;
  frequency: RecurringFrequency;
  startDate: string;
  endDate?: string;
  occurrenceCount?: number;
  preferredTime: string;
}

// ==================== MESSAGE TYPES ====================

export interface Message {
  id: string;
  senderId: string;
  senderName: string;
  receiverId: string;
  receiverName: string;
  jobId?: string;
  content: string;
  isRead: boolean;
  readAt?: string;
  createdAt: string;
}

export interface SendMessageDto {
  receiverId: string;
  jobId?: string;
  content: string;
}

// ==================== STATISTICS TYPES ====================

export interface AdminStatistics {
  totalUsers: number;
  totalDrivers: number;
  totalCustomers: number;
  pendingApprovals: number;
  totalJobs: number;
  activeJobs: number;
  completedJobs: number;
  totalRevenue: number;
  platformFees: number;
  driverEarnings: number;
}
