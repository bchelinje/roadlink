import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { UsersService, RolesService, UserViewModel, UpdateUserModel, RoleDetailDto, AssignRoleModel } from '@core/api';
import { forkJoin, of } from 'rxjs';
import { switchMap, catchError } from 'rxjs/operators';
import { ToastService } from '@core/services/toast.service';

@Component({
  selector: 'app-user-edit',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './user-edit.component.html',
  styleUrls: ['./user-edit.component.scss']
})
export class UserEditComponent implements OnInit {
  private fb = inject(FormBuilder);
  private usersService = inject(UsersService);
  private rolesService = inject(RolesService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private toastService = inject(ToastService);

  userId: string = '';
  user: UserViewModel | null = null;
  userForm: FormGroup;
  availableRoles: RoleDetailDto[] = [];
  isLoading = false;
  isSaving = false;
  errorMessage = '';
  originalRoles: string[] = [];
  originalEmailConfirmed = false;

  constructor() {
    this.userForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      userName: ['', [Validators.required, Validators.minLength(3)]],
      phoneNumber: ['', [Validators.pattern(/^\+?[1-9]\d{1,14}$/)]],
      roles: [[]],
      emailConfirmed: [false]
    });
  }

  ngOnInit(): void {
    this.userId = this.route.snapshot.paramMap.get('id') || '';
    if (this.userId) {
      this.loadUser();
      this.loadRoles();
    }
  }

  loadUser(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.usersService.apiUsersIdGet(this.userId).subscribe({
      next: (user) => {
        this.user = user;
        this.originalRoles = user.roles || [];
        this.originalEmailConfirmed = user.emailConfirmed || false;

        this.userForm.patchValue({
          email: user.email,
          userName: user.userName,
          phoneNumber: user.phoneNumber,
          roles: user.roles || [],
          emailConfirmed: user.emailConfirmed
        });
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading user:', error);
        const errorMessage = 'Failed to load user. Please try again.';
        this.errorMessage = errorMessage;
        this.toastService.error('Error Loading User', errorMessage);
        this.isLoading = false;
      }
    });
  }

  loadRoles(): void {
    this.rolesService.apiRolesGet().subscribe({
      next: (roles) => {
        this.availableRoles = roles;
      },
      error: (error) => {
        console.error('Error loading roles:', error);
      }
    });
  }

  get email() {
    return this.userForm.get('email');
  }

  get userName() {
    return this.userForm.get('userName');
  }

  get phoneNumber() {
    return this.userForm.get('phoneNumber');
  }

  get roles() {
    return this.userForm.get('roles');
  }

  get emailConfirmed() {
    return this.userForm.get('emailConfirmed');
  }

  /**
   * Toggle role selection
   * ✅ Handles nullable role names
   */
  toggleRole(roleName: string | null | undefined): void {
    // Guard against null/undefined
    if (!roleName) {
      console.warn('Attempted to toggle role with null/undefined name');
      return;
    }

    const currentRoles = this.roles?.value || [];
    const index = currentRoles.indexOf(roleName);

    if (index > -1) {
      currentRoles.splice(index, 1);
    } else {
      currentRoles.push(roleName);
    }

    this.roles?.setValue([...currentRoles]);
  }

  /**
   * Check if role is selected
   * ✅ Handles nullable role names
   */
  isRoleSelected(roleName: string | null | undefined): boolean {
    // Guard against null/undefined
    if (!roleName) {
      return false;
    }

    return this.roles?.value?.includes(roleName) || false;
  }

  onSubmit(): void {
    if (this.userForm.invalid) {
      Object.keys(this.userForm.controls).forEach(key => {
        this.userForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';

    const formValue = this.userForm.value;

    // UpdateUserModel doesn't include emailConfirmed in the generated types,
    // but the backend API (UpdateUserDto) does support it.
    // We cast to any to include it.
    const updateUserModel: any = {
      email: formValue.email,
      userName: formValue.userName,
      phoneNumber: formValue.phoneNumber || undefined,
      emailConfirmed: formValue.emailConfirmed
    };

    console.log('Updating user with data:', updateUserModel);

    // First update the user, then sync roles
    this.usersService.apiUsersIdPut(this.userId, updateUserModel).pipe(
      switchMap(() => {
        // Sync roles after user update
        return this.syncRoles(formValue.roles || []);
      })
    ).subscribe({
      next: () => {
        console.log('User and roles updated successfully');
        this.isSaving = false;
        this.toastService.success('Success', 'User updated successfully!');
        this.router.navigate(['/users', this.userId]);
      },
      error: (error) => {
        console.error('Error updating user:', error);
        const errorMessage = error.error?.message || error.error?.title || 'Failed to update user. Please try again.';
        this.errorMessage = errorMessage;
        this.toastService.error('Error Updating User', errorMessage);
        this.isSaving = false;
      }
    });
  }

  /**
   * Synchronize roles with backend
   * Handles individual role operation failures gracefully
   */
  syncRoles(newRoles: string[]) {
    const rolesToAdd = newRoles.filter(role => !this.originalRoles.includes(role));
    const rolesToRemove = this.originalRoles.filter(role => !newRoles.includes(role));

    const operations: any[] = [];

    // Add new roles with individual error handling
    rolesToAdd.forEach(roleName => {
      const assignRoleModel: AssignRoleModel = { roleName };
      operations.push(
        this.usersService.apiUsersIdRolesPost(this.userId, assignRoleModel).pipe(
          catchError(error => {
            console.warn(`Failed to add role "${roleName}":`, error);
            return of(null); // Continue despite error
          })
        )
      );
    });

    // Remove old roles with individual error handling
    rolesToRemove.forEach(roleName => {
      operations.push(
        this.usersService.apiUsersIdRolesRoleNameDelete(this.userId, roleName).pipe(
          catchError(error => {
            console.warn(`Failed to remove role "${roleName}":`, error);
            return of(null); // Continue despite error
          })
        )
      );
    });

    // If no operations, return a completed observable
    if (operations.length === 0) {
      return of(null);
    }

    return forkJoin(operations);
  }

  cancel(): void {
    if (this.userForm.dirty) {
      if (confirm('Are you sure you want to cancel? Any unsaved changes will be lost.')) {
        this.router.navigate(['/users', this.userId]);
      }
    } else {
      this.router.navigate(['/users', this.userId]);
    }
  }

  resetPassword(): void {
    if (confirm('Send password reset email to this user?')) {
      if (!this.user?.email) return;

      this.usersService.apiUsersForgotPasswordPost({ email: this.user.email }).subscribe({
        next: () => {
          this.toastService.success('Success', 'Password reset email sent successfully');
        },
        error: (err: any) => {
          console.error('Failed to send password reset email:', err);
          this.toastService.error('Error', 'Failed to send password reset email');
        }
      });
    }
  }

  unlockAccount(): void {
    if (confirm('Unlock this user account?')) {
      this.usersService.apiUsersIdUnlockPost(this.userId).subscribe({
        next: () => {
          this.toastService.success('Success', 'Account unlocked successfully');
          this.loadUser(); // Reload to show updated status
        },
        error: (err: any) => {
          console.error('Failed to unlock account:', err);
          this.toastService.error('Error', 'Failed to unlock account');
        }
      });
    }
  }
}
