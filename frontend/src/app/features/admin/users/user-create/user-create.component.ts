import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { UsersService, RolesService, RegisterUserModel, RoleDetailDto, AssignRoleModel } from '@core/api';
import {HeaderComponent} from '@app/layout/header/header.component';
import { forkJoin, of } from 'rxjs';
import { ToastService } from '@core/services/toast.service';

@Component({
  selector: 'app-user-create',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './user-create.component.html',
  styleUrls: ['./user-create.component.scss']
})
export class UserCreateComponent implements OnInit {
  private fb = inject(FormBuilder);
  private usersService = inject(UsersService);
  private rolesService = inject(RolesService);
  private router = inject(Router);
  private toastService = inject(ToastService);

  userForm: FormGroup;
  availableRoles: RoleDetailDto[] = [];
  isLoading = false;
  errorMessage = '';
  showPassword = false;
  showConfirmPassword = false;

  constructor() {
    this.userForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      userName: ['', [Validators.required, Validators.minLength(3)]],
      phoneNumber: ['', [Validators.pattern(/^\+?[1-9]\d{1,14}$/)]],
      password: ['', [Validators.required, Validators.minLength(8), this.passwordValidator]],
      confirmPassword: ['', [Validators.required]],
      roles: [[]],
      emailConfirmed: [true]
    }, { validators: this.passwordMatchValidator });
  }

  ngOnInit(): void {
    this.loadRoles();
  }

  loadRoles(): void {
    this.rolesService.apiRolesGet().subscribe({
      next: (roles) => {
        this.availableRoles = roles;
      },
      error: (error) => {
        console.error('Error loading roles:', error);
        this.errorMessage = 'Failed to load roles.';
      }
    });
  }

  passwordValidator(control: any) {
    const value = control.value;
    if (!value) return null;

    const hasUpperCase = /[A-Z]/.test(value);
    const hasLowerCase = /[a-z]/.test(value);
    const hasNumeric = /[0-9]/.test(value);
    const hasSpecialChar = /[!@#$%^&*(),.?":{}|<>]/.test(value);

    const passwordValid = hasUpperCase && hasLowerCase && hasNumeric && hasSpecialChar;

    return passwordValid ? null : {
      passwordStrength: {
        hasUpperCase,
        hasLowerCase,
        hasNumeric,
        hasSpecialChar
      }
    };
  }

  passwordMatchValidator(group: FormGroup) {
    const password = group.get('password')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { passwordMismatch: true };
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

  get password() {
    return this.userForm.get('password');
  }

  get confirmPassword() {
    return this.userForm.get('confirmPassword');
  }

  get roles() {
    return this.userForm.get('roles');
  }

  get emailConfirmed() {
    return this.userForm.get('emailConfirmed');
  }

  toggleRole(roleId: string): void {
    const currentRoles = this.roles?.value || [];
    const index = currentRoles.indexOf(roleId);

    if (index > -1) {
      currentRoles.splice(index, 1);
    } else {
      currentRoles.push(roleId);
    }

    this.roles?.setValue([...currentRoles]);
  }

  isRoleSelected(roleId: string): boolean {
    return this.roles?.value?.includes(roleId) || false;
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  toggleConfirmPasswordVisibility(): void {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  onSubmit(): void {
    if (this.userForm.invalid) {
      Object.keys(this.userForm.controls).forEach(key => {
        this.userForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const formValue = this.userForm.value;
    const registerUserModel: RegisterUserModel = {
      email: formValue.email,
      password: formValue.password,
      phoneNumber: formValue.phoneNumber || undefined
    };

    this.usersService.apiUsersRegisterPost(registerUserModel).subscribe({
      next: (user) => {
        // If user created successfully and has roles to assign
        const selectedRoles = formValue.roles || [];
        if (selectedRoles.length > 0 && user.id) {
          this.assignRolesToUser(user.id, selectedRoles);
        } else {
          this.isLoading = false;
          this.toastService.success('Success', 'User created successfully!');
          this.router.navigate(['/users']);
        }
      },
      error: (error) => {
        console.error('Error creating user:', error);
        const errorMessage = error.error?.message || error.error?.title || 'Failed to create user. Please try again.';
        this.errorMessage = errorMessage;
        this.toastService.error('Error Creating User', errorMessage);
        this.isLoading = false;
      }
    });
  }

  /**
   * Assign roles to newly created user
   */
  assignRolesToUser(userId: string, roleNames: string[]): void {
    const roleAssignments = roleNames.map(roleName => {
      const assignRoleModel: AssignRoleModel = { roleName };
      return this.usersService.apiUsersIdRolesPost(userId, assignRoleModel);
    });

    // Use forkJoin to wait for all role assignments to complete
    forkJoin(roleAssignments.length > 0 ? roleAssignments : [of(null)]).subscribe({
      next: () => {
        this.isLoading = false;
        this.toastService.success('Success', 'User created successfully with assigned roles!');
        this.router.navigate(['/users']);
      },
      error: (error) => {
        console.error('Error assigning roles:', error);
        this.errorMessage = 'User created but failed to assign some roles. Please edit the user to assign roles.';
        this.toastService.warning('Partial Success', 'User created but some roles could not be assigned. You can edit the user to assign roles.');
        this.isLoading = false;
        // Still navigate to users list since user was created
        setTimeout(() => this.router.navigate(['/users']), 3000);
      }
    });
  }

  cancel(): void {
    if (confirm('Are you sure you want to cancel? Any unsaved changes will be lost.')) {
      this.router.navigate(['/users']);
    }
  }
}
