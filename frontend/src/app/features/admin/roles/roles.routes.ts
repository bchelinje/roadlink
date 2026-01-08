import { Routes } from '@angular/router';

export const ROLES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./role-list/role-list.component').then(m => m.RoleListComponent)
  },
  {
    path: 'create',
    loadComponent: () => import('./role-create/role-create.component').then(m => m.RoleCreateComponent)
  },
  {
    path: ':id',
    loadComponent: () => import('./role-detail/role-detail.component').then(m => m.RoleDetailComponent)
  },
  {
    path: ':id/edit',
    loadComponent: () => import('./role-edit/role-edit.component').then(m => m.RoleEditComponent)
  }
];
