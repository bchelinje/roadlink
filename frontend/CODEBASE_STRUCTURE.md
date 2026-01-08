# BEC Admin Dashboard - Codebase Structure Analysis

## 1. PROJECT OVERVIEW

### Angular Version & Framework
- **Angular Version**: 19.0.0 (Latest)
- **Build System**: Angular CLI 19.0.4
- **Styling**: SCSS + Tailwind CSS 3.4.1
- **Component Style**: Standalone components (modern Angular architecture)
- **UI Library**: Angular Material 19.2.19
- **HTTP Client**: Angular HttpClient with interceptors
- **Authentication**: OAuth2/OpenID Connect (JWT-based)
- **State Management**: RxJS observables (no NgRx/Redux)
- **Linting**: SCSS preferred for components
- **Testing**: Jasmine/Karma configured

### Project Structure
```
src/
├── app/
│   ├── core/                          # Core application logic
│   │   ├── guards/                    # Route guards (auth, role-based)
│   │   ├── interceptors/              # HTTP interceptors (auth, error handling)
│   │   ├── services/                  # Shared services
│   │   ├── models/                    # TypeScript interfaces & models
│   │   └── api/                       # Auto-generated OpenAPI services
│   │       ├── api/                   # API service files (users, drivers, jobs, etc.)
│   │       └── model/                 # API response models
│   ├── features/                      # Feature modules (business logic)
│   │   ├── admin/                     # Admin dashboard features
│   │   ├── customer/                  # Customer portal features
│   │   ├── driver/                    # Driver portal features
│   │   ├── public/                    # Public pages (landing, help center)
│   │   └── shared/                    # Shared features across roles
│   ├── layout/                        # Layout components
│   │   ├── main-layout/               # Admin layout
│   │   ├── customer-layout/           # Customer portal layout
│   │   ├── driver-layout/             # Driver portal layout
│   │   ├── header/
│   │   ├── sidebar/
│   │   └── footer/
│   ├── shared/                        # Shared components & utilities
│   │   ├── components/                # Reusable UI components
│   │   └── pages/                     # Shared pages (notifications)
│   ├── dtos/                          # Data transfer objects
│   ├── app.routes.ts                  # Route configuration
│   ├── app.config.ts                  # Application configuration
│   └── app.component.ts               # Root component
├── environments/                      # Environment configurations
└── styles.scss                        # Global styles
```

---

## 2. FOLDER ORGANIZATION

### Core Module (`/app/core`)

#### **Guards** (`/core/guards`)
- `auth.guard.ts` - Protects routes requiring authentication
  - `authGuard`: Requires valid JWT token
  - `roleGuard`: Enforces role-based access control
  - `guestGuard`: Prevents authenticated users from accessing login
- Guards use synchronous checks to avoid race conditions on page refresh

#### **Interceptors** (`/core/interceptors`)
- `auth.interceptor.ts`
  - Adds JWT Bearer token to all HTTP requests
  - Handles both JWT and JWE tokens
  - Skips expired token transmission for standard JWTs
  
- `error.interceptor.ts`
  - Centralized error handling
  - 401 errors trigger logout
  - Global error notifications via ToastService

#### **Services** (`/core/services`)
Key shared services:
- **auth.service.ts** - Authentication management
  - OAuth2 Resource Owner Password flow
  - JWT/JWE token handling
  - User info loading from token or `/userinfo` endpoint
  - Role-based default routing
  
- **token.service.ts** - Token storage & management
  - LocalStorage persistence
  - Token expiration checking
  - JWT decoding utilities
  
- **settings.service.ts** - User/platform settings management
  - User, Driver, Customer, and Platform settings
  - CRUD operations for settings
  
- **activity-log.service.ts** - Activity logging
  - Filtered pagination support
  - Activity statistics and export
  
- **toast.service.ts** - Toast notifications
  - Success, error, warning notifications
  
- **Other services**: support, help-center, messages, complaints, promo-codes, admin-dashboard, job-bids

#### **Models** (`/core/models`)
Type definitions for:
- `settings.models.ts` - UserSettings, DriverSettings, CustomerSettings, PlatformSettings
- Admin dashboards, complaints, help center, job bids, messages, promos, support

