import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { NotificationsService, Notification, UnreadCountDto } from '@core/api';
import { Subject, interval } from 'rxjs';
import { takeUntil, switchMap, startWith } from 'rxjs/operators';

@Component({
  selector: 'app-notifications-bell',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="relative">
      <button
        type="button"
        (click)="toggleDropdown()"
        class="relative p-2 text-gray-600 hover:text-gray-900 hover:bg-gray-100 rounded-full transition-colors"
        [class.bg-gray-100]="isOpen"
      >
        <!-- Bell Icon -->
        <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
        </svg>

        <!-- Unread Badge -->
        <span
          *ngIf="unreadCount > 0"
          class="absolute top-0 right-0 inline-flex items-center justify-center w-5 h-5 text-xs font-bold text-white bg-red-500 rounded-full transform translate-x-1 -translate-y-1"
        >
          {{ unreadCount > 99 ? '99+' : unreadCount }}
        </span>
      </button>

      <!-- Dropdown -->
      <div
        *ngIf="isOpen"
        class="absolute right-0 mt-2 w-96 bg-white rounded-lg shadow-lg border border-gray-200 z-50 max-h-[32rem] overflow-hidden flex flex-col"
      >
        <!-- Header -->
        <div class="flex items-center justify-between p-4 border-b border-gray-200">
          <h3 class="text-lg font-semibold text-gray-900">Notifications</h3>
          <div class="flex items-center gap-2">
            <button
              *ngIf="unreadCount > 0"
              (click)="markAllAsRead()"
              class="text-sm text-blue-600 hover:text-blue-700 font-medium"
            >
              Mark all read
            </button>
            <a
              routerLink="/notifications"
              (click)="closeDropdown()"
              class="text-sm text-gray-600 hover:text-gray-900"
            >
              View all
            </a>
          </div>
        </div>

        <!-- Notifications List -->
        <div class="overflow-y-auto flex-1" style="max-height: 400px;">
          <div *ngIf="isLoading" class="p-8 text-center">
            <div class="inline-block w-8 h-8 border-4 border-gray-300 border-t-blue-500 rounded-full animate-spin"></div>
            <p class="mt-2 text-sm text-gray-600">Loading notifications...</p>
          </div>

          <div *ngIf="!isLoading && notifications.length === 0" class="p-8 text-center">
            <svg class="w-16 h-16 mx-auto text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
            </svg>
            <p class="mt-4 text-sm text-gray-600">No notifications</p>
          </div>

          <div *ngIf="!isLoading && notifications.length > 0">
            <div
              *ngFor="let notification of notifications"
              (click)="handleNotificationClick(notification)"
              class="p-4 border-b border-gray-100 hover:bg-gray-50 cursor-pointer transition-colors"
              [class.bg-blue-50]="!notification.isRead"
            >
              <div class="flex items-start gap-3">
                <!-- Icon -->
                <div
                  class="flex-shrink-0 w-10 h-10 rounded-full flex items-center justify-center"
                  [ngClass]="{
                    'bg-blue-100 text-blue-600': notification.type === 'Job',
                    'bg-green-100 text-green-600': notification.type === 'Payment',
                    'bg-yellow-100 text-yellow-600': notification.type === 'Review',
                    'bg-purple-100 text-purple-600': notification.type === 'System',
                    'bg-gray-100 text-gray-600': notification.type === 'Promotional'
                  }"
                >
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path *ngIf="notification.type === 'Job'" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
                    <path *ngIf="notification.type === 'Payment'" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                    <path *ngIf="notification.type === 'Review'" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z" />
                    <path *ngIf="notification.type === 'System' || notification.type === 'Promotional'" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                </div>

                <!-- Content -->
                <div class="flex-1 min-w-0">
                  <p class="text-sm font-medium text-gray-900" [class.font-semibold]="!notification.isRead">
                    {{ notification.title }}
                  </p>
                  <p class="mt-1 text-sm text-gray-600 line-clamp-2">
                    {{ notification.message }}
                  </p>
                  <p class="mt-1 text-xs text-gray-500">
                    {{ getTimeAgo(notification.createdAt) }}
                  </p>
                </div>

                <!-- Unread Indicator -->
                <div *ngIf="!notification.isRead" class="flex-shrink-0 w-2 h-2 bg-blue-500 rounded-full"></div>
              </div>
            </div>
          </div>
        </div>

        <!-- Footer -->
        <div class="p-3 border-t border-gray-200 bg-gray-50">
          <a
            routerLink="/notifications/preferences"
            (click)="closeDropdown()"
            class="block w-full text-center text-sm text-gray-600 hover:text-gray-900 font-medium"
          >
            Notification Settings
          </a>
        </div>
      </div>
    </div>

    <!-- Backdrop -->
    <div
      *ngIf="isOpen"
      (click)="closeDropdown()"
      class="fixed inset-0 z-40"
    ></div>
  `,
  styles: [`
    :host {
      display: block;
    }

    .line-clamp-2 {
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }
  `]
})
export class NotificationsBellComponent implements OnInit, OnDestroy {
  private notificationsService = inject(NotificationsService);
  private destroy$ = new Subject<void>();

  notifications: Notification[] = [];
  unreadCount = 0;
  isOpen = false;
  isLoading = false;

  ngOnInit(): void {
    this.loadNotifications();
    this.startPolling();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  toggleDropdown(): void {
    this.isOpen = !this.isOpen;
    if (this.isOpen) {
      this.loadNotifications();
    }
  }

  closeDropdown(): void {
    this.isOpen = false;
  }

  private startPolling(): void {
    // Poll for new notifications every 30 seconds
    interval(30000)
      .pipe(
        startWith(0),
        switchMap(() => this.notificationsService.apiNotificationsMeUnreadCountGet()),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: (count: UnreadCountDto) => {
          this.unreadCount = count.count || 0;
        },
        error: (error) => {
          console.error('Error fetching unread count:', error);
        }
      });
  }

  private loadNotifications(): void {
    this.isLoading = true;
    this.notificationsService.apiNotificationsMeGet(undefined, undefined, 1, 10)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (notifications) => {
          this.notifications = (notifications as any) || [];
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Error loading notifications:', error);
          this.isLoading = false;
        }
      });
  }

  handleNotificationClick(notification: Notification): void {
    if (!notification.isRead) {
      this.markAsRead(notification.id!);
    }

    // Navigate based on notification type and data
    // You can implement routing logic here based on notification.data
    this.closeDropdown();
  }

  markAsRead(notificationId: string): void {
    this.notificationsService.apiNotificationsIdReadPatch(notificationId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          const notification = this.notifications.find(n => n.id === notificationId);
          if (notification) {
            notification.isRead = true;
          }
          this.unreadCount = Math.max(0, this.unreadCount - 1);
        },
        error: (error) => {
          console.error('Error marking notification as read:', error);
        }
      });
  }

  markAllAsRead(): void {
    this.notificationsService.apiNotificationsReadAllPatch()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.notifications.forEach(n => n.isRead = true);
          this.unreadCount = 0;
        },
        error: (error) => {
          console.error('Error marking all as read:', error);
        }
      });
  }

  getTimeAgo(date: string | undefined): string {
    if (!date) return '';

    const now = new Date();
    const notificationDate = new Date(date);
    const diffInSeconds = Math.floor((now.getTime() - notificationDate.getTime()) / 1000);

    if (diffInSeconds < 60) return 'Just now';
    if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m ago`;
    if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h ago`;
    if (diffInSeconds < 604800) return `${Math.floor(diffInSeconds / 86400)}d ago`;
    return notificationDate.toLocaleDateString();
  }
}
