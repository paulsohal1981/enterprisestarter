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
import { MatExpansionModule } from '@angular/material/expansion';
import { environment } from '../../../../environments/environment';

interface Permission {
  id: string;
  name: string;
  description: string;
  category: string;
}

interface RoleDetail {
  id: string;
  name: string;
  description: string;
  isSystemRole: boolean;
  permissions: Permission[];
  userCount: number;
  createdAt: string;
  modifiedAt: string;
}

interface GroupedPermissions {
  [category: string]: Permission[];
}

@Component({
  selector: 'app-role-detail',
  standalone: true,
  imports: [
    CommonModule, RouterModule,
    MatCardModule, MatButtonModule, MatIconModule,
    MatChipsModule, MatProgressSpinnerModule,
    MatListModule, MatExpansionModule
  ],
  template: `
    @if (loading()) {
      <div class="loading-container">
        <mat-spinner></mat-spinner>
      </div>
    } @else if (role()) {
      <div class="detail-container">
        <div class="header">
          <div class="title-section">
            <h1>{{ role()!.name }}</h1>
            @if (role()!.isSystemRole) {
              <mat-chip class="system-chip">System Role</mat-chip>
            }
          </div>
          <div class="actions">
            @if (!role()!.isSystemRole) {
              <button mat-stroked-button routerLink="edit">
                <mat-icon>edit</mat-icon>
                Edit
              </button>
              <button mat-stroked-button color="warn" (click)="deleteRole()">
                <mat-icon>delete</mat-icon>
                Delete
              </button>
            }
            <button mat-stroked-button routerLink="/roles">
              <mat-icon>arrow_back</mat-icon>
              Back
            </button>
          </div>
        </div>

        <div class="content-grid">
          <mat-card>
            <mat-card-header>
              <mat-card-title>Details</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <dl>
                <dt>Description</dt>
                <dd>{{ role()!.description || 'No description' }}</dd>
                <dt>Users</dt>
                <dd>{{ role()!.userCount }} user(s) assigned</dd>
                <dt>Created</dt>
                <dd>{{ role()!.createdAt | date:'medium' }}</dd>
                <dt>Last Modified</dt>
                <dd>{{ role()!.modifiedAt | date:'medium' }}</dd>
              </dl>
            </mat-card-content>
          </mat-card>

          <mat-card>
            <mat-card-header>
              <mat-card-title>Statistics</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <div class="stats-grid">
                <div class="stat">
                  <span class="stat-value">{{ role()!.permissions.length }}</span>
                  <span class="stat-label">Permissions</span>
                </div>
                <div class="stat">
                  <span class="stat-value">{{ role()!.userCount }}</span>
                  <span class="stat-label">Users</span>
                </div>
                <div class="stat">
                  <span class="stat-value">{{ getCategories().length }}</span>
                  <span class="stat-label">Categories</span>
                </div>
              </div>
            </mat-card-content>
          </mat-card>
        </div>

        <mat-card>
          <mat-card-header>
            <mat-card-title>Permissions ({{ role()!.permissions.length }})</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            @if (role()!.permissions.length === 0) {
              <p>No permissions assigned</p>
            } @else {
              <mat-accordion>
                @for (category of getCategories(); track category) {
                  <mat-expansion-panel>
                    <mat-expansion-panel-header>
                      <mat-panel-title>{{ category }}</mat-panel-title>
                      <mat-panel-description>
                        {{ groupedPermissions()[category].length }} permission(s)
                      </mat-panel-description>
                    </mat-expansion-panel-header>
                    <mat-list>
                      @for (permission of groupedPermissions()[category]; track permission.id) {
                        <mat-list-item>
                          <mat-icon matListItemIcon>check_circle</mat-icon>
                          <span matListItemTitle>{{ permission.name }}</span>
                          <span matListItemLine>{{ permission.description }}</span>
                        </mat-list-item>
                      }
                    </mat-list>
                  </mat-expansion-panel>
                }
              </mat-accordion>
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
    }
    .content-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 24px;
      margin-bottom: 24px;
    }
    dl {
      display: grid;
      grid-template-columns: 120px 1fr;
      gap: 8px;
    }
    dt { font-weight: 500; color: #666; }
    dd { margin: 0; }
    .stats-grid {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 16px;
      text-align: center;
    }
    .stat-value { font-size: 24px; font-weight: bold; display: block; }
    .stat-label { color: #666; }
    .system-chip {
      background: #e3f2fd;
      color: #1976d2;
    }
  `]
})
export class RoleDetailComponent implements OnInit {
  private http = inject(HttpClient);
  private route = inject(ActivatedRoute);

  loading = signal(true);
  role = signal<RoleDetail | null>(null);
  groupedPermissions = signal<GroupedPermissions>({});

  ngOnInit(): void {
    this.loadRole();
  }

  loadRole(): void {
    const id = this.route.snapshot.paramMap.get('id');
    this.http.get<RoleDetail>(`${environment.apiUrl}/roles/${id}`).subscribe({
      next: (role) => {
        this.role.set(role);
        this.groupPermissions(role.permissions);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  groupPermissions(permissions: Permission[]): void {
    const grouped: GroupedPermissions = {};
    permissions.forEach(p => {
      if (!grouped[p.category]) {
        grouped[p.category] = [];
      }
      grouped[p.category].push(p);
    });
    this.groupedPermissions.set(grouped);
  }

  getCategories(): string[] {
    return Object.keys(this.groupedPermissions()).sort();
  }

  deleteRole(): void {
    if (!confirm('Are you sure you want to delete this role?')) {
      return;
    }

    const id = this.role()!.id;
    this.http.delete(`${environment.apiUrl}/roles/${id}`).subscribe({
      next: () => {
        window.location.href = '/roles';
      }
    });
  }
}
