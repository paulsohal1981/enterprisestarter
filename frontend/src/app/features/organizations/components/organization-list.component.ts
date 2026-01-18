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
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FormsModule } from '@angular/forms';
import { environment } from '../../../../environments/environment';

interface Organization {
  id: string;
  name: string;
  description: string;
  code: string;
  status: number;
  subOrganizationCount: number;
  userCount: number;
  createdAt: string;
}

interface PaginatedResult {
  items: Organization[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  pageSize: number;
}

@Component({
  selector: 'app-organization-list',
  standalone: true,
  imports: [
    CommonModule, RouterModule, FormsModule,
    MatTableModule, MatPaginatorModule, MatSortModule,
    MatButtonModule, MatIconModule, MatCardModule,
    MatFormFieldModule, MatInputModule, MatChipsModule,
    MatProgressSpinnerModule
  ],
  template: `
    <div class="page-container">
      <div class="page-header">
        <h1>Organizations</h1>
        <button mat-raised-button color="primary" routerLink="new">
          <mat-icon>add</mat-icon>
          New Organization
        </button>
      </div>

      <mat-card>
        <mat-card-content>
          <div class="filters">
            <mat-form-field appearance="outline">
              <mat-label>Search</mat-label>
              <input matInput [(ngModel)]="searchTerm" (keyup.enter)="loadData()" placeholder="Search by name or code">
              <mat-icon matSuffix>search</mat-icon>
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
                <td mat-cell *matCellDef="let org">{{ org.name }}</td>
              </ng-container>

              <ng-container matColumnDef="code">
                <th mat-header-cell *matHeaderCellDef mat-sort-header>Code</th>
                <td mat-cell *matCellDef="let org">{{ org.code }}</td>
              </ng-container>

              <ng-container matColumnDef="status">
                <th mat-header-cell *matHeaderCellDef mat-sort-header>Status</th>
                <td mat-cell *matCellDef="let org">
                  <mat-chip [class]="getStatusClass(org.status)">
                    {{ getStatusLabel(org.status) }}
                  </mat-chip>
                </td>
              </ng-container>

              <ng-container matColumnDef="subOrganizationCount">
                <th mat-header-cell *matHeaderCellDef>Sub-Orgs</th>
                <td mat-cell *matCellDef="let org">{{ org.subOrganizationCount }}</td>
              </ng-container>

              <ng-container matColumnDef="userCount">
                <th mat-header-cell *matHeaderCellDef>Users</th>
                <td mat-cell *matCellDef="let org">{{ org.userCount }}</td>
              </ng-container>

              <ng-container matColumnDef="actions">
                <th mat-header-cell *matHeaderCellDef>Actions</th>
                <td mat-cell *matCellDef="let org">
                  <button mat-icon-button [routerLink]="[org.id]">
                    <mat-icon>visibility</mat-icon>
                  </button>
                  <button mat-icon-button [routerLink]="[org.id, 'edit']">
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
      max-width: 1200px;
      margin: 0 auto;
    }
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 24px;
    }
    .filters {
      margin-bottom: 16px;
    }
    .loading-container {
      display: flex;
      justify-content: center;
      padding: 48px;
    }
    table {
      width: 100%;
    }
    .mat-chip.active { background: #e8f5e9; color: #2e7d32; }
    .mat-chip.inactive { background: #ffebee; color: #c62828; }
    .mat-chip.suspended { background: #fff3e0; color: #ef6c00; }
  `]
})
export class OrganizationListComponent implements OnInit {
  private http = inject(HttpClient);

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  displayedColumns = ['name', 'code', 'status', 'subOrganizationCount', 'userCount', 'actions'];
  dataSource = new MatTableDataSource<Organization>([]);

  loading = signal(true);
  searchTerm = '';
  pageNumber = 1;
  pageSize = 10;
  totalCount = 0;
  sortBy = 'Name';
  sortDescending = false;

  ngOnInit(): void {
    this.loadData();
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

    this.http.get<PaginatedResult>(`${environment.apiUrl}/organizations`, { params }).subscribe({
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
      case 3: return 'Suspended';
      default: return 'Unknown';
    }
  }

  getStatusClass(status: number): string {
    switch (status) {
      case 1: return 'active';
      case 2: return 'inactive';
      case 3: return 'suspended';
      default: return '';
    }
  }
}
