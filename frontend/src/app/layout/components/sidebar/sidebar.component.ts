import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { AuthService } from '../../../core/auth/auth.service';

interface NavItem {
  label: string;
  icon: string;
  route: string;
  permission?: string;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule, MatListModule, MatIconModule, MatDividerModule],
  template: `
    <mat-nav-list>
      @for (item of filteredNavItems; track item.route) {
        <a mat-list-item [routerLink]="item.route" routerLinkActive="active">
          <mat-icon matListItemIcon>{{ item.icon }}</mat-icon>
          <span matListItemTitle>{{ item.label }}</span>
        </a>
      }
    </mat-nav-list>
  `,
  styles: [`
    mat-nav-list {
      padding-top: 0;
    }
    a.active {
      background-color: rgba(0, 0, 0, 0.08);
    }
    mat-icon {
      margin-right: 16px;
    }
  `]
})
export class SidebarComponent {
  private authService = inject(AuthService);

  navItems: NavItem[] = [
    { label: 'Dashboard', icon: 'dashboard', route: '/dashboard', permission: 'dashboard.view' },
    { label: 'Organizations', icon: 'business', route: '/organizations', permission: 'organizations.view' },
    { label: 'Users', icon: 'people', route: '/users', permission: 'users.view' },
    { label: 'Roles', icon: 'admin_panel_settings', route: '/roles', permission: 'roles.view' },
    { label: 'Audit Logs', icon: 'history', route: '/audit-logs', permission: 'auditlogs.view' }
  ];

  get filteredNavItems(): NavItem[] {
    return this.navItems.filter(item =>
      !item.permission || this.authService.hasPermission(item.permission)
    );
  }
}
