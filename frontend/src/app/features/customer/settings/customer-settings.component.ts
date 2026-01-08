import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SettingsService } from '@core/services/settings.service';
import { ToastService } from '@core/services/toast.service';
import { CustomerSettings, UpdateCustomerSettingsDto } from '@core/models/settings.models';

@Component({
  selector: 'app-customer-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './customer-settings.component.html',
  styleUrls: ['./customer-settings.component.scss']
})
export class CustomerSettingsComponent implements OnInit {
  private readonly settingsService = inject(SettingsService);
  private readonly toastService = inject(ToastService);

  settings: CustomerSettings | null = null;
  loading = false;
  saving = false;
  activeTab: 'booking' | 'payment' | 'notifications' | 'display' | 'accessibility' = 'booking';

  // Form models
  form: UpdateCustomerSettingsDto = {};

  ngOnInit(): void {
    this.loadSettings();
  }

  loadSettings(): void {
    this.loading = true;
    this.settingsService.getCustomerSettings().subscribe({
      next: (data) => {
        this.settings = data;
        this.initializeForm();
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading customer settings:', error);
        this.toastService.error('Error', 'Failed to load settings');
        this.loading = false;
      }
    });
  }

  initializeForm(): void {
    if (!this.settings) return;

    this.form = {
      defaultVehicleType: this.settings.defaultVehicleType,
      autoBookFavoriteDriver: this.settings.autoBookFavoriteDriver,
      allowAlternativeDrivers: this.settings.allowAlternativeDrivers,
      preferredMaxDistance: this.settings.preferredMaxDistance,
      defaultPickupAddress: this.settings.defaultPickupAddress,
      defaultDeliveryAddress: this.settings.defaultDeliveryAddress,
      defaultPaymentMethodId: this.settings.defaultPaymentMethodId,
      savePaymentMethods: this.settings.savePaymentMethods,
      autoTipEnabled: this.settings.autoTipEnabled,
      defaultTipPercentage: this.settings.defaultTipPercentage,
      requestReceiptByEmail: this.settings.requestReceiptByEmail,
      notifyOnDriverAssigned: this.settings.notifyOnDriverAssigned,
      notifyOnDriverArriving: this.settings.notifyOnDriverArriving,
      notifyOnJobStarted: this.settings.notifyOnJobStarted,
      notifyOnJobCompleted: this.settings.notifyOnJobCompleted,
      notifyOnSpecialOffers: this.settings.notifyOnSpecialOffers,
      showDriverRating: this.settings.showDriverRating,
      showPriceEstimate: this.settings.showPriceEstimate,
      showDriverLocation: this.settings.showDriverLocation,
      enableJobTracking: this.settings.enableJobTracking,
      requireAccessibleVehicle: this.settings.requireAccessibleVehicle,
      requireDriverAssistance: this.settings.requireDriverAssistance,
      specialRequirements: this.settings.specialRequirements
    };
  }

  saveSettings(): void {
    this.saving = true;
    this.settingsService.updateCustomerSettings(this.form).subscribe({
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

  setTab(tab: 'booking' | 'payment' | 'notifications' | 'display' | 'accessibility'): void {
    this.activeTab = tab;
  }
}
