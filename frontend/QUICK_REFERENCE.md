# Quick Reference - Code Patterns & Conventions

## File Locations

| Purpose | Location |
|---------|----------|
| Services | `/app/core/services/` |
| Models/Interfaces | `/app/core/models/` |
| Guards | `/app/core/guards/` |
| Interceptors | `/app/core/interceptors/` |
| API Services (auto-generated) | `/app/core/api/api/` |
| API Models (auto-generated) | `/app/core/api/model/` |
| Admin Features | `/app/features/admin/[feature]/` |
| Customer Features | `/app/features/customer/[feature]/` |
| Driver Features | `/app/features/driver/[feature]/` |
| Shared Components | `/app/shared/components/[component]/` |
| Layouts | `/app/layout/[layout-type]/` |

---

## Service Creation Template

```typescript
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import { MyModel, MyDto } from '@core/models/my.models';

@Injectable({
  providedIn: 'root'
})
export class MyService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/api/my-endpoint`;

  getItems(): Observable<MyModel[]> {
    return this.http.get<MyModel[]>(this.apiUrl);
  }

  getItem(id: string): Observable<MyModel> {
    return this.http.get<MyModel>(`${this.apiUrl}/${id}`);
  }

  createItem(dto: MyDto): Observable<MyModel> {
    return this.http.post<MyModel>(this.apiUrl, dto);
  }

  updateItem(id: string, dto: MyDto): Observable<MyModel> {
    return this.http.put<MyModel>(`${this.apiUrl}/${id}`, dto);
  }

  deleteItem(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  // With query parameters
  search(query: string, page: number = 1): Observable<MyModel[]> {
    let params = new HttpParams()
      .set('q', query)
      .set('page', page.toString());
    return this.http.get<MyModel[]>(this.apiUrl, { params });
  }
}
```

---

## Component Creation Template

```typescript
import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MyService } from '@core/services/my.service';
import { ToastService } from '@core/services/toast.service';
import { MyModel, MyDto } from '@core/models/my.models';

@Component({
  selector: 'app-my-feature',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './my-feature.component.html',
  styleUrls: ['./my-feature.component.scss']
})
export class MyFeatureComponent implements OnInit {
  // Dependency Injection
  private readonly myService = inject(MyService);
  private readonly toastService = inject(ToastService);

  // State Management
  items: MyModel[] = [];
  loading = false;
  saving = false;
  selectedItem: MyModel | null = null;

  // Form State
  form: MyDto = {};
  showModal = false;

  ngOnInit(): void {
    this.loadItems();
  }

  // Load Data
  loadItems(): void {
    this.loading = true;
    this.myService.getItems().subscribe({
      next: (data) => {
        this.items = data;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading items:', error);
        this.toastService.error('Error', 'Failed to load items');
        this.loading = false;
      }
    });
  }

  // Save Data
  saveItem(): void {
    this.saving = true;
    const request = this.selectedItem?.id
      ? this.myService.updateItem(this.selectedItem.id, this.form)
      : this.myService.createItem(this.form);

    request.subscribe({
      next: (result) => {
        if (this.selectedItem?.id) {
          const index = this.items.findIndex(i => i.id === result.id);
          if (index >= 0) {
            this.items[index] = result;
          }
        } else {
          this.items.push(result);
        }
        this.toastService.success('Success', 'Item saved successfully');
        this.closeModal();
        this.saving = false;
      },
      error: (error) => {
        console.error('Error saving item:', error);
        this.toastService.error('Error', 'Failed to save item');
        this.saving = false;
      }
    });
  }

  // Delete Data
  deleteItem(id: string): void {
    if (!confirm('Are you sure you want to delete this item?')) {
      return;
    }

    this.myService.deleteItem(id).subscribe({
      next: () => {
        this.items = this.items.filter(i => i.id !== id);
        this.toastService.success('Success', 'Item deleted successfully');
      },
      error: (error) => {
        console.error('Error deleting item:', error);
        this.toastService.error('Error', 'Failed to delete item');
      }
    });
  }

  // Modal Management
  openModal(item?: MyModel): void {
    this.selectedItem = item || null;
    this.form = item ? { /* map item to form */ } : {};
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.selectedItem = null;
    this.form = {};
  }
}
```

---

## Model Creation Template

```typescript
// /app/core/models/my.models.ts