#### **API** (`/core/api`)
Auto-generated OpenAPI services from Swagger:
```
api/
├── api/
│   ├── users.service.ts
│   ├── drivers.service.ts
│   ├── jobs.service.ts
│   ├── customers.service.ts
│   ├── payments.service.ts
│   ├── notifications.service.ts
│   ├── reviews.service.ts
│   ├── documents.service.ts
│   ├── roles.service.ts
│   ├── vehicles.service.ts
│   ├── pricing.service.ts
│   ├── activity-logs.service.ts
│   └── ...other services
└── model/
    └── [Auto-generated DTOs]
```
- Generated via OpenAPI Generator CLI from Swagger/OpenAPI spec
- Configured in `package.json`: `generate:api` script

---

## 3. ROUTING ARCHITECTURE

### Route Structure
```
App Routes (app.routes.ts):
├── Public Routes (no auth required)
│   ├── / - Landing page
│   ├── /login - Login page (guestGuard)
│   ├── /forgot-password
│   ├── /reset-password
│   ├── /verify-email
│   ├── /book - Customer booking page (public)
│   └── /help-center
│
├── Customer Portal (/customer) - authGuard + roleGuard['Customer']
│   └── Uses CustomerLayoutComponent
│       ├── /customer/dashboard
│       ├── /customer/my-jobs
│       ├── /customer/request-job
│       ├── /customer/book-job
│       ├── /customer/job-templates
│       ├── /customer/recurring-jobs
│       ├── /customer/addresses
│       ├── /customer/favorites
│       ├── /customer/payments
│       ├── /customer/my-reviews
│       ├── /customer/profile
│       ├── /customer/settings
│       ├── /customer/messages
│       └── /customer/support
│
├── Driver Portal (/driver) - authGuard + roleGuard['Driver']
│   └── Uses DriverLayoutComponent
│       ├── /driver/dashboard
│       ├── /driver/jobs
│       ├── /driver/profile
│       ├── /driver/vehicles
│       ├── /driver/schedule
│       ├── /driver/marketplace
│       ├── /driver/earnings
│       ├── /driver/documents
│       ├── /driver/reviews
│       ├── /driver/settings
│       ├── /driver/messages
│       └── /driver/my-bids
│
└── Admin Portal (/) - authGuard (any authenticated user)
    └── Uses MainLayoutComponent
        ├── /dashboard
        ├── /users (with sub-routes: create, detail, edit)
        ├── /drivers (with sub-routes: create, detail, edit)
        ├── /jobs (with sub-routes: create, detail, edit)
        ├── /roles
        ├── /documents
        ├── /pricing-rules
        ├── /activity-logs & /activity-logs-advanced
        ├── /profile
        ├── /notifications/preferences
        ├── /settings
        └── /promo-codes
```

### Guard Usage Pattern
```typescript
// Route with authentication
{ path: 'protected', component: X, canActivate: [authGuard] }

// Route with role-based access
{ path: 'admin', component: X, canActivate: [authGuard, roleGuard], data: { roles: ['Admin'] } }

// Public route (explicit guest check)
{ path: 'login', component: X, canActivate: [guestGuard] }
```

---

## 4. AUTHENTICATION & AUTHORIZATION FLOW

### Authentication Architecture
```
User Login
    ↓
login() in AuthService
    ↓
POST to environment.auth.tokenEndpoint (OAuth2 Resource Owner Password)
    ↓
Receive TokenResponse { access_token, refresh_token, expires_in }
    ↓
Store in LocalStorage via TokenService
    ↓
Load user info from JWT payload OR /userinfo endpoint
    ↓
Update currentUserSubject & isAuthenticatedSubject observables
```

### Token Management
- **Storage**: LocalStorage
  - Key: `bec_access_token`
  - Key: `bec_refresh_token`
- **Token Types Supported**:
  - JWT (decodable client-side, can extract user info immediately)
  - JWE (encrypted, must fetch from `/userinfo` endpoint)
- **Expiration Checking**: Synchronous check using jwt-decode library
- **Refresh Token Flow**: Automatic refresh via `refreshToken()` method

### User Info Loading
1. **For JWT tokens**: Decode on client, extract roles/email immediately
2. **For JWE tokens**: Fetch from `/userinfo` endpoint via authInterceptor
3. **Fallback**: Use minimal user info if endpoint fails

