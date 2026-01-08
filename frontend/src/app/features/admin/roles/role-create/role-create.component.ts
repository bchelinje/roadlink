import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RolesService, CreateRoleRequest } from '@core/api';

@Component({
  selector: 'app-role-create',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './role-create.component.html',
  styleUrls: ['./role-create.component.scss']
})
export class RoleCreateComponent {
  private fb = inject(FormBuilder);
  private rolesService = inject(RolesService);
  private router = inject(Router);

  roleForm: FormGroup;
  isLoading = false;
  errorMessage = '';

  constructor() {
    this.roleForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      description: ['']
    });
  }

  get name() {
    return this.roleForm.get('name');
  }

  get description() {
    return this.roleForm.get('description');
  }

  onSubmit(): void {
    if (this.roleForm.invalid) {
      Object.keys(this.roleForm.controls).forEach(key => {
        this.roleForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const formValue = this.roleForm.value;
    const createRoleDto: CreateRoleRequest = {
      name: formValue.name,
      description: formValue.description || undefined
    };

    console.log('Creating role with data:', createRoleDto);

    this.rolesService.apiRolesPost(createRoleDto).subscribe({
      next: (role) => {
        console.log('Role created successfully:', role);
        if (role?.id) {
          this.router.navigate(['/roles', role.id]);
        } else {
          this.router.navigate(['/roles']);
        }
      },
      error: (error) => {
        console.error('Error creating role:', error);
        this.errorMessage = error.error?.message || 'Failed to create role. Please try again.';
        this.isLoading = false;
      }
    });
  }

  cancel(): void {
    if (this.roleForm.dirty) {
      if (confirm('Are you sure you want to cancel? Any unsaved changes will be lost.')) {
        this.router.navigate(['/roles']);
      }
    } else {
      this.router.navigate(['/roles']);
    }
  }
}
