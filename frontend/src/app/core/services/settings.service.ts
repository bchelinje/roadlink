import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import {
  UserSettings,
  UpdateUserSettingsDto,
  DriverSettings,
  UpdateDriverSettingsDto,
  CustomerSettings,
  UpdateCustomerSettingsDto,
  PlatformSettings,
  CreatePlatformSettingDto,
  UpdatePlatformSettingDto
} from '@core/models/settings.models';

@Injectable({
  providedIn: 'root'
})
export class SettingsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/api/settings`;

  // User Settings Endpoints
  getUserSettings(): Observable<UserSettings> {
    return this.http.get<UserSettings>(`${this.apiUrl}/user`);
  }

  updateUserSettings(dto: UpdateUserSettingsDto): Observable<UserSettings> {
    return this.http.put<UserSettings>(`${this.apiUrl}/user`, dto);
  }

  // Driver Settings Endpoints
  getDriverSettings(): Observable<DriverSettings> {
    return this.http.get<DriverSettings>(`${this.apiUrl}/driver`);
  }

  updateDriverSettings(dto: UpdateDriverSettingsDto): Observable<DriverSettings> {
    return this.http.put<DriverSettings>(`${this.apiUrl}/driver`, dto);
  }

  // Customer Settings Endpoints
  getCustomerSettings(): Observable<CustomerSettings> {
    return this.http.get<CustomerSettings>(`${this.apiUrl}/customer`);
  }

  updateCustomerSettings(dto: UpdateCustomerSettingsDto): Observable<CustomerSettings> {
    return this.http.put<CustomerSettings>(`${this.apiUrl}/customer`, dto);
  }

  // Platform Settings Endpoints (Admin Only)
  getPlatformSettings(category?: string, isPublic?: boolean): Observable<PlatformSettings[]> {
    let params = new HttpParams();

    if (category) {
      params = params.set('category', category);
    }

    if (isPublic !== undefined) {
      params = params.set('isPublic', isPublic.toString());
    }

    return this.http.get<PlatformSettings[]>(`${this.apiUrl}/platform`, { params });
  }

  getPublicPlatformSettings(): Observable<PlatformSettings[]> {
    return this.http.get<PlatformSettings[]>(`${this.apiUrl}/platform/public`);
  }

  getPlatformSettingByKey(key: string): Observable<PlatformSettings> {
    return this.http.get<PlatformSettings>(`${this.apiUrl}/platform/${key}`);
  }

  createPlatformSetting(dto: CreatePlatformSettingDto): Observable<PlatformSettings> {
    return this.http.post<PlatformSettings>(`${this.apiUrl}/platform`, dto);
  }

  updatePlatformSetting(key: string, dto: UpdatePlatformSettingDto): Observable<PlatformSettings> {
    return this.http.put<PlatformSettings>(`${this.apiUrl}/platform/${key}`, dto);
  }

  deletePlatformSetting(key: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/platform/${key}`);
  }
}