### Role-Based Access Control
- Roles stored in JWT as `role` claim (array or string)
- Available roles: `Customer`, `Driver`, `Admin`, `SuperAdmin`
- Default route based on role:
  - Customer → `/customer/dashboard`
  - Driver → `/driver/dashboard`
  - Admin/SuperAdmin → `/dashboard`

### AuthService API
```typescript
// Authentication
login(email, password): Observable<boolean>
logout(): void
refreshToken(): Observable<boolean>

// User Info
getCurrentUser(): UserInfo | null
currentUser$: Observable<UserInfo | null>

// Role Checks
hasRole(role: string): boolean
hasAnyRole(roles: string[]): boolean

// Status
isAuthenticated(): boolean
isAuthenticated$: Observable<boolean>

// Navigation
getDefaultRoute(): string
```

---

## 5. API CALL PATTERNS & DATA MODELS

### HTTP Client Pattern
```typescript
// Service Implementation
@Injectable({ providedIn: 'root' })
export class MyService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/api/endpoint`;

  getItems(): Observable<Item[]> {
    return this.http.get<Item[]>(this.apiUrl);
  }

  getItem(id: string): Observable<Item> {
    return this.http.get<Item>(`${this.apiUrl}/${id}`);
  }

  createItem(dto: CreateItemDto): Observable<Item> {
    return this.http.post<Item>(this.apiUrl, dto);
  }

  updateItem(id: string, dto: UpdateItemDto): Observable<Item> {
    return this.http.put<Item>(`${this.apiUrl}/${id}`, dto);
  }

  deleteItem(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  // With query params
  listItems(filter?: ItemFilter): Observable<ItemPage> {
    let params = new HttpParams();
    if (filter?.search) params = params.set('search', filter.search);
    if (filter?.page) params = params.set('page', filter.page.toString());
    return this.http.get<ItemPage>(this.apiUrl, { params });
  }
}
```

### Component API Usage Pattern
```typescript
export class MyComponent implements OnInit {
  private readonly myService = inject(MyService);
  private readonly toastService = inject(ToastService);

  items: Item[] = [];
  loading = false;
  saving = false;

  ngOnInit(): void {
    this.loadItems();
  }

  loadItems(): void {
    this.loading = true;
    this.myService.getItems().subscribe({
      next: (data) => {
        this.items = data;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error:', error);
        this.toastService.error('Error', 'Failed to load items');
        this.loading = false;
      }
    });
  }

  saveItem(item: Item): void {
    this.saving = true;
    const dto = { /* map to DTO */ };
    
    this.myService.updateItem(item.id, dto).subscribe({
      next: (updated) => {
        this.items = this.items.map(i => i.id === updated.id ? updated : i);
        this.toastService.success('Success', 'Item saved');
        this.saving = false;
      },
      error: (error) => {
        this.toastService.error('Error', 'Failed to save item');
        this.saving = false;
      }
    });
  }
}
```

### API Configuration
- **Base URL**: `environment.apiBaseUrl` (default: `https://localhost:7172`)
- **Configuration**: Created in `app.config.ts` via `Configuration` class
- **Auth Header**: Added automatically by `authInterceptor`
- **Error Handling**: Centralized in `errorInterceptor`

### Data Models Pattern
Models are in two locations:

1. **Core Models** (`/core/models/*.ts`)
   - TypeScript interfaces for frontend logic
   - Example: `UserSettings`, `DriverSettings`, `CustomerSettings`
   - Match with API contracts but frontend-specific

2. **API Models** (`/core/api/model/*.ts`)
   - Auto-generated from OpenAPI spec
   - Named with DTOs (e.g., `CreateUserDto`, `UpdateUserModel`)
   - Auto-regenerated via `npm run generate:api`

---

## 6. SETTINGS & ADMIN PANELS

### User Settings Pages

#### **Admin Settings** (`/settings`)
- **Component**: `AdminSettingsComponent`
- **Service**: `SettingsService`
- **Model**: `PlatformSettings`, `CreatePlatformSettingDto`, `UpdatePlatformSettingDto`
- **Features**:
  - Create/Read/Update/Delete platform-wide settings
  - Filter by category (general, payment, email, maps, notifications, security)
  - Search across key, name, description
  - Editable flag enforcement
  - Modal-based workflows

