import { Routes } from '@angular/router';
import { authGuard, guestGuard, roleGuard } from '@core/guards/auth.guard';
import {ActivityLogsComponent} from '@features/admin/activity-logs/activity-logs/activity-logs.component';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/public/landing/landing.component').then(m => m.LandingComponent)
  },
  // Public Routes (no authentication required)
  {
    path: 'book',
    loadComponent: () => import('./features/customer/request-job/request-job.component').then(m => m.RequestJobComponent)
  },
  {
    path: 'login',
    canActivate: [guestGuard], // Prevent authenticated users from accessing login
    loadComponent: () => import('./features/admin/auth/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'forgot-password',
    loadComponent: () => import('./features/admin/auth/forgot-password/forgot-password.component')
      .then(m => m.ForgotPasswordComponent)
  },
  {
    path: 'reset-password',
    loadComponent: () => import('./features/admin/auth/reset-password/reset-password.component')
      .then(m => m.ResetPasswordComponent)
  },
  {
    path: 'verify-email',
    loadComponent: () => import('./features/admin/auth/verify-email/verify-email.component')
      .then(m => m.VerifyEmailComponent)
  },
  {
    path: 'unauthorized',
    loadComponent: () => import('./features/admin/auth/unauthorized/unauthorized.component').then(m => m.UnauthorizedComponent)
  },
  // Public Registration Routes
  {
    path: 'register-customer',
    canActivate: [guestGuard], // Prevent authenticated users from accessing registration
    loadComponent: () => import('./features/public/register-customer/register-customer.component')
      .then(m => m.RegisterCustomerComponent)
  },
  {
    path: 'register-driver',
    canActivate: [guestGuard], // Prevent authenticated users from accessing registration
    loadComponent: () => import('./features/public/register-driver/register-driver.component')
      .then(m => m.RegisterDriverComponent)
  },
  // Public Help Center
  {
    path: 'help-center',
    loadComponent: () => import('./features/public/help-center/help-center.component').then(m => m.HelpCenterComponent)
  },
  // Customer Portal - wrapped in customer layout
  {
    path: 'customer',
    loadComponent: () => import('./layout/customer-layout/customer-layout.component').then(m => m.CustomerLayoutComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Customer'] },
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/customer/dashboard/dashboard.component').then(m => m.CustomerDashboardComponent)
      },
      {
        path: 'my-jobs',
        loadComponent: () => import('./features/customer/my-jobs/my-jobs.component').then(m => m.MyJobsComponent)
      },
      {
        path: 'jobs/:id',
        loadComponent: () => import('./features/customer/job-detail/job-detail.component').then(m => m.JobDetailComponent)
      },
      {
        path: 'jobs/:jobId/review',
        loadComponent: () => import('./features/customer/submit-review/submit-review.component').then(m => m.SubmitReviewComponent)
      },
      {
        path: 'request-job',
        loadComponent: () => import('./features/customer/request-job/request-job.component').then(m => m.RequestJobComponent)
      },
      {
        path: 'book-job',
        loadComponent: () => import('./features/customer/book-job/book-job.component').then(m => m.BookJobComponent)
      },
      {
        path: 'profile',
        loadComponent: () => import('./features/customer/profile/profile.component').then(m => m.CustomerProfileComponent)
      },
      {
        path: 'addresses',
        loadComponent: () => import('./features/customer/addresses/addresses-list.component').then(m => m.AddressesListComponent)
      },
      {
        path: 'payments',
        loadComponent: () => import('./features/customer/payments/payment-history.component').then(m => m.PaymentHistoryComponent)
      },
      {
        path: 'favorites',
        loadComponent: () => import('./features/customer/favorites/favorite-drivers.component').then(m => m.FavoriteDriversComponent)
      },
      {
        path: 'job-templates',
        loadComponent: () => import('./features/customer/job-templates/job-templates-list.component').then(m => m.JobTemplatesListComponent)
      },
      {
        path: 'job-templates/create',
        loadComponent: () => import('./features/customer/job-templates/job-template-form.component').then(m => m.JobTemplateFormComponent)
      },
      {
        path: 'job-templates/edit/:id',
        loadComponent: () => import('./features/customer/job-templates/job-template-form.component').then(m => m.JobTemplateFormComponent)
      },
      {
        path: 'recurring-jobs',
        loadComponent: () => import('./features/customer/recurring-jobs/recurring-jobs-list.component').then(m => m.RecurringJobsListComponent)
      },
      {
        path: 'recurring-jobs/create',
        loadComponent: () => import('./features/customer/recurring-jobs/recurring-job-form.component').then(m => m.RecurringJobFormComponent)
      },
      {
        path: 'recurring-jobs/edit/:id',
        loadComponent: () => import('./features/customer/recurring-jobs/recurring-job-form.component').then(m => m.RecurringJobFormComponent)
      },
      {
        path: 'my-reviews',
        loadComponent: () => import('./features/customer/my-reviews/my-reviews.component').then(m => m.MyReviewsComponent)
      },
      {
        path: 'track-driver/:jobId',
        loadComponent: () => import('./features/customer/track-driver/track-driver.component').then(m => m.TrackDriverComponent)
      },
      {
        path: 'price-calculator',
        loadComponent: () => import('./features/customer/price-calculator/price-calculator.component').then(m => m.PriceCalculatorComponent)
      },
      {
        path: 'settings',
        loadComponent: () => import('./features/customer/settings/customer-settings.component')
          .then(m => m.CustomerSettingsComponent)
      },
      {
        path: 'messages',
        loadComponent: () => import('./features/shared/messages/messages.component').then(m => m.MessagesComponent)
      },
      {
        path: 'support',
        loadComponent: () => import('./features/customer/support/support-tickets.component').then(m => m.SupportTicketsComponent)
      },
      {
        path: 'complaints',
        loadComponent: () => import('./features/customer/complaints/complaints-list.component').then(m => m.ComplaintsListComponent)
      },
      {
        path: 'gdpr-privacy',
        loadComponent: () => import('./features/customer/gdpr-privacy/gdpr-privacy.component').then(m => m.GdprPrivacyComponent)
      }
    ]
  },
  // Driver Portal - wrapped in driver layout
  {
    path: 'driver',
    loadComponent: () => import('./layout/driver-layout/driver-layout.component').then(m => m.DriverLayoutComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Driver'] },
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/driver/dashboard/dashboard.component').then(m => m.DriverDashboardComponent)
      },
      {
        path: 'jobs',
        loadComponent: () => import('./features/driver/my-jobs/my-jobs.component').then(m => m.MyJobsComponent)
      },
      {
        path: 'jobs/:id',
        loadComponent: () => import('./features/driver/job-details/job-details.component').then(m => m.JobDetailsComponent)
      },
      {
        path: 'profile',
        loadComponent: () => import('./features/driver/profile/profile.component').then(m => m.DriverProfileComponent)
      },
      {
        path: 'vehicles',
        loadComponent: () => import('./features/driver/my-vehicles/my-vehicles.component').then(m => m.MyVehiclesComponent)
      },
      {
        path: 'schedule',
        loadComponent: () => import('./features/driver/schedule/schedule.component').then(m => m.DriverScheduleComponent)
      },
      {
        path: 'marketplace',
        loadComponent: () => import('./features/driver/marketplace/marketplace.component').then(m => m.MarketplaceComponent)
      },
      {
        path: 'earnings',
        loadComponent: () => import('./features/driver/earnings/earnings.component').then(m => m.EarningsComponent)
      },
      {
        path: 'documents',
        loadComponent: () => import('./features/driver/documents/my-documents.component').then(m => m.MyDocumentsComponent)
      },
      {
        path: 'reviews',
        loadComponent: () => import('./features/driver/reviews/driver-reviews.component').then(m => m.DriverReviewsComponent)
      },
      {
        path: 'settings',
        loadComponent: () => import('./features/driver/settings/driver-settings.component')
          .then(m => m.DriverSettingsComponent)
      },
      {
        path: 'messages',
        loadComponent: () => import('./features/shared/messages/messages.component').then(m => m.MessagesComponent)
      },
      {
        path: 'my-bids',
        loadComponent: () => import('./features/driver/job-bids/my-bids.component').then(m => m.MyBidsComponent)
      }
    ]
  },
  // Admin Portal - wrapped in main layout (kept at root level for backward compatibility)
  {
    path: '',
    loadComponent: () => import('./layout/main-layout/main-layout.component').then(m => m.MainLayoutComponent),
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () => import('./features/admin/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      {
        path: 'activity-logs',
        component: ActivityLogsComponent
      },
      {
        path: 'activity-logs-advanced',
        loadComponent: () => import('./features/admin/activity-logs/advanced-activity-logs.component')
          .then(m => m.AdvancedActivityLogsComponent)
      },
      {
        path: 'profile',
        loadComponent: () => import('./features/admin/profile/profile.component')
          .then(m => m.ProfileComponent)
      },
      {
        path: 'notifications/preferences',
        loadComponent: () => import('./shared/pages/notification-preferences/notification-preferences.component')
          .then(m => m.NotificationPreferencesComponent)
      },
      {
        path: 'active-drivers',
        loadComponent: () => import('./features/admin/active-drivers/active-drivers.component')
          .then(m => m.ActiveDriversComponent)
      },
      {
        path: 'pricing-rules',
        loadComponent: () => import('./features/admin/pricing-rules/pricing-rules.component')
          .then(m => m.PricingRulesComponent)
      },
      {
        path: 'documents',
        loadComponent: () => import('./features/admin/documents/document-management.component')
          .then(m => m.DocumentManagementComponent)
      },
      {
        path: 'vehicles/:id/maintenance',
        loadComponent: () => import('./features/admin/vehicles/vehicle-maintenance.component')
          .then(m => m.VehicleMaintenanceComponent)
      },
      {
        path: 'users',
        loadChildren: () => import('./features/admin/users/users.routes').then(m => m.USERS_ROUTES)
      },
      {
        path: 'roles',
        loadChildren: () => import('./features/admin/roles/roles.routes').then(m => m.ROLES_ROUTES)
      },
      {
        path: 'drivers',
        loadChildren: () => import('./features/admin/drivers/drivers.routes').then(m => m.DRIVERS_ROUTES)
      },
      {
        path: 'jobs',
        loadChildren: () => import('./features/admin/jobs/jobs.routes').then(m => m.JOBS_ROUTES)
      },
      {
        path: 'settings',
        loadComponent: () => import('./features/admin/settings/settings.component')
          .then(m => m.SettingsComponent)
      },
      {
        path: 'promo-codes',
        loadComponent: () => import('./features/admin/promo-codes/promo-codes.component')
          .then(m => m.PromoCodesComponent)
      },
      {
        path: 'gdpr-management',
        loadComponent: () => import('./features/admin/gdpr-management/gdpr-management.component')
          .then(m => m.GdprManagementComponent),
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'SuperAdmin'] }
      },
      {
        path: 'user-data-management',
        loadComponent: () => import('./features/admin/user-data-management/user-data-management.component')
          .then(m => m.UserDataManagementComponent),
        canActivate: [roleGuard],
        data: { roles: ['SuperAdmin'] }
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];

/**
 * Route Configuration Examples:
 *
 * 1. Public Route (no guard):
 * { path: 'public', component: PublicComponent }
 *
 * 2. Protected Route (authentication required):
 * { path: 'protected', component: ProtectedComponent, canActivate: [authGuard] }
 *
 * 3. Role-Based Route (specific roles required):
 * {
 *   path: 'admin',
 *   component: AdminComponent,
 *   canActivate: [roleGuard],
 *   data: { roles: ['Admin', 'SuperAdmin'] }
 * }
 *
 * 4. Guest Only Route (logged out users only):
 * { path: 'login', component: LoginComponent, canActivate: [guestGuard] }
 *
 * 5. Multiple Guards:
 * {
 *   path: 'special',
 *   component: SpecialComponent,
 *   canActivate: [authGuard, roleGuard],
 *   data: { roles: ['SpecialRole'] }
 * }
 */
