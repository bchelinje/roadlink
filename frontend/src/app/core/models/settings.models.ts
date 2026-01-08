// User Settings Models
export interface UserSettings {
  id: string;
  userId: string;
  // Profile Settings
  preferredLanguage?: string;
  timeZone?: string;
  currency?: string;
  dateFormat?: string;
  timeFormat?: string;
  // Privacy Settings
  showProfileToPublic: boolean;
  allowDataSharing: boolean;
  shareLocationWithDriver: boolean;
  showOnlineStatus: boolean;
  allowMarketingEmails: boolean;
  // Security Settings
  twoFactorEnabled: boolean;
  emailVerificationRequired: boolean;
  phoneVerificationRequired: boolean;
  sessionTimeoutMinutes: number;
  requirePasswordChangeEvery90Days: boolean;
  // Communication Preferences
  preferredContactMethod?: string;
  // Display Preferences
  theme?: string;
  highContrastMode: boolean;
  reducedMotion: boolean;
  // Audit
  createdAt: Date;
  updatedAt: Date;
}

export interface UpdateUserSettingsDto {
  // Profile Settings
  preferredLanguage?: string;
  timeZone?: string;
  currency?: string;
  dateFormat?: string;
  timeFormat?: string;
  // Privacy Settings
  showProfileToPublic?: boolean;
  allowDataSharing?: boolean;
  shareLocationWithDriver?: boolean;
  showOnlineStatus?: boolean;
  allowMarketingEmails?: boolean;
  // Security Settings
  twoFactorEnabled?: boolean;
  emailVerificationRequired?: boolean;
  phoneVerificationRequired?: boolean;
  sessionTimeoutMinutes?: number;
  requirePasswordChangeEvery90Days?: boolean;
  // Communication Preferences
  preferredContactMethod?: string;
  // Display Preferences
  theme?: string;
  highContrastMode?: boolean;
  reducedMotion?: boolean;
}

// Driver Settings Models
export interface DriverSettings {
  id: string;
  userId: string;
  // Availability Settings
  acceptingJobs: boolean;
  maxServiceRadiusMiles?: number;
  workingHours?: string;
  daysOff?: string;
  // Job Preferences
  minimumJobValue?: number;
  maximumJobDistanceMiles?: number;
  preferredJobTypes?: string;
  preferredVehicleTypes?: string;
  autoAcceptJobs: boolean;
  autoAcceptRadiusMiles?: number;
  // Payment Settings
  payoutFrequency?: string;
  bankAccountLast4?: string;
  stripeAccountId?: string;
  instantPayoutEnabled: boolean;
  minimumPayoutAmount?: number;
  // Notification Settings
  notifyOnNewJobsNearby: boolean;
  notifyOnJobRequests: boolean;
  notifyOnPayoutProcessed: boolean;
  notifyOnLowRating: boolean;
  // Vehicle Settings
  defaultVehicleId?: number;
  // Privacy Settings
  sharePerformanceMetrics: boolean;
  participateInLeaderboard: boolean;
  // Audit
  createdAt: Date;
  updatedAt: Date;
}

export interface UpdateDriverSettingsDto {
  // Availability Settings
  acceptingJobs?: boolean;
  maxServiceRadiusMiles?: number;
  workingHours?: string;
  daysOff?: string;
  // Job Preferences
  minimumJobValue?: number;
  maximumJobDistanceMiles?: number;
  preferredJobTypes?: string;
  preferredVehicleTypes?: string;
  autoAcceptJobs?: boolean;
  autoAcceptRadiusMiles?: number;
  // Payment Settings
  payoutFrequency?: string;
  bankAccountLast4?: string;
  stripeAccountId?: string;
  instantPayoutEnabled?: boolean;
  minimumPayoutAmount?: number;
  // Notification Settings
  notifyOnNewJobsNearby?: boolean;
  notifyOnJobRequests?: boolean;
  notifyOnPayoutProcessed?: boolean;
  notifyOnLowRating?: boolean;
  // Vehicle Settings
  defaultVehicleId?: number;
  // Privacy Settings
  sharePerformanceMetrics?: boolean;
  participateInLeaderboard?: boolean;
}

// Customer Settings Models
export interface CustomerSettings {
  id: string;
  userId: string;
  // Booking Preferences
  defaultVehicleType?: string;
  autoBookFavoriteDriver: boolean;
  allowAlternativeDrivers: boolean;
  preferredMaxDistance?: number;
  defaultPickupAddress?: string;
  defaultDeliveryAddress?: string;
  // Payment Settings
  defaultPaymentMethodId?: string;
  savePaymentMethods: boolean;
  autoTipEnabled: boolean;
  defaultTipPercentage?: number;
  requestReceiptByEmail: boolean;
  // Notification Settings
  notifyOnDriverAssigned: boolean;
  notifyOnDriverArriving: boolean;
  notifyOnJobStarted: boolean;
  notifyOnJobCompleted: boolean;
  notifyOnSpecialOffers: boolean;
  // Display Preferences
  showDriverRating: boolean;
  showPriceEstimate: boolean;
  showDriverLocation: boolean;
  enableJobTracking: boolean;
  // Accessibility Settings
  requireAccessibleVehicle: boolean;
  requireDriverAssistance: boolean;
  specialRequirements?: string;
  // Audit
  createdAt: Date;
  updatedAt: Date;
}

export interface UpdateCustomerSettingsDto {
  // Booking Preferences
  defaultVehicleType?: string;
  autoBookFavoriteDriver?: boolean;
  allowAlternativeDrivers?: boolean;
  preferredMaxDistance?: number;
  defaultPickupAddress?: string;
  defaultDeliveryAddress?: string;
  // Payment Settings
  defaultPaymentMethodId?: string;
  savePaymentMethods?: boolean;
  autoTipEnabled?: boolean;
  defaultTipPercentage?: number;
  requestReceiptByEmail?: boolean;
  // Notification Settings
  notifyOnDriverAssigned?: boolean;
  notifyOnDriverArriving?: boolean;
  notifyOnJobStarted?: boolean;
  notifyOnJobCompleted?: boolean;
  notifyOnSpecialOffers?: boolean;
  // Display Preferences
  showDriverRating?: boolean;
  showPriceEstimate?: boolean;
  showDriverLocation?: boolean;
  enableJobTracking?: boolean;
  // Accessibility Settings
  requireAccessibleVehicle?: boolean;
  requireDriverAssistance?: boolean;
  specialRequirements?: string;
}

// Platform Settings Models
export interface PlatformSettings {
  id: string;
  settingKey: string;
  settingName: string;
  settingValue?: string;
  valueType?: string;
  description?: string;
  category?: string;
  isPublic: boolean;
  isEditable: boolean;
  createdAt: Date;
  updatedAt: Date;
  updatedBy?: string;
}

export interface CreatePlatformSettingDto {
  settingKey: string;
  settingName: string;
  settingValue?: string;
  valueType?: string;
  description?: string;
  category?: string;
  isPublic: boolean;
  isEditable: boolean;
}

export interface UpdatePlatformSettingDto {
  settingValue: string;
}
