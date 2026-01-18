import { Routes } from '@angular/router';

export const ROLES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./components/role-list.component').then(m => m.RoleListComponent)
  },
  {
    path: 'new',
    loadComponent: () => import('./components/role-form.component').then(m => m.RoleFormComponent)
  },
  {
    path: ':id',
    loadComponent: () => import('./components/role-detail.component').then(m => m.RoleDetailComponent)
  },
  {
    path: ':id/edit',
    loadComponent: () => import('./components/role-form.component').then(m => m.RoleFormComponent)
  }
];
