import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpParams } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { environment } from '../../../../environments/environment';

interface Role {
  id: string;
  name: string;
  description: string;
  isSystemRole: boolean;
  permissionCount: number;
  userCount: number;
  createdAt: string;
}

interface PaginatedResult {
  items: Role[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  pageSize: number;
}

@Component({
  selector: 'app-role-list',
  standalone: true,
  imports: [
    CommonModule, RouterModule,
    MatTableModule, MatPaginatorModule, MatSortModule,
    MatButtonModule, MatIconModule, MatCardModule,
    MatChipsModule, MatProgressSpinnerModule
  ],
  template: `
    <div class="page-container">
      <div class="page-header">
        <h1>Roles</h1>
        <button mat-raised-button color="primary" routerLink="new">
          <mat-icon>add</mat-icon>
          New Role
        </button>
      </div>

      <mat-card>
        <mat-card-content>
          @if (loading()) {
            <div class="loading-container">
              <mat-spinner></mat-spinner>
            </div>
          } @else {
            <table mat-table [dataSource]="dataSource" matSort (matSortChange)="onSort($event)">
              <ng-container matColumnDef="name">
                <th mat-header-cell *matHeaderCellDef mat-sort-header>Name</th>
                <td mat-cell *matCellDef="let role">
                  {{ role.name }}
                  @if (role.isSystemRole) {
                    <mat-chip class="system-chip">System</mat-chip>
                  }
                </td>
              </ng-container>

              <ng-container matColumnDef="description">
                <th mat-header-cell *matHeaderCellDef>Description</th>
                <td mat-cell *matCellDef="let role">{{ role.description }}</td>
              </ng-container>

              <ng-container matColumnDef="permissionCount">
                <th mat-header-cell *matHeaderCellDef>Permissions</th>
                <td mat-cell *matCellDef="let role">{{ role.permissionCount }}</td>
              </ng-container>

              <ng-container matColumnDef="userCount">
                <th mat-header-cell *matHeaderCellDef>Users</th>
                <td mat-cell *matCellDef="let role">{{ role.userCount }}</td>
              </ng-container>

              <ng-container matColumnDef="actions">
                <th mat-header-cell *matHeaderCellDef>Actions</th>
                <td mat-cell *matCellDef="let role">
                  <button mat-icon-button [routerLink]="[role.id]">
                    <mat-icon>visibility</mat-icon>
                  </button>
                  @if (!role.isSystemRole) {
                    <button mat-icon-button [routerLink]="[role.id, 'edit']">
                      <mat-icon>edit</mat-icon>
                    </button>
                  }
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
    .loading-container {
      display: flex;
      justify-content: center;
      padding: 48px;
    }
    table {
      width: 100%;
    }
    .system-chip {
      background: #e3f2fd;
      color: #1976d2;
      font-size: 0.7em;
      margin-left: 8px;
    }
  `]
})
export class RoleListComponent implements OnInit {
  private http = inject(HttpClient);

  displayedColumns = ['name', 'description', 'permissionCount', 'userCount', 'actions'];
  dataSource = new MatTableDataSource<Role>([]);

  loading = signal(true);
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
    const params = new HttpParams()
      .set('pageNumber', this.pageNumber)
      .set('pageSize', this.pageSize)
      .set('sortBy', this.sortBy)
      .set('sortDescending', this.sortDescending);

    this.http.get<PaginatedResult>(`${environment.apiUrl}/roles`, { params }).subscribe({
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
}