#### **Customer Settings** (`/customer/settings`)
- **Component**: `CustomerSettingsComponent`
- **Model**: `CustomerSettings`, `UpdateCustomerSettingsDto`
- **Tab Organization**:
  - Booking Preferences
  - Payment Settings
  - Notification Settings
  - Display Preferences
  - Accessibility Settings
- **Features**: Auto-load, form initialization, update with validation

#### **Driver Settings** (potential)
- **Model**: `DriverSettings`, `UpdateDriverSettingsDto`
- **Categories**:
  - Availability Settings
  - Job Preferences
  - Payment Settings
  - Notification Settings
  - Vehicle Settings
  - Privacy Settings

#### **User Profile Settings** (universal)
- **Component**: `ProfileComponent` (admin), `CustomerProfileComponent`, `DriverProfileComponent`
- **Model**: `UserSettings`, `UpdateUserSettingsDto`
- **Settings Categories**:
  - Profile (language, timezone, currency, date/time formats)
  - Privacy (data sharing, profile visibility, marketing emails)
  - Security (2FA, verification requirements, session timeouts)
  - Display (theme, contrast mode, motion reduction)

### Notification Preferences
- **Component**: `NotificationPreferencesComponent` at `/notifications/preferences`
- **Route**: Accessible via admin layout
- **Purpose**: Centralized notification settings management

### Settings Service Architecture
```typescript
SettingsService methods:
- getUserSettings(): Get user-level settings
- updateUserSettings(dto): Update user settings
- getDriverSettings(): Get driver-specific settings
- updateDriverSettings(dto): Update driver settings
- getCustomerSettings(): Get customer-specific settings
- updateCustomerSettings(dto): Update customer settings
- getPlatformSettings(category?, isPublic?): List platform settings
- getPublicPlatformSettings(): Get public settings only
- getPlatformSettingByKey(key): Get single setting
- createPlatformSetting(dto): Create admin setting
- updatePlatformSetting(key, dto): Update admin setting
- deletePlatformSetting(key): Delete admin setting
```

### Admin Panels & Features
- **Dashboard** (`/dashboard`) - Overview metrics
- **User Management** (`/users`) - CRUD operations with create/detail/edit views
- **Driver Management** (`/drivers`) - Driver listing and management
  - Sub-page: `/drivers/:id/dashboard` - Individual driver dashboard
- **Job Management** (`/jobs`) - Job CRUD with bulk operations
- **Role & Permissions** (`/roles`) - Role creation and permission assignment
- **Activity Logs** (`/activity-logs`, `/activity-logs-advanced`) - Audit trail
  - Standard view with filtering
  - Advanced view with analytics
- **Document Management** (`/documents`) - Document upload/review
- **Pricing Rules** (`/pricing-rules`) - Dynamic pricing configuration
- **Promo Codes** (`/promo-codes`) - Promotional code management

---

## 7. KEY ARCHITECTURAL PATTERNS

### 1. Standalone Components
- All components use `standalone: true` (modern Angular 14+)
- Explicit imports array for dependencies
- No NgModule files (feature-based organization)

### 2. Dependency Injection
- Constructor injection via `inject()` function
- Services provided at root level via `providedIn: 'root'`
- Type-safe, testable pattern

### 3. Reactive Programming
- RxJS observables for async operations
- No async/await in services (RxJS piping)
- Components subscribe in templates or TypeScript

### 4. Lazy Loading
- Route-based code splitting via `loadComponent` and `loadChildren`
- Feature modules loaded on demand
- Reduces initial bundle size

### 5. Error Handling
```typescript
// Component pattern
.subscribe({
  next: (data) => { /* handle success */ },
  error: (error) => {
    console.error('Error:', error);
    this.toastService.error('Error', errorMessage);
  }
})
```

### 6. Form Management
- Reactive Forms pattern with `FormGroup`, `FormControl`
- Template-driven forms for simple forms
- Manual form state management (no NgRx)

### 7. State Management
- Minimal state via RxJS `BehaviorSubject`
- No NgRx/Redux complexity
- Service-level caching where needed

---