// Request/Response Models
export interface MyModel {
  id: string;
  name: string;
  status: 'active' | 'inactive';
  createdAt: Date;
  updatedAt: Date;
}

// DTOs (Data Transfer Objects)
export interface CreateMyDto {
  name: string;
  status?: string;
}

export interface UpdateMyDto {
  name?: string;
  status?: string;
}

// API Response Wrapper (if needed)
export interface MyListResponse {
  items: MyModel[];
  totalCount: number;
  page: number;
  pageSize: number;
}
```

---

## Angular Material Button Patterns

```html
<!-- Primary Action -->
<button type="button" (click)="save()" [disabled]="saving">
  {{ saving ? 'Saving...' : 'Save' }}
</button>

<!-- Secondary Action -->
<button type="button" (click)="cancel()" class="secondary">
  Cancel
</button>

<!-- Danger Action -->
<button type="button" (click)="delete()" class="danger">
  Delete
</button>

<!-- Loading State -->
<div *ngIf="loading" class="spinner">
  <p>Loading...</p>
</div>
```

---

## RxJS Common Patterns

```typescript
// Single value
this.service.getItem(id).subscribe({
  next: (data) => { /* handle */ },
  error: (error) => { /* handle */ }
});

// Multiple items with filter
this.service.getItems()
  .pipe(
    map(items => items.filter(i => i.active))
  )
  .subscribe(filtered => { /* handle */ });

// Combine multiple observables
combineLatest([
  this.service.getUsers(),
  this.service.getRoles()
]).subscribe(([users, roles]) => { /* handle */ });

// Handle errors gracefully
this.service.getItem(id)
  .pipe(
    catchError(error => {
      console.error('Error:', error);
      return of(null);
    })
  )
  .subscribe(data => { /* handle */ });
```

---

## Authentication Patterns

### Protect a Route
```typescript
// In app.routes.ts
{
  path: 'admin/gdpr',
  component: GdprComponent,
  canActivate: [authGuard, roleGuard],
  data: { roles: ['Admin', 'SuperAdmin'] }
}
```

### Check User Role in Component
```typescript
import { AuthService } from '@core/services/auth.service';

export class MyComponent {
  private authService = inject(AuthService);

