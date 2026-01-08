import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import {RolesService, RoleDto, UpdateRoleRequest} from '@core/api';

@Component({
  selector: 'app-role-edit',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './role-edit.component.html',
  styleUrls: ['./role-edit.component.scss']
})
export class RoleEditComponent implements OnInit {
  private fb = inject(FormBuilder);
  private rolesService = inject(RolesService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  roleId: string = '';
  role: RoleDto | null = null;
  roleForm: FormGroup;
  isLoading = false;
  isSaving = false;
  errorMessage = '';

  constructor() {
    this.roleForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      description: ['']
    });
  }

  ngOnInit(): void {
    this.roleId = this.route.snapshot.paramMap.get('id') || '';
    if (this.roleId) {
      this.loadRole();
    }
  }

  loadRole(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.rolesService.apiRolesIdGet(this.roleId).subscribe({
      next: (role) => {
        this.role = role;
        this.roleForm.patchValue({
          name: role.name,
          description: role.description || ''
        });
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading role:', error);
        this.errorMessage = 'Failed to load role. Please try again.';
        this.isLoading = false;
      }
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

    this.isSaving = true;
    this.errorMessage = '';

    const formValue = this.roleForm.value;
    const updateRoleDto: UpdateRoleRequest = {
      name: formValue.name,
      description: formValue.description || undefined
    };

    console.log('Updating role with data:', updateRoleDto);

    this.rolesService.apiRolesIdPut(this.roleId, updateRoleDto).subscribe({
      next: () => {
        console.log('Role updated successfully');
        this.router.navigate(['/roles', this.roleId]);
      },
      error: (error) => {
        console.error('Error updating role:', error);
        this.errorMessage = error.error?.message || 'Failed to update role. Please try again.';
        this.isSaving = false;
      }
    });
  }

  cancel(): void {
    if (this.roleForm.dirty) {
      if (confirm('Are you sure you want to cancel? Any unsaved changes will be lost.')) {
        this.router.navigate(['/roles', this.roleId]);
      }
    } else {
      this.router.navigate(['/roles', this.roleId]);
    }
  }

  getRoleIcon(roleName: string | null | undefined): string {
    const name = roleName?.toLowerCase() || '';

    if (name.includes('admin') || name.includes('super')) {
      return '👑';
    } else if (name.includes('manager') || name.includes('lead')) {
      return '👔';
    } else if (name.includes('user') || name.includes('member')) {
      return '👤';
    } else if (name.includes('guest') || name.includes('viewer')) {
      return '👁️';
    }

    return '🎭';
  }
}
