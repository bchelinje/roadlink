import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { TokenService } from '@core/services/token.service';
import { environment } from '@environments/environment';

interface UserProfile {
  id: string;
  email: string;
  userName: string;
  emailConfirmed: boolean;
  phoneNumber?: string;
  phoneNumberConfirmed: boolean;
  roles: string[];
  twoFactorEnabled: boolean;
}

@Component({
  selector: 'app-customer-profile',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss']
})
export class CustomerProfileComponent implements OnInit {
  private http = inject(HttpClient);
  private tokenService = inject(TokenService);

  profile: UserProfile | null = null;
  isLoading = true;
  errorMessage = '';

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.isLoading = true;
    this.errorMessage = '';

    const token = this.tokenService.getAccessToken();
    if (!token) {
      this.errorMessage = 'Not authenticated';
      this.isLoading = false;
      return;
    }

    const headers = new HttpHeaders({
      'Authorization': `Bearer ${token}`
    });

    this.http.get<UserProfile>(`${environment.apiBaseUrl}/api/Users/me`, { headers }).subscribe({
      next: (profile) => {
        this.profile = profile;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading profile:', error);
        this.errorMessage = 'Failed to load profile information';
        this.isLoading = false;
      }
    });
  }
}
