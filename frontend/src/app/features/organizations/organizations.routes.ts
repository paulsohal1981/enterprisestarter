import { Routes } from '@angular/router';

export const ORGANIZATIONS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./components/organization-list.component').then(m => m.OrganizationListComponent)
  },
  {
    path: 'new',
    loadComponent: () => import('./components/organization-form.component').then(m => m.OrganizationFormComponent)
  },
  {
    path: ':id',
    loadComponent: () => import('./components/organization-detail.component').then(m => m.OrganizationDetailComponent)
  },
  {
    path: ':id/edit',
    loadComponent: () => import('./components/organization-form.component').then(m => m.OrganizationFormComponent)
  }
];
