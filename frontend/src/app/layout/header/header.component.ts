
// shared/components/page-header/page-header.component.ts
import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

export interface Breadcrumb {
  label: string;
  route?: string;
  icon?: string;
}

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="bg-white border-b border-gray-200 sticky top-0 z-30">
      <div class="px-4 sm:px-6 lg:px-8">
        <!-- Breadcrumbs -->
        <nav *ngIf="breadcrumbs && breadcrumbs.length > 0" class="flex py-3" aria-label="Breadcrumb">
          <ol class="flex items-center space-x-2">
            <li *ngFor="let crumb of breadcrumbs; let isLast = last" class="flex items-center">
              <a
                *ngIf="crumb.route && !isLast"
                [routerLink]="crumb.route"
                class="text-sm font-medium text-gray-500 hover:text-gray-700 transition-colors flex items-center gap-1.5"
              >
                <svg *ngIf="crumb.icon" class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" [attr.d]="getIconPath(crumb.icon)" />
                </svg>
                {{ crumb.label }}
              </a>
              <span
                *ngIf="isLast || !crumb.route"
                class="text-sm font-medium text-gray-900 flex items-center gap-1.5"
              >
                <svg *ngIf="crumb.icon" class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" [attr.d]="getIconPath(crumb.icon)" />
                </svg>
                {{ crumb.label }}
              </span>
              <svg
                *ngIf="!isLast"
                class="h-5 w-5 text-gray-400 mx-2"
                fill="currentColor"
                viewBox="0 0 20 20"
              >
                <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd" />
              </svg>
            </li>
          </ol>
        </nav>

        <!-- Page Header -->
        <div class="py-6">
          <div class="flex items-center justify-between">
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-3">
                <!-- Back Button (Optional) -->
                <button
                  *ngIf="showBackButton"
                  (click)="goBack()"
                  class="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-lg transition-colors"
                  title="Go back"
                >
                  <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
                  </svg>
                </button>

                <!-- Icon (Optional) -->
                <div *ngIf="icon" class="flex-shrink-0">
                  <div class="h-12 w-12 bg-gradient-to-br rounded-xl flex items-center justify-center"
                       [ngClass]="iconColorClass">
                    <svg class="h-6 w-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" [attr.d]="getIconPath(icon)" />
                    </svg>
                  </div>
                </div>

                <!-- Title & Description -->
                <div>
                  <h1 class="text-2xl font-bold text-gray-900">{{ title }}</h1>
                  <p *ngIf="description" class="mt-1 text-sm text-gray-600">{{ description }}</p>
                </div>
              </div>
            </div>

            <!-- Action Buttons -->
            <div *ngIf="actionButtons" class="flex items-center gap-3">
              <ng-content></ng-content>
            </div>
          </div>
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
export class HeaderComponent {
  @Input() title = '';
  @Input() description = '';
  @Input() icon?: string;
  @Input() iconColorClass = 'from-blue-500 to-blue-600';
  @Input() breadcrumbs?: Breadcrumb[];
  @Input() showBackButton = false;
  @Input() actionButtons = false;

  goBack(): void {
    window.history.back();
  }

  getIconPath(iconName: string): string {
    const icons: Record<string, string> = {
      'home': 'M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6',
      'users': 'M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z',
      'shield': 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z',
      'user-plus': 'M18 9v3m0 0v3m0-3h3m-3 0h-3m-2-5a4 4 0 11-8 0 4 4 0 018 0zM3 20a6 6 0 0112 0v1H3v-1z',
      'edit': 'M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z',
      'eye': 'M15 12a3 3 0 11-6 0 3 3 0 016 0z M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z'
    };
    return icons[iconName] || icons['home'];
  }
}
