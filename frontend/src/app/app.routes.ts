import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { permissionGuard } from './core/guards/permission.guard';
import { MainLayoutComponent } from './layout/main-layout.component';

export const routes: Routes = [
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES)
  },
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
        canActivate: [permissionGuard],
        data: { permission: 'dashboard.view' }
      },
      {
        path: 'organizations',
        loadChildren: () => import('./features/organizations/organizations.routes').then(m => m.ORGANIZATIONS_ROUTES),
        canActivate: [permissionGuard],
        data: { permission: 'organizations.view' }
      },
      {
        path: 'users',
        loadChildren: () => import('./features/users/users.routes').then(m => m.USERS_ROUTES),
        canActivate: [permissionGuard],
        data: { permission: 'users.view' }
      },
      {
        path: 'roles',
        loadChildren: () => import('./features/roles/roles.routes').then(m => m.ROLES_ROUTES),
        canActivate: [permissionGuard],
        data: { permission: 'roles.view' }
      },
      {
        path: 'audit-logs',
        loadChildren: () => import('./features/audit/audit.routes').then(m => m.AUDIT_ROUTES),
        canActivate: [permissionGuard],
        data: { permission: 'auditlogs.view' }
      },
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];
