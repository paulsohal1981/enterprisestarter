import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { environment } from '../../../environments/environment';

interface DashboardData {
  totalOrganizations: number;
  activeOrganizations: number;
  totalSubOrganizations: number;
  totalUsers: number;
  activeUsers: number;
  inactiveUsers: number;
  lockedUsers: number;
  totalRoles: number;
  systemRoles: number;
  customRoles: number;
  recentActivity: RecentActivity[];
  usersByOrganization: UsersByOrg[];
}

interface RecentActivity {
  id: string;
  entityType: string;
  action: string;
  userEmail: string;
  createdAt: string;
}

interface UsersByOrg {
  organizationId: string;
  organizationName: string;
  userCount: number;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule, MatProgressSpinnerModule, MatTableModule],
  template: `
    @if (loading()) {
      <div class="loading-container">
        <mat-spinner></mat-spinner>
      </div>
    } @else if (data()) {
      <div class="dashboard-container">
        <h1>Dashboard</h1>

        <div class="stats-grid">
          <mat-card class="stat-card">
            <mat-card-content>
              <div class="stat-icon organizations">
                <mat-icon>business</mat-icon>
              </div>
              <div class="stat-info">
                <div class="stat-value">{{ data()!.totalOrganizations }}</div>
                <div class="stat-label">Organizations</div>
              </div>
            </mat-card-content>
          </mat-card>

          <mat-card class="stat-card">
            <mat-card-content>
              <div class="stat-icon users">
                <mat-icon>people</mat-icon>
              </div>
              <div class="stat-info">
                <div class="stat-value">{{ data()!.totalUsers }}</div>
                <div class="stat-label">Total Users</div>
              </div>
            </mat-card-content>
          </mat-card>

          <mat-card class="stat-card">
            <mat-card-content>
              <div class="stat-icon active">
                <mat-icon>check_circle</mat-icon>
              </div>
              <div class="stat-info">
                <div class="stat-value">{{ data()!.activeUsers }}</div>
                <div class="stat-label">Active Users</div>
              </div>
            </mat-card-content>
          </mat-card>

          <mat-card class="stat-card">
            <mat-card-content>
              <div class="stat-icon roles">
                <mat-icon>admin_panel_settings</mat-icon>
              </div>
              <div class="stat-info">
                <div class="stat-value">{{ data()!.totalRoles }}</div>
                <div class="stat-label">Roles</div>
              </div>
            </mat-card-content>
          </mat-card>
        </div>

        <div class="dashboard-grid">
          <mat-card>
            <mat-card-header>
              <mat-card-title>Recent Activity</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <table mat-table [dataSource]="data()!.recentActivity">
                <ng-container matColumnDef="entityType">
                  <th mat-header-cell *matHeaderCellDef>Entity</th>
                  <td mat-cell *matCellDef="let activity">{{ activity.entityType }}</td>
                </ng-container>
                <ng-container matColumnDef="action">
                  <th mat-header-cell *matHeaderCellDef>Action</th>
                  <td mat-cell *matCellDef="let activity">{{ activity.action }}</td>
                </ng-container>
                <ng-container matColumnDef="userEmail">
                  <th mat-header-cell *matHeaderCellDef>User</th>
                  <td mat-cell *matCellDef="let activity">{{ activity.userEmail }}</td>
                </ng-container>
                <ng-container matColumnDef="createdAt">
                  <th mat-header-cell *matHeaderCellDef>Time</th>
                  <td mat-cell *matCellDef="let activity">{{ activity.createdAt | date:'short' }}</td>
                </ng-container>
                <tr mat-header-row *matHeaderRowDef="['entityType', 'action', 'userEmail', 'createdAt']"></tr>
                <tr mat-row *matRowDef="let row; columns: ['entityType', 'action', 'userEmail', 'createdAt']"></tr>
              </table>
            </mat-card-content>
          </mat-card>

          <mat-card>
            <mat-card-header>
              <mat-card-title>Users by Organization</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <table mat-table [dataSource]="data()!.usersByOrganization">
                <ng-container matColumnDef="organizationName">
                  <th mat-header-cell *matHeaderCellDef>Organization</th>
                  <td mat-cell *matCellDef="let org">{{ org.organizationName }}</td>
                </ng-container>
                <ng-container matColumnDef="userCount">
                  <th mat-header-cell *matHeaderCellDef>Users</th>
                  <td mat-cell *matCellDef="let org">{{ org.userCount }}</td>
                </ng-container>
                <tr mat-header-row *matHeaderRowDef="['organizationName', 'userCount']"></tr>
                <tr mat-row *matRowDef="let row; columns: ['organizationName', 'userCount']"></tr>
              </table>
            </mat-card-content>
          </mat-card>
        </div>
      </div>
    }
  `,
  styles: [`
    .loading-container {
      display: flex;
      justify-content: center;
      padding: 48px;
    }
    .dashboard-container {
      max-width: 1200px;
      margin: 0 auto;
    }
    h1 {
      margin-bottom: 24px;
    }
    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 16px;
      margin-bottom: 24px;
    }
    .stat-card mat-card-content {
      display: flex;
      align-items: center;
      gap: 16px;
      padding: 16px;
    }
    .stat-icon {
      width: 48px;
      height: 48px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
    }
    .stat-icon.organizations { background: #e3f2fd; color: #1976d2; }
    .stat-icon.users { background: #fce4ec; color: #c2185b; }
    .stat-icon.active { background: #e8f5e9; color: #388e3c; }
    .stat-icon.roles { background: #fff3e0; color: #f57c00; }
    .stat-icon mat-icon { font-size: 24px; }
    .stat-value {
      font-size: 24px;
      font-weight: bold;
    }
    .stat-label {
      color: #666;
    }
    .dashboard-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
      gap: 24px;
    }
    table {
      width: 100%;
    }
  `]
})
export class DashboardComponent implements OnInit {
  private http = inject(HttpClient);

  loading = signal(true);
  data = signal<DashboardData | null>(null);

  ngOnInit(): void {
    this.http.get<DashboardData>(`${environment.apiUrl}/dashboard`).subscribe({
      next: (data) => {
        this.data.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }
}
