export interface PromoCode {
  id: string;
  code: string;
  description?: string;
  discountType: DiscountType;
  discountValue: number;
  minimumOrderValue?: number;
  maximumDiscount?: number;
  validFrom: Date;
  validUntil: Date;
  usageLimit?: number;
  usageCount: number;
  perCustomerLimit?: number;
  allowedVehicleTypes?: string[];
  allowedCustomerTypes?: string[];
  firstTimeCustomersOnly: boolean;
  isActive: boolean;
  createdAt: Date;
  updatedAt: Date;
}

export interface CreatePromoCodeRequest {
  code: string;
  description?: string;
  discountType: DiscountType;
  discountValue: number;
  minimumOrderValue?: number;
  maximumDiscount?: number;
  validFrom: Date;
  validUntil: Date;
  usageLimit?: number;
  perCustomerLimit?: number;
  allowedVehicleTypes?: string[];
  allowedCustomerTypes?: string[];
  firstTimeCustomersOnly?: boolean;
  isActive?: boolean;
}

export interface UpdatePromoCodeRequest {
  description?: string;
  discountValue?: number;
  minimumOrderValue?: number;
  maximumDiscount?: number;
  validFrom?: Date;
  validUntil?: Date;
  usageLimit?: number;
  perCustomerLimit?: number;
  allowedVehicleTypes?: string[];
  allowedCustomerTypes?: string[];
  firstTimeCustomersOnly?: boolean;
  isActive?: boolean;
}

export interface ValidatePromoCodeRequest {
  code: string;
  jobValue: number;
  vehicleType?: string;
}

export interface ValidatePromoCodeResponse {
  isValid: boolean;
  message?: string;
  discountAmount?: number;
  promoCode?: PromoCode;
}

export interface ApplyPromoCodeRequest {
  code: string;
  jobId: string;
}

export enum DiscountType {
  Percentage = 'percentage',
  FixedAmount = 'fixed_amount'
}

export interface PromoCodeFilter {
  isActive?: boolean;
  validNow?: boolean;
  pageNumber?: number;
  pageSize?: number;
}
