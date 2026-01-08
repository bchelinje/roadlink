import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SettingsService } from '@core/services/settings.service';
import { ToastService } from '@core/services/toast.service';
import { DriverSettings, UpdateDriverSettingsDto } from '@core/models/settings.models';

@Component({
  selector: 'app-driver-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './driver-settings.component.html',
  styleUrls: ['./driver-settings.component.scss']
})
export class DriverSettingsComponent implements OnInit {
  private readonly settingsService = inject(SettingsService);
  private readonly toastService = inject(ToastService);

  settings: DriverSettings | null = null;
  loading = false;
  saving = false;
  activeTab: 'availability' | 'jobs' | 'payment' | 'notifications' | 'privacy' = 'availability';

  // Form models
  form: UpdateDriverSettingsDto = {};

  ngOnInit(): void {
    this.loadSettings();
  }

  loadSettings(): void {
    this.loading = true;
    this.settingsService.getDriverSettings().subscribe({
      next: (data) => {
        this.settings = data;
        this.initializeForm();
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading driver settings:', error);
        this.toastService.error('Error', 'Failed to load settings');
        this.loading = false;
      }
    });
  }

  initializeForm(): void {
    if (!this.settings) return;

    this.form = {
      acceptingJobs: this.settings.acceptingJobs,
      maxServiceRadiusMiles: this.settings.maxServiceRadiusMiles,
      workingHours: this.settings.workingHours,
      daysOff: this.settings.daysOff,
      minimumJobValue: this.settings.minimumJobValue,
      maximumJobDistanceMiles: this.settings.maximumJobDistanceMiles,
      preferredJobTypes: this.settings.preferredJobTypes,
      preferredVehicleTypes: this.settings.preferredVehicleTypes,
      autoAcceptJobs: this.settings.autoAcceptJobs,
      autoAcceptRadiusMiles: this.settings.autoAcceptRadiusMiles,
      payoutFrequency: this.settings.payoutFrequency,
      bankAccountLast4: this.settings.bankAccountLast4,
      stripeAccountId: this.settings.stripeAccountId,
      instantPayoutEnabled: this.settings.instantPayoutEnabled,
      minimumPayoutAmount: this.settings.minimumPayoutAmount,
      notifyOnNewJobsNearby: this.settings.notifyOnNewJobsNearby,
      notifyOnJobRequests: this.settings.notifyOnJobRequests,
      notifyOnPayoutProcessed: this.settings.notifyOnPayoutProcessed,
      notifyOnLowRating: this.settings.notifyOnLowRating,
      defaultVehicleId: this.settings.defaultVehicleId,
      sharePerformanceMetrics: this.settings.sharePerformanceMetrics,
      participateInLeaderboard: this.settings.participateInLeaderboard
    };
  }

  saveSettings(): void {
    this.saving = true;
    this.settingsService.updateDriverSettings(this.form).subscribe({
      next: (data) => {
        this.settings = data;
        this.initializeForm();
        this.toastService.success('Success', 'Settings saved successfully');
        this.saving = false;
      },
      error: (error) => {
        console.error('Error saving settings:', error);
        this.toastService.error('Error', 'Failed to save settings');
        this.saving = false;
      }
    });
  }

  setTab(tab: 'availability' | 'jobs' | 'payment' | 'notifications' | 'privacy'): void {
    this.activeTab = tab;
  }
}
