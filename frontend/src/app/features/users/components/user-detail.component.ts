import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { environment } from '../../../../environments/environment';

interface UserDetail {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  status: number;
  organizationId: string;
  organizationName: string;
  subOrganizationId: string | null;
  subOrganizationName: string | null;
  roles: { id: string; name: string }[];
  mustChangePassword: boolean;
  failedLoginAttempts: number;
  lastLoginAt: string | null;
  createdAt: string;
  modifiedAt: string;
}

@Component({
  selector: 'app-user-detail',
  standalone: true,
  imports: [
    CommonModule, RouterModule,
    MatCardModule, MatButtonModule, MatIconModule,
    MatChipsModule, MatProgressSpinnerModule,
    MatListModule, MatDividerModule, MatDialogModule
  ],
  template: `
    @if (loading()) {
      <div class="loading-container">
        <mat-spinner></mat-spinner>
      </div>
    } @else if (user()) {
      <div class="detail-container">
        <div class="header">
          <div class="title-section">
            <h1>{{ user()!.firstName }} {{ user()!.lastName }}</h1>
            <mat-chip [class]="getStatusClass(user()!.status)">
              {{ getStatusLabel(user()!.status) }}
            </mat-chip>
          </div>
          <div class="actions">
            <button mat-stroked-button routerLink="edit">
              <mat-icon>edit</mat-icon>
              Edit
            </button>
            @if (user()!.status === 1) {
              <button mat-stroked-button color="warn" (click)="deactivateUser()">
                <mat-icon>block</mat-icon>
                Deactivate
              </button>
            } @else if (user()!.status === 2) {
              <button mat-stroked-button color="primary" (click)="activateUser()">
                <mat-icon>check_circle</mat-icon>
                Activate
              </button>
            }
            @if (user()!.status === 3) {
              <button mat-stroked-button color="primary" (click)="unlockUser()">
                <mat-icon>lock_open</mat-icon>
                Unlock
              </button>
            }
            <button mat-stroked-button (click)="resetPassword()">
              <mat-icon>vpn_key</mat-icon>
              Reset Password
            </button>
            <button mat-stroked-button routerLink="/users">
              <mat-icon>arrow_back</mat-icon>
              Back
            </button>
          </div>
        </div>

        <div class="content-grid">
          <mat-card>
            <mat-card-header>
              <mat-card-title>User Information</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <dl>
                <dt>Email</dt>
                <dd>{{ user()!.email }}</dd>
                <dt>Organization</dt>
                <dd>{{ user()!.organizationName }}</dd>
                @if (user()!.subOrganizationName) {
                  <dt>Sub-Organization</dt>
                  <dd>{{ user()!.subOrganizationName }}</dd>
                }
                <dt>Created</dt>
                <dd>{{ user()!.createdAt | date:'medium' }}</dd>
                <dt>Last Modified</dt>
                <dd>{{ user()!.modifiedAt | date:'medium' }}</dd>
              </dl>
            </mat-card-content>
          </mat-card>

          <mat-card>
            <mat-card-header>
              <mat-card-title>Security</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <dl>
                <dt>Last Login</dt>
                <dd>{{ user()!.lastLoginAt ? (user()!.lastLoginAt | date:'medium') : 'Never' }}</dd>
                <dt>Failed Attempts</dt>
                <dd>{{ user()!.failedLoginAttempts }}</dd>
                <dt>Must Change Password</dt>
                <dd>{{ user()!.mustChangePassword ? 'Yes' : 'No' }}</dd>
              </dl>
            </mat-card-content>
          </mat-card>
        </div>

        <mat-card>
          <mat-card-header>
            <mat-card-title>Assigned Roles</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            @if (user()!.roles.length === 0) {
              <p>No roles assigned</p>
            } @else {
              <mat-list>
                @for (role of user()!.roles; track role.id) {
                  <mat-list-item>
                    <mat-icon matListItemIcon>security</mat-icon>
                    <span matListItemTitle>{{ role.name }}</span>
                  </mat-list-item>
                }
              </mat-list>
            }
          </mat-card-content>
        </mat-card>
      </div>
    }
  `,
  styles: [`
    .loading-container, .detail-container {
      max-width: 1200px;
      margin: 0 auto;
    }
    .loading-container {
      display: flex;
      justify-content: center;
      padding: 48px;
    }
    .header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 24px;
      flex-wrap: wrap;
      gap: 16px;
    }
    .title-section {
      display: flex;
      align-items: center;
      gap: 16px;
    }
    .actions {
      display: flex;
      gap: 8px;
      flex-wrap: wrap;
    }
    .content-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 24px;
      margin-bottom: 24px;
    }
    dl {
      display: grid;
      grid-template-columns: 140px 1fr;
      gap: 8px;
    }
    dt { font-weight: 500; color: #666; }
    dd { margin: 0; }
    .mat-chip.active { background: #e8f5e9; color: #2e7d32; }
    .mat-chip.inactive { background: #ffebee; color: #c62828; }
    .mat-chip.locked { background: #fff3e0; color: #ef6c00; }
  `]
})
export class UserDetailComponent implements OnInit {
  private http = inject(HttpClient);
  private route = inject(ActivatedRoute);
  private dialog = inject(MatDialog);

  loading = signal(true);
  user = signal<UserDetail | null>(null);

  ngOnInit(): void {
    this.loadUser();
  }

  loadUser(): void {
    const id = this.route.snapshot.paramMap.get('id');
    this.http.get<UserDetail>(`${environment.apiUrl}/users/${id}`).subscribe({
      next: (user) => {
        this.user.set(user);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  deactivateUser(): void {
    const id = this.user()!.id;
    this.http.post(`${environment.apiUrl}/users/${id}/deactivate`, {}).subscribe({
      next: () => {
        this.loadUser();
      }
    });
  }

  activateUser(): void {
    const id = this.user()!.id;
    this.http.post(`${environment.apiUrl}/users/${id}/activate`, {}).subscribe({
      next: () => {
        this.loadUser();
      }
    });
  }

  unlockUser(): void {
    const id = this.user()!.id;
    this.http.post(`${environment.apiUrl}/users/${id}/unlock`, {}).subscribe({
      next: () => {
        this.loadUser();
      }
    });
  }

  resetPassword(): void {
    const id = this.user()!.id;
    this.http.post(`${environment.apiUrl}/users/${id}/reset-password`, {}).subscribe({
      next: () => {
        alert('Password reset email has been sent.');
      }
    });
  }

  getStatusLabel(status: number): string {
    switch (status) {
      case 1: return 'Active';
      case 2: return 'Inactive';
      case 3: return 'Locked';
      default: return 'Unknown';
    }
  }

  getStatusClass(status: number): string {
    switch (status) {
      case 1: return 'active';
      case 2: return 'inactive';
      case 3: return 'locked';
      default: return '';
    }
  }
}
