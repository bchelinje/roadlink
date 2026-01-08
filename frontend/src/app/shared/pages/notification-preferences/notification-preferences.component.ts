import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NotificationsService, NotificationPreferences } from '@core/api';

@Component({
  selector: 'app-notification-preferences',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container mx-auto px-4 py-8 max-w-4xl">
      <div class="mb-6">
        <h1 class="text-2xl font-bold text-gray-900">Notification Preferences</h1>
        <p class="mt-1 text-sm text-gray-600">Manage how you receive notifications</p>
      </div>

      <!-- Loading State -->
      <div *ngIf="isLoading" class="text-center py-12">
        <div class="inline-block w-12 h-12 border-4 border-gray-300 border-t-blue-600 rounded-full animate-spin"></div>
        <p class="mt-4 text-gray-600">Loading preferences...</p>
      </div>

      <!-- Success Message -->
      <div *ngIf="successMessage" class="mb-6 bg-green-50 border border-green-200 rounded-lg p-4">
        <p class="text-green-800">{{ successMessage }}</p>
      </div>

      <!-- Error Message -->
      <div *ngIf="errorMessage" class="mb-6 bg-red-50 border border-red-200 rounded-lg p-4">
        <p class="text-red-800">{{ errorMessage }}</p>
      </div>

      <!-- Preferences Form -->
      <div *ngIf="!isLoading && preferences" class="space-y-6">
        <!-- Notification Channels -->
        <div class="bg-white border border-gray-200 rounded-lg p-6">
          <h2 class="text-lg font-semibold text-gray-900 mb-4">Notification Channels</h2>
          <div class="space-y-4">
            <div class="flex items-center justify-between">
              <div>
                <h3 class="font-medium text-gray-900">Email Notifications</h3>
                <p class="text-sm text-gray-600">Receive notifications via email</p>
              </div>
              <label class="relative inline-flex items-center cursor-pointer">
                <input
                  type="checkbox"
                  [(ngModel)]="preferences.emailEnabled"
                  class="sr-only peer"
                />
                <div class="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
              </label>
            </div>

            <div class="flex items-center justify-between">
              <div>
                <h3 class="font-medium text-gray-900">SMS Notifications</h3>
                <p class="text-sm text-gray-600">Receive text messages for important updates</p>
              </div>
              <label class="relative inline-flex items-center cursor-pointer">
                <input
                  type="checkbox"
                  [(ngModel)]="preferences.smsEnabled"
                  class="sr-only peer"
                />
                <div class="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
              </label>
            </div>

            <div class="flex items-center justify-between">
              <div>
                <h3 class="font-medium text-gray-900">Push Notifications</h3>
                <p class="text-sm text-gray-600">Receive push notifications in your browser or mobile app</p>
              </div>
              <label class="relative inline-flex items-center cursor-pointer">
                <input
                  type="checkbox"
                  [(ngModel)]="preferences.pushEnabled"
                  class="sr-only peer"
                />
                <div class="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
              </label>
            </div>
          </div>
        </div>

        <!-- Job Notifications -->
        <div class="bg-white border border-gray-200 rounded-lg p-6">
          <h2 class="text-lg font-semibold text-gray-900 mb-4">Job Notifications</h2>
          <div class="space-y-3">
            <label class="flex items-center">
              <input type="checkbox" [(ngModel)]="preferences.jobAssignedEmail" class="mr-2">
              <span class="text-sm text-gray-700">Email me when jobs are assigned</span>
            </label>
            <label class="flex items-center">
              <input type="checkbox" [(ngModel)]="preferences.jobCompletedEmail" class="mr-2">
              <span class="text-sm text-gray-700">Email me when jobs are completed</span>
            </label>
            <label class="flex items-center">
              <input type="checkbox" [(ngModel)]="preferences.jobCancelledEmail" class="mr-2">
              <span class="text-sm text-gray-700">Email me when jobs are cancelled</span>
            </label>
          </div>
        </div>

        <!-- Payment Notifications -->
        <div class="bg-white border border-gray-200 rounded-lg p-6">
          <h2 class="text-lg font-semibold text-gray-900 mb-4">Payment Notifications</h2>
          <div class="space-y-3">
            <label class="flex items-center">
              <input type="checkbox" [(ngModel)]="preferences.paymentReceivedEmail" class="mr-2">
              <span class="text-sm text-gray-700">Email me when payments are received</span>
            </label>
            <label class="flex items-center">
              <input type="checkbox" [(ngModel)]="preferences.payoutProcessedEmail" class="mr-2">
              <span class="text-sm text-gray-700">Email me when payouts are processed</span>
            </label>
          </div>
        </div>

        <!-- Email Digest -->
        <div class="bg-white border border-gray-200 rounded-lg p-6">
          <h2 class="text-lg font-semibold text-gray-900 mb-4">Email Digest</h2>
          <div class="space-y-4">
            <label class="flex items-center">
              <input type="checkbox" [(ngModel)]="preferences.enableEmailDigest" class="mr-2">
              <span class="text-sm text-gray-700">Enable email digest</span>
            </label>
            <div *ngIf="preferences.enableEmailDigest">
              <label class="block text-sm font-medium text-gray-700 mb-2">
                How often would you like to receive email summaries?
              </label>
              <select
                [(ngModel)]="preferences.digestFrequency"
                class="w-full md:w-64 px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              >
                <option [ngValue]="null">Never</option>
                <option value="Daily">Daily</option>
                <option value="Weekly">Weekly</option>
                <option value="Monthly">Monthly</option>
              </select>
            </div>
          </div>
        </div>

        <!-- Quiet Hours -->
        <div class="bg-white border border-gray-200 rounded-lg p-6">
          <h2 class="text-lg font-semibold text-gray-900 mb-4">Quiet Hours</h2>
          <div class="flex items-center mb-4">
            <input
              type="checkbox"
              [(ngModel)]="preferences.enableQuietHours"
              id="quietHours"
              class="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
            />
            <label for="quietHours" class="ml-3 text-sm font-medium text-gray-900">
              Enable quiet hours (no non-urgent notifications)
            </label>
          </div>

          <div *ngIf="preferences.enableQuietHours" class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Start Time</label>
              <input
                type="time"
                [(ngModel)]="preferences.quietHoursStart"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">End Time</label>
              <input
                type="time"
                [(ngModel)]="preferences.quietHoursEnd"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              />
            </div>
          </div>
        </div>

        <!-- Save Button -->
        <div class="flex justify-end gap-3 pt-4">
          <button
            (click)="resetToDefaults()"
            class="px-6 py-2 text-gray-700 hover:text-gray-900 font-medium"
          >
            Reset to Defaults
          </button>
          <button
            (click)="savePreferences()"
            [disabled]="isSaving"
            class="px-6 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {{ isSaving ? 'Saving...' : 'Save Preferences' }}
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class NotificationPreferencesComponent implements OnInit {
  private notificationsService = inject(NotificationsService);

  preferences: NotificationPreferences | null = null;
  isLoading = false;
  isSaving = false;
  errorMessage = '';
  successMessage = '';

  ngOnInit(): void {
    this.loadPreferences();
  }

  loadPreferences(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.notificationsService.apiNotificationsPreferencesGet().subscribe({
      next: (preferences: NotificationPreferences) => {
        this.preferences = preferences || this.getDefaultPreferences();
        this.isLoading = false;
      },
      error: (error: any) => {
        console.error('Error loading preferences:', error);
        this.errorMessage = 'Failed to load preferences. Using defaults.';
        this.preferences = this.getDefaultPreferences();
        this.isLoading = false;
      }
    });
  }

  savePreferences(): void {
    if (!this.preferences) return;

    this.isSaving = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.notificationsService.apiNotificationsPreferencesPut(this.preferences).subscribe({
      next: () => {
        this.successMessage = 'Preferences saved successfully!';
        this.isSaving = false;
        setTimeout(() => {
          this.successMessage = '';
        }, 3000);
      },
      error: (error: any) => {
        console.error('Error saving preferences:', error);
        this.errorMessage = 'Failed to save preferences. Please try again.';
        this.isSaving = false;
      }
    });
  }

  resetToDefaults(): void {
    if (confirm('Reset all notification preferences to defaults?')) {
      this.preferences = this.getDefaultPreferences();
    }
  }

  private getDefaultPreferences(): NotificationPreferences {
    return {
      userId: '', // Will be set by the server
      emailEnabled: true,
      smsEnabled: false,
      pushEnabled: true,
      jobAssignedEmail: true,
      jobAssignedSms: false,
      jobAssignedPush: true,
      jobCompletedEmail: true,
      jobCompletedSms: false,
      jobCompletedPush: true,
      jobCancelledEmail: true,
      jobCancelledSms: false,
      jobCancelledPush: true,
      jobRescheduledEmail: true,
      jobRescheduledSms: false,
      jobRescheduledPush: false,
      paymentReceivedEmail: true,
      paymentReceivedSms: false,
      paymentReceivedPush: true,
      payoutProcessedEmail: true,
      payoutProcessedSms: false,
      payoutProcessedPush: true,
      refundProcessedEmail: true,
      refundProcessedSms: false,
      refundProcessedPush: false,
      reviewReceivedEmail: true,
      reviewReceivedSms: false,
      reviewReceivedPush: true,
      reviewResponseEmail: true,
      reviewResponseSms: false,
      reviewResponsePush: false,
      systemAlertsEmail: true,
      systemAlertsSms: true,
      systemAlertsPush: true,
      accountUpdatesEmail: true,
      accountUpdatesSms: false,
      accountUpdatesPush: true,
      promotionalEmail: false,
      promotionalSms: false,
      promotionalPush: false,
      enableQuietHours: false,
      quietHoursStart: '22:00',
      quietHoursEnd: '08:00',
      enableEmailDigest: false,
      digestFrequency: 'Weekly'
    };
  }
}
