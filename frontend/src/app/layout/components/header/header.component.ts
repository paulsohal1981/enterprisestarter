import { Component, inject, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule, MatToolbarModule, MatButtonModule, MatIconModule, MatMenuModule, MatDividerModule],
  template: `
    <mat-toolbar color="primary">
      <button mat-icon-button (click)="menuToggle.emit()">
        <mat-icon>menu</mat-icon>
      </button>
      <span class="title">Organization Management</span>
      <span class="spacer"></span>
      @if (currentUser$ | async; as user) {
        <button mat-button [matMenuTriggerFor]="userMenu">
          <mat-icon>account_circle</mat-icon>
          <span class="user-name">{{ user.firstName }} {{ user.lastName }}</span>
          <mat-icon>arrow_drop_down</mat-icon>
        </button>
        <mat-menu #userMenu="matMenu">
          <button mat-menu-item disabled>
            <mat-icon>email</mat-icon>
            <span>{{ user.email }}</span>
          </button>
          <button mat-menu-item routerLink="/profile">
            <mat-icon>person</mat-icon>
            <span>Profile</span>
          </button>
          <button mat-menu-item routerLink="/change-password">
            <mat-icon>lock</mat-icon>
            <span>Change Password</span>
          </button>
          <mat-divider></mat-divider>
          <button mat-menu-item (click)="onLogout()">
            <mat-icon>logout</mat-icon>
            <span>Logout</span>
          </button>
        </mat-menu>
      }
    </mat-toolbar>
  `,
  styles: [`
    mat-toolbar {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      z-index: 1000;
    }
    .title {
      margin-left: 16px;
    }
    .spacer {
      flex: 1;
    }
    .user-name {
      margin: 0 8px;
    }
    @media (max-width: 600px) {
      .user-name {
        display: none;
      }
    }
  `]
})
export class HeaderComponent {
  private authService = inject(AuthService);
  menuToggle = output<void>();
  currentUser$ = this.authService.currentUser$;

  onLogout(): void {
    this.authService.logout().subscribe();
  }
}
