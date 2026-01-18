import { Routes } from '@angular/router';

export const USERS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./components/user-list.component').then(m => m.UserListComponent)
  },
  {
    path: 'new',
    loadComponent: () => import('./components/user-form.component').then(m => m.UserFormComponent)
  },
  {
    path: ':id',
    loadComponent: () => import('./components/user-detail.component').then(m => m.UserDetailComponent)
  },
  {
    path: ':id/edit',
    loadComponent: () => import('./components/user-form.component').then(m => m.UserFormComponent)
  }
];