  isAdmin = this.authService.hasRole('Admin');
  isCustomer = this.authService.hasAnyRole(['Customer', 'Guest']);

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
```

### Get Current User
```typescript
this.authService.currentUser$.subscribe(user => {
  if (user) {
    console.log('User:', user.email, user.roles);
  }
});
```

---

## Toast Notifications

```typescript
// Success notification
this.toastService.success('Success', 'Item created successfully');

// Error notification
this.toastService.error('Error', 'Failed to create item');

// Warning notification
this.toastService.warning('Warning', 'This action cannot be undone');

// Info notification
this.toastService.info('Info', 'Please wait while processing');
```

---

## Form Patterns

### Template-Driven Form (Simple)
```html
<form (ngSubmit)="save()">
  <input [(ngModel)]="form.name" name="name" placeholder="Name" />
  <input [(ngModel)]="form.email" name="email" type="email" placeholder="Email" />
  <button type="submit" [disabled]="saving">Save</button>
</form>
```

### Component (Template-Driven)
```typescript
form: MyDto = { name: '', email: '' };
saving = false;

save(): void {
  this.saving = true;
  this.myService.create(this.form).subscribe({
    next: () => {
      this.form = { name: '', email: '' };
      this.toastService.success('Success', 'Saved');
      this.saving = false;
    },
    error: () => {
      this.toastService.error('Error', 'Failed to save');
      this.saving = false;
    }
  });
}
```

---

## Conditional Rendering

```html
<!-- Show/Hide -->
<div *ngIf="isVisible">Content</div>

<!-- Alternative Content -->
<div *ngIf="items.length > 0; else noItems">
  <div *ngFor="let item of items">{{ item.name }}</div>
</div>
<ng-template #noItems>
  <p>No items found</p>
</ng-template>

<!-- CSS Classes -->
<div [class.active]="isActive" [class.disabled]="isDisabled">
  Content
</div>

<!-- Multiple Classes -->
<div [ngClass]="{ 'active': isActive, 'loading': isLoading }">
  Content
</div>
```

---

## Modal/Dialog Pattern

```typescript
// Component
showModal = false;
selectedItem: MyModel | null = null;

openModal(item?: MyModel): void {
  this.selectedItem = item || null;
  this.showModal = true;
}

closeModal(): void {
  this.showModal = false;
  this.selectedItem = null;
}
```

```html
<!-- Template -->
<div class="modal" *ngIf="showModal">
  <div class="modal-content">
    <div class="modal-header">
      <h2>{{ selectedItem ? 'Edit' : 'Create' }} Item</h2>
      <button (click)="closeModal()">Close</button>
    </div>
    <div class="modal-body">
      <!-- Form content -->
    </div>
    <div class="modal-footer">
      <button (click)="closeModal()">Cancel</button>
      <button (click)="save()">Save</button>
    </div>
  </div>
</div>
```

---

## HTTP Error Handling

```typescript
// In service
createItem(dto: MyDto): Observable<MyModel> {
  return this.http.post<MyModel>(this.apiUrl, dto).pipe(
    catchError(error => {
      console.error('API Error:', error);
      // Transform error for component
      throw error;
    })
  );
}

// In component
this.service.createItem(data).subscribe({
  next: (result) => { /* success */ },
  error: (error) => {
    const message = error.error?.message || 'An error occurred';
    this.toastService.error('Error', message);
  }
});
```

---

## Environment Variables

```typescript
// Access environment config
import { environment } from '@environments/environment';

const apiUrl = environment.apiBaseUrl;
const tokenEndpoint = environment.auth.tokenEndpoint;

// Available environment properties
environment.production  // boolean
environment.apiBaseUrl  // string
environment.apiConfig   // { basePath, withCredentials }
environment.auth        // { clientId, tokenEndpoint, userinfoEndpoint, etc }
environment.tokenStorageKey
environment.refreshTokenStorageKey
```

---

## Common Imports

```typescript
// Angular Core
import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';

// Services
import { HttpClient, HttpParams } from '@angular/common/http';
import { AuthService } from '@core/services/auth.service';
import { ToastService } from '@core/services/toast.service';

// RxJS
import { Observable } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';

// Models
import { environment } from '@environments/environment';
```

---

## Standalone Component Imports

```typescript
@Component({
  selector: 'app-my-component',
  standalone: true,
  imports: [
    CommonModule,           // ngIf, ngFor, ngClass, etc
    FormsModule,            // ngModel, ngSubmit
    ReactiveFormsModule,    // FormGroup, FormControl
    RouterModule,           // routerLink, routerOutlet
    MyChildComponent        // Custom components
  ],
  template: `...`,
  styles: [`...`]
})
```

---

## Testing Patterns

### Service Test
```typescript
describe('MyService', () => {
  let service: MyService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(MyService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should fetch items', (done) => {
    service.getItems().subscribe((items) => {
      expect(items).toBeTruthy();
      done();
    });
  });
});
```

### Component Test
```typescript
describe('MyComponent', () => {
  let component: MyComponent;
  let fixture: ComponentFixture<MyComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MyComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(MyComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
```

---

## File Naming Convention

| Type | Pattern | Example |
|------|---------|---------|
| Component | `*.component.ts` | `user-list.component.ts` |
| Service | `*.service.ts` | `user.service.ts` |
| Guard | `*.guard.ts` | `auth.guard.ts` |
| Interceptor | `*.interceptor.ts` | `auth.interceptor.ts` |
| Model/Interface | `*.models.ts` | `user.models.ts` |
| Routes | `*.routes.ts` | `user.routes.ts` |
| Module | `*.module.ts` | (deprecated - use standalone) |

---

## Directory Structure for New Feature

```
/app/features/admin/my-feature/
├── my-feature.component.ts
├── my-feature.component.html
├── my-feature.component.scss
├── my-feature.routes.ts (if needed)
└── [sub-components]
  ├── my-sub.component.ts
  ├── my-sub.component.html
  └── my-sub.component.scss
```

---

## Key Resources

- **Main Routes**: `/app/app.routes.ts`
- **Configuration**: `/app/app.config.ts`
- **Environment**: `/src/environments/environment.ts`
- **Global Styles**: `/src/styles.scss`
- **Auth Guards**: `/app/core/guards/auth.guard.ts`
- **Auth Service**: `/app/core/services/auth.service.ts`
- **Toast Service**: `/app/core/services/toast.service.ts`

