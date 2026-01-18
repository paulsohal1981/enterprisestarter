import { Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpParams } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginatorModule, MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatSortModule, MatSort, Sort } from '@angular/material/sort';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FormsModule } from '@angular/forms';
import { environment } from '../../../../environments/environment';

interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  status: number;
  organizationName: string;
  subOrganizationName: string | null;
  roles: string[];
  lastLoginAt: string | null;
  createdAt: string;
}

interface PaginatedResult {
  items: User[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  pageSize: number;
}

interface Organization {
  id: string;
  name: string;
}

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [
    CommonModule, RouterModule, FormsModule,
    MatTableModule, MatPaginatorModule, MatSortModule,
    MatButtonModule, MatIconModule, MatCardModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatChipsModule, MatProgressSpinnerModule
  ],
  template: `
    <div class="page-container">
      <div class="page-header">
        <h1>Users</h1>
        <button mat-raised-button color="primary" routerLink="new">
          <mat-icon>add</mat-icon>
          New User
        </button>
      </div>

      <mat-card>
        <mat-card-content>
          <div class="filters">
            <mat-form-field appearance="outline">
              <mat-label>Search</mat-label>
              <input matInput [(ngModel)]="searchTerm" (keyup.enter)="loadData()" placeholder="Search by name or email">
              <mat-icon matSuffix>search</mat-icon>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Organization</mat-label>
              <mat-select [(ngModel)]="organizationId" (selectionChange)="loadData()">
                <mat-option [value]="null">All Organizations</mat-option>
                @for (org of organizations(); track org.id) {
                  <mat-option [value]="org.id">{{ org.name }}</mat-option>
                }
              </mat-select>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Status</mat-label>
              <mat-select [(ngModel)]="status" (selectionChange)="loadData()">
                <mat-option [value]="null">All Statuses</mat-option>
                <mat-option [value]="1">Active</mat-option>
                <mat-option [value]="2">Inactive</mat-option>
                <mat-option [value]="3">Locked</mat-option>
              </mat-select>
            </mat-form-field>
          </div>

          @if (loading()) {
            <div class="loading-container">
              <mat-spinner></mat-spinner>
            </div>
          } @else {
            <table mat-table [dataSource]="dataSource" matSort (matSortChange)="onSort($event)">
              <ng-container matColumnDef="name">
                <th mat-header-cell *matHeaderCellDef mat-sort-header>Name</th>
                <td mat-cell *matCellDef="let user">{{ user.firstName }} {{ user.lastName }}</td>
              </ng-container>

              <ng-container matColumnDef="email">
                <th mat-header-cell *matHeaderCellDef mat-sort-header>Email</th>
                <td mat-cell *matCellDef="let user">{{ user.email }}</td>
              </ng-container>

              <ng-container matColumnDef="organization">
                <th mat-header-cell *matHeaderCellDef>Organization</th>
                <td mat-cell *matCellDef="let user">
                  {{ user.organizationName }}
                  @if (user.subOrganizationName) {
                    <span class="sub-org"> / {{ user.subOrganizationName }}</span>
                  }
                </td>
              </ng-container>

              <ng-container matColumnDef="status">
                <th mat-header-cell *matHeaderCellDef mat-sort-header>Status</th>
                <td mat-cell *matCellDef="let user">
                  <mat-chip [class]="getStatusClass(user.status)">
                    {{ getStatusLabel(user.status) }}
                  </mat-chip>
                </td>
              </ng-container>

              <ng-container matColumnDef="roles">
                <th mat-header-cell *matHeaderCellDef>Roles</th>
                <td mat-cell *matCellDef="let user">
                  @for (role of user.roles; track role) {
                    <mat-chip class="role-chip">{{ role }}</mat-chip>
                  }
                </td>
              </ng-container>

              <ng-container matColumnDef="lastLogin">
                <th mat-header-cell *matHeaderCellDef mat-sort-header>Last Login</th>
                <td mat-cell *matCellDef="let user">
                  {{ user.lastLoginAt ? (user.lastLoginAt | date:'short') : 'Never' }}
                </td>
              </ng-container>

              <ng-container matColumnDef="actions">
                <th mat-header-cell *matHeaderCellDef>Actions</th>
                <td mat-cell *matCellDef="let user">
                  <button mat-icon-button [routerLink]="[user.id]">
                    <mat-icon>visibility</mat-icon>
                  </button>
                  <button mat-icon-button [routerLink]="[user.id, 'edit']">
                    <mat-icon>edit</mat-icon>
                  </button>
                </td>
              </ng-container>

              <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: displayedColumns"></tr>
            </table>

            <mat-paginator
              [length]="totalCount"
              [pageSize]="pageSize"
              [pageIndex]="pageNumber - 1"
              [pageSizeOptions]="[10, 25, 50]"
              (page)="onPageChange($event)">
            </mat-paginator>
          }
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .page-container {
      max-width: 1400px;
      margin: 0 auto;
    }
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 24px;
    }
    .filters {
      display: flex;
      gap: 16px;
      margin-bottom: 16px;
      flex-wrap: wrap;
    }
    .loading-container {
      display: flex;
      justify-content: center;
      padding: 48px;
    }
    table {
      width: 100%;
    }
    .sub-org {
      color: #666;
      font-size: 0.85em;
    }
    .mat-chip.active { background: #e8f5e9; color: #2e7d32; }
    .mat-chip.inactive { background: #ffebee; color: #c62828; }
    .mat-chip.locked { background: #fff3e0; color: #ef6c00; }
    .role-chip {
      font-size: 0.75em;
      margin: 2px;
    }
  `]
})
export class UserListComponent implements OnInit {
  private http = inject(HttpClient);

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  displayedColumns = ['name', 'email', 'organization', 'status', 'roles', 'lastLogin', 'actions'];
  dataSource = new MatTableDataSource<User>([]);

  loading = signal(true);
  organizations = signal<Organization[]>([]);
  searchTerm = '';
  organizationId: string | null = null;
  status: number | null = null;
  pageNumber = 1;
  pageSize = 10;
  totalCount = 0;
  sortBy = 'LastName';
  sortDescending = false;

  ngOnInit(): void {
    this.loadOrganizations();
    this.loadData();
  }

  loadOrganizations(): void {
    this.http.get<{ items: Organization[] }>(`${environment.apiUrl}/organizations?pageSize=1000`).subscribe({
      next: (result) => {
        this.organizations.set(result.items);
      }
    });
  }

  loadData(): void {
    this.loading.set(true);
    let params = new HttpParams()
      .set('pageNumber', this.pageNumber)
      .set('pageSize', this.pageSize)
      .set('sortBy', this.sortBy)
      .set('sortDescending', this.sortDescending);

    if (this.searchTerm) {
      params = params.set('searchTerm', this.searchTerm);
    }
    if (this.organizationId) {
      params = params.set('organizationId', this.organizationId);
    }
    if (this.status !== null) {
      params = params.set('status', this.status);
    }

    this.http.get<PaginatedResult>(`${environment.apiUrl}/users`, { params }).subscribe({
      next: (result) => {
        this.dataSource.data = result.items;
        this.totalCount = result.totalCount;
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  onPageChange(event: PageEvent): void {
    this.pageNumber = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadData();
  }

  onSort(sort: Sort): void {
    this.sortBy = sort.active;
    this.sortDescending = sort.direction === 'desc';
    this.loadData();
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