## 8. ENVIRONMENT CONFIGURATION

### Environment File
```typescript
environment: {
  production: false,
  apiBaseUrl: 'https://localhost:7172',
  apiConfig: { basePath, withCredentials },
  auth: {
    clientId: 'angular-admin-app',
    tokenEndpoint: 'https://localhost:7172/connect/token',
    userinfoEndpoint: 'https://localhost:7172/connect/userinfo',
    authority: 'https://localhost:7172',
    scope: 'openid profile email roles'
  }
}
```

---

## 9. NAMING CONVENTIONS & STRUCTURE

### File Naming
- Components: `*.component.ts`, `*.component.html`, `*.component.scss`
- Services: `*.service.ts`
- Guards: `*.guard.ts`
- Interceptors: `*.interceptor.ts`
- Models: `*.models.ts`
- Routes: `*.routes.ts`

### Component Naming
- Class: `PascalCase` (e.g., `CustomerSettingsComponent`)
- Selector: `kebab-case` (e.g., `app-customer-settings`)
- Template: matches component name with `.html` extension

### CSS/SCSS Naming
- BEM (Block Element Modifier) convention
- Component-scoped styling (ViewEncapsulation.Emulated)
- Tailwind classes for utility styling
- Global styles in `styles.scss`

### Folder Organization
- **By feature**: `/features/[role]/[feature]`
- **Shared components**: `/shared/components/[component-name]`
- **Core utilities**: `/core/[services|guards|interceptors|models]`
- **Layouts**: `/layout/[layout-type]`

---

## 10. CONVENTIONS TO FOLLOW FOR GDPR FEATURES

Based on the codebase analysis, here are the patterns to follow:

### Service Creation
1. Create in `/app/core/services/gdpr.service.ts`
2. Inject `HttpClient` and use `environment.apiBaseUrl`
3. Use Observable return types with proper typing
4. No circular dependencies (don't inject AuthService in interceptors)

### Model Creation
1. Create interfaces in `/app/core/models/gdpr.models.ts`
2. Separate DTOs for request/response
3. Follow existing naming: `GdprRequest`, `GdprResponse`, `UpdateGdprDto`

### Component Creation
1. Standalone component in `/app/features/[role]/[feature]/[feature].component.ts`
2. Inject services with `private readonly service = inject(Service)`
3. Manage state with `loading`, `saving` flags
4. Use `ToastService` for notifications
5. Subscribe in components with error handling pattern

### Routing
1. Add to `app.routes.ts` under appropriate role section
2. Use `canActivate: [authGuard, roleGuard]` with `data: { roles: [...] }`
3. Use lazy loading: `loadComponent: () => import(...)`

### API Calls
1. Service methods return `Observable<T>`
2. Components handle subscription with `next`/`error` pattern
3. Use `ToastService` for success/error messages
4. Proper error logging and user feedback

---

## 11. KEY FILES TO REFERENCE

- **Routing**: `/app/app.routes.ts`
- **Auth Guard**: `/app/core/guards/auth.guard.ts`
- **Auth Service**: `/app/core/services/auth.service.ts`
- **Settings Service**: `/app/core/services/settings.service.ts`
- **Settings Models**: `/app/core/models/settings.models.ts`
- **Customer Settings Component**: `/app/features/customer/settings/customer-settings.component.ts`
- **Admin Settings Component**: `/app/features/admin/settings/settings.component.ts`
- **Main Layout**: `/app/layout/main-layout/main-layout.component.ts`
- **Customer Layout**: `/app/layout/customer-layout/customer-layout.component.ts`
- **App Config**: `/app/app.config.ts`
- **Environment**: `/src/environments/environment.ts`

---

## SUMMARY

The BEC Admin Dashboard is a modern Angular 19 application with:
- Clear separation between Admin, Customer, and Driver portals
- Robust authentication with OAuth2/OpenID Connect
- Centralized service-based architecture
- Lazy-loaded feature modules
- Type-safe API integration via auto-generated OpenAPI services
- Existing settings management patterns for users and platform configs
- RxJS-based reactive programming approach
- SCSS + Tailwind CSS styling

For GDPR privacy features, follow the established patterns in the settings components and services, maintain the role-based routing structure, and leverage the existing authentication/authorization infrastructure.
