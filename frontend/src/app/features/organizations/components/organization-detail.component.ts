import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTreeModule, MatTreeNestedDataSource } from '@angular/material/tree';
import { NestedTreeControl } from '@angular/cdk/tree';
import { environment } from '../../../../environments/environment';

interface SubOrganization {
  id: string;
  name: string;
  description: string;
  code: string;
  status: number;
  level: number;
  userCount: number;
  children: SubOrganization[];
}

interface OrganizationDetail {
  id: string;
  name: string;
  description: string;
  code: string;
  status: number;
  createdAt: string;
  modifiedAt: string;
  subOrganizations: SubOrganization[];
  stats: {
    totalSubOrganizations: number;
    totalUsers: number;
    activeUsers: number;
    inactiveUsers: number;
  };
}

@Component({
  selector: 'app-organization-detail',
  standalone: true,
  imports: [
    CommonModule, RouterModule,
    MatCardModule, MatButtonModule, MatIconModule,
    MatChipsModule, MatProgressSpinnerModule, MatTreeModule
  ],
  template: `
    @if (loading()) {
      <div class="loading-container">
        <mat-spinner></mat-spinner>
      </div>
    } @else if (org()) {
      <div class="detail-container">
        <div class="header">
          <div class="title-section">
            <h1>{{ org()!.name }}</h1>
            <mat-chip [class]="getStatusClass(org()!.status)">
              {{ getStatusLabel(org()!.status) }}
            </mat-chip>
          </div>
          <div class="actions">
            <button mat-stroked-button routerLink="edit">
              <mat-icon>edit</mat-icon>
              Edit
            </button>
            <button mat-stroked-button routerLink="/organizations">
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
                <dt>Code</dt>
                <dd>{{ org()!.code }}</dd>
                <dt>Description</dt>
                <dd>{{ org()!.description || 'No description' }}</dd>
                <dt>Created</dt>
                <dd>{{ org()!.createdAt | date:'medium' }}</dd>
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
                  <span class="stat-value">{{ org()!.stats.totalSubOrganizations }}</span>
                  <span class="stat-label">Sub-Organizations</span>
                </div>
                <div class="stat">
                  <span class="stat-value">{{ org()!.stats.totalUsers }}</span>
                  <span class="stat-label">Total Users</span>
                </div>
                <div class="stat">
                  <span class="stat-value">{{ org()!.stats.activeUsers }}</span>
                  <span class="stat-label">Active Users</span>
                </div>
              </div>
            </mat-card-content>
          </mat-card>
        </div>

        <mat-card class="tree-card">
          <mat-card-header>
            <mat-card-title>Organization Hierarchy</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            @if (org()!.subOrganizations.length === 0) {
              <p>No sub-organizations</p>
            } @else {
              <mat-tree [dataSource]="treeDataSource" [treeControl]="treeControl">
                <mat-tree-node *matTreeNodeDef="let node" matTreeNodePadding>
                  <button mat-icon-button disabled></button>
                  {{ node.name }} ({{ node.userCount }} users)
                </mat-tree-node>
                <mat-tree-node *matTreeNodeDef="let node; when: hasChild" matTreeNodePadding>
                  <button mat-icon-button matTreeNodeToggle>
                    <mat-icon>
                      {{ treeControl.isExpanded(node) ? 'expand_more' : 'chevron_right' }}
                    </mat-icon>
                  </button>
                  {{ node.name }} ({{ node.userCount }} users)
                </mat-tree-node>
              </mat-tree>
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
    .tree-card { margin-bottom: 24px; }
    .mat-chip.active { background: #e8f5e9; color: #2e7d32; }
    .mat-chip.inactive { background: #ffebee; color: #c62828; }
  `]
})
export class OrganizationDetailComponent implements OnInit {
  private http = inject(HttpClient);
  private route = inject(ActivatedRoute);

  loading = signal(true);
  org = signal<OrganizationDetail | null>(null);

  treeControl = new NestedTreeControl<SubOrganization>(node => node.children);
  treeDataSource = new MatTreeNestedDataSource<SubOrganization>();

  hasChild = (_: number, node: SubOrganization) => !!node.children && node.children.length > 0;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    this.http.get<OrganizationDetail>(`${environment.apiUrl}/organizations/${id}`).subscribe({
      next: (org) => {
        this.org.set(org);
        this.treeDataSource.data = org.subOrganizations;
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  getStatusLabel(status: number): string {
    switch (status) {
      case 1: return 'Active';
      case 2: return 'Inactive';
      case 3: return 'Suspended';
      default: return 'Unknown';
    }
  }

  getStatusClass(status: number): string {
    switch (status) {
      case 1: return 'active';
      case 2: return 'inactive';
      default: return '';
    }
  }
}
