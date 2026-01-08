import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, Subject, debounceTime, switchMap, distinctUntilChanged } from 'rxjs';
import { environment } from '@environments/environment';

export interface DistanceResult {
  distanceInMeters: number;
  distanceInMiles: number;
  distanceInKilometers: number;
  durationInSeconds: number;
  durationInMinutes: number;
  distanceText: string;
  durationText: string;
}

export interface GeocodeResult {
  latitude: number;
  longitude: number;
  formattedAddress: string;
  placeId?: string;
}

@Injectable({
  providedIn: 'root'
})
export class MapsService {
  private http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/api/Maps`;

  // Subject for autocomplete debouncing
  private autocompleteSubject = new Subject<{ input: string; callback: (suggestions: string[]) => void }>();

  constructor() {
    // Set up autocomplete with debouncing
    this.autocompleteSubject
      .pipe(
        debounceTime(300),
        distinctUntilChanged((prev, curr) => prev.input === curr.input),
        switchMap(({ input }) => this.autocompleteAddressInternal(input))
      )
      .subscribe(({ suggestions, callback }) => {
        callback(suggestions);
      });
  }

  /**
   * Get address autocomplete suggestions with debouncing
   */
  autocompleteAddress(input: string, callback: (suggestions: string[]) => void): void {
    if (!input || input.trim().length < 3) {
      callback([]);
      return;
    }
    this.autocompleteSubject.next({ input, callback });
  }

  /**
   * Internal method for autocomplete API call
   */
  private autocompleteAddressInternal(input: string): Observable<{ suggestions: string[]; callback: any }> {
    const params = new HttpParams().set('input', input);

    return new Observable(observer => {
      this.http.get<string[]>(`${this.apiUrl}/autocomplete`, { params }).subscribe({
        next: (suggestions) => {
          // Store callback for later invocation
          observer.next({ suggestions, callback: () => {} });
          observer.complete();
        },
        error: (error) => {
          console.error('Autocomplete error:', error);
          observer.next({ suggestions: [], callback: () => {} });
          observer.complete();
        }
      });
    });
  }

  /**
   * Calculate distance between two addresses
   */
  calculateDistance(origin: string, destination: string): Observable<DistanceResult> {
    const params = new HttpParams()
      .set('origin', origin)
      .set('destination', destination);

    return this.http.get<DistanceResult>(`${this.apiUrl}/distance`, { params });
  }

  /**
   * Geocode an address to get coordinates
   */
  geocodeAddress(address: string): Observable<GeocodeResult> {
    const params = new HttpParams().set('address', address);
    return this.http.get<GeocodeResult>(`${this.apiUrl}/geocode`, { params });
  }

  /**
   * Reverse geocode coordinates to get address
   */
  reverseGeocode(latitude: number, longitude: number): Observable<{ address: string }> {
    const params = new HttpParams()
      .set('latitude', latitude.toString())
      .set('longitude', longitude.toString());

    return this.http.get<{ address: string }>(`${this.apiUrl}/reverse-geocode`, { params });
  }
}
