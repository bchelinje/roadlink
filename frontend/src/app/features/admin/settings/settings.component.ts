import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SettingsService } from '@core/services/settings.service';
import { ToastService } from '@core/services/toast.service';
import { PlatformSettings, CreatePlatformSettingDto, UpdatePlatformSettingDto } from '@core/models/settings.models';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss']
})
export class SettingsComponent implements OnInit {
  private readonly settingsService = inject(SettingsService);
  private readonly toastService = inject(ToastService);

  settings: PlatformSettings[] = [];
  filteredSettings: PlatformSettings[] = [];
  loading = false;
  selectedCategory = 'all';
  searchTerm = '';

  // Modal state
  showCreateModal = false;
  showEditModal = false;
  showDeleteModal = false;
  selectedSetting: PlatformSettings | null = null;

  // Form data
  createForm: CreatePlatformSettingDto = {
    settingKey: '',
    settingName: '',
    settingValue: '',
    valueType: 'string',
    description: '',
    category: 'general',
    isPublic: false,
    isEditable: true
  };

  editValue = '';

  // Categories
  categories = [
    { value: 'all', label: 'All Categories' },
    { value: 'general', label: 'General' },
    { value: 'payment', label: 'Payment' },
    { value: 'email', label: 'Email' },
    { value: 'maps', label: 'Maps' },
    { value: 'notifications', label: 'Notifications' },
    { value: 'security', label: 'Security' }
  ];

  valueTypes = ['string', 'number', 'boolean', 'json'];

  ngOnInit(): void {
    this.loadSettings();
  }

  loadSettings(): void {
    this.loading = true;
    this.settingsService.getPlatformSettings().subscribe({
      next: (data) => {
        this.settings = data;
        this.applyFilters();
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading settings:', error);
        this.toastService.error('Error', 'Failed to load platform settings');
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    this.filteredSettings = this.settings.filter(setting => {
      const matchesCategory = this.selectedCategory === 'all' || setting.category === this.selectedCategory;
      const matchesSearch = !this.searchTerm ||
        setting.settingName.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        setting.settingKey.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        (setting.description && setting.description.toLowerCase().includes(this.searchTerm.toLowerCase()));

      return matchesCategory && matchesSearch;
    });
  }

  onCategoryChange(): void {
    this.applyFilters();
  }

  onSearchChange(): void {
    this.applyFilters();
  }

  openCreateModal(): void {
    this.createForm = {
      settingKey: '',
      settingName: '',
      settingValue: '',
      valueType: 'string',
      description: '',
      category: 'general',
      isPublic: false,
      isEditable: true
    };
    this.showCreateModal = true;
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
  }

  createSetting(): void {
    this.loading = true;
    this.settingsService.createPlatformSetting(this.createForm).subscribe({
      next: () => {
        this.toastService.success('Success', 'Platform setting created successfully');
        this.closeCreateModal();
        this.loadSettings();
      },
      error: (error) => {
        console.error('Error creating setting:', error);
        this.toastService.error('Error', error.error?.message || 'Failed to create platform setting');
        this.loading = false;
      }
    });
  }

  openEditModal(setting: PlatformSettings): void {
    if (!setting.isEditable) {
      this.toastService.warning('Warning', 'This setting is not editable');
      return;
    }
    this.selectedSetting = setting;
    this.editValue = setting.settingValue || '';
    this.showEditModal = true;
  }

  closeEditModal(): void {
    this.showEditModal = false;
    this.selectedSetting = null;
    this.editValue = '';
  }

  updateSetting(): void {
    if (!this.selectedSetting) return;

    this.loading = true;
    const dto: UpdatePlatformSettingDto = {
      settingValue: this.editValue
    };

    this.settingsService.updatePlatformSetting(this.selectedSetting.settingKey, dto).subscribe({
      next: () => {
        this.toastService.success('Success', 'Platform setting updated successfully');
        this.closeEditModal();
        this.loadSettings();
      },
      error: (error) => {
        console.error('Error updating setting:', error);
        this.toastService.error('Error', error.error?.message || 'Failed to update platform setting');
        this.loading = false;
      }
    });
  }

  openDeleteModal(setting: PlatformSettings): void {
    this.selectedSetting = setting;
    this.showDeleteModal = true;
  }

  closeDeleteModal(): void {
    this.showDeleteModal = false;
    this.selectedSetting = null;
  }

  deleteSetting(): void {
    if (!this.selectedSetting) return;

    this.loading = true;
    this.settingsService.deletePlatformSetting(this.selectedSetting.settingKey).subscribe({
      next: () => {
        this.toastService.success('Success', 'Platform setting deleted successfully');
        this.closeDeleteModal();
        this.loadSettings();
      },
      error: (error) => {
        console.error('Error deleting setting:', error);
        this.toastService.error('Error', error.error?.message || 'Failed to delete platform setting');
        this.loading = false;
      }
    });
  }

  getSettingsByCategory(): { [key: string]: PlatformSettings[] } {
    return this.filteredSettings.reduce((acc, setting) => {
      const category = setting.category || 'general';
      if (!acc[category]) {
        acc[category] = [];
      }
      acc[category].push(setting);
      return acc;
    }, {} as { [key: string]: PlatformSettings[] });
  }

  getCategoryKeys(): string[] {
    return Object.keys(this.getSettingsByCategory()).sort();
  }
}
