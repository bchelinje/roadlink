import { Routes } from '@angular/router';

export const JOBS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./job-list/job-list.component').then(m => m.JobListComponent)
  },
  {
    path: 'create',
    loadComponent: () => import('./job-form/job-form.component').then(m => m.JobFormComponent)
  },
  {
    path: 'bulk-create',
    loadComponent: () => import('./bulk-job-creation.component').then(m => m.BulkJobCreationComponent)
  },
  {
    path: ':id/edit',
    loadComponent: () => import('./job-form/job-form.component').then(m => m.JobFormComponent)
  },
  {
    path: ':id/stops',
    loadComponent: () => import('./job-stops.component').then(m => m.JobStopsComponent)
  },
  {
    path: ':id',
    loadComponent: () => import('./job-detail/job-detail.component').then(m => m.JobDetailComponent)
  }
];
