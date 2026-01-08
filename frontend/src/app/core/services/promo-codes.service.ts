import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import {
  PromoCode,
  CreatePromoCodeRequest,
  UpdatePromoCodeRequest,
  ValidatePromoCodeRequest,
  ValidatePromoCodeResponse,
  ApplyPromoCodeRequest,
  PromoCodeFilter
} from '@core/models/promo-code.models';

@Injectable({
  providedIn: 'root'
})
export class PromoCodesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/promotioncodes`;

  /**
   * Validate a promo code (Customer)
   */
  validatePromoCode(request: ValidatePromoCodeRequest): Observable<ValidatePromoCodeResponse> {
    return this.http.post<ValidatePromoCodeResponse>(`${this.baseUrl}/validate`, request);
  }

  /**
   * Apply promo code to a job (Customer)
   */
  applyPromoCode(request: ApplyPromoCodeRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/apply`, request);
  }

  /**
   * Get all promo codes (Admin only)
   */
  getPromoCodes(filter?: PromoCodeFilter): Observable<PromoCode[]> {
    let params = new HttpParams();
    if (filter?.isActive !== undefined) params = params.set('isActive', filter.isActive.toString());
    if (filter?.validNow !== undefined) params = params.set('validNow', filter.validNow.toString());
    if (filter?.pageNumber) params = params.set('pageNumber', filter.pageNumber.toString());
    if (filter?.pageSize) params = params.set('pageSize', filter.pageSize.toString());

    return this.http.get<PromoCode[]>(this.baseUrl, { params });
  }

  /**
   * Get a specific promo code (Admin only)
   */
  getPromoCode(promoCodeId: string): Observable<PromoCode> {
    return this.http.get<PromoCode>(`${this.baseUrl}/${promoCodeId}`);
  }

  /**
   * Create promo code (Admin only)
   */
  createPromoCode(request: CreatePromoCodeRequest): Observable<PromoCode> {
    return this.http.post<PromoCode>(this.baseUrl, request);
  }

  /**
   * Update promo code (Admin only)
   */
  updatePromoCode(promoCodeId: string, request: UpdatePromoCodeRequest): Observable<PromoCode> {
    return this.http.put<PromoCode>(`${this.baseUrl}/${promoCodeId}`, request);
  }

  /**
   * Delete promo code (Admin only)
   */
  deletePromoCode(promoCodeId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${promoCodeId}`);
  }

  /**
   * Deactivate promo code (Admin only)
   */
  deactivatePromoCode(promoCodeId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${promoCodeId}/deactivate`, {});
  }
}
