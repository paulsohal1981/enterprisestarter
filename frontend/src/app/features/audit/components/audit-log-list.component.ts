import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatExpansionModule } from '@angular/material/expansion';
import { FormsModule } from '@angular/forms';
import { environment } from '../../../../environments/environment';

interface AuditLog {
  id: string;
  action: string;
  entityType: string;
  entityId: string;
  entityName: string;
  userId: string;
  userEmail: string;
  changes: string;
  ipAddress: string;
  createdAt: string;
}

interface PaginatedResult {
  items: AuditLog[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  pageSize: number;
}

@Component({
  selector: 'app-audit-log-list',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatTableModule, MatPaginatorModule, MatSortModule,
    MatButtonModule, MatIconModule, MatCardModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatChipsModule, MatProgressSpinnerModule,
    MatDatepickerModule, MatNativeDateModule, MatExpansionModule
  ],
  template: `
    <div class="page-container">
      <div class="page-header">
        <h1>Audit Logs</h1>
      </div>

      <mat-card>
        <mat-card-content>
          <div class="filters">
            <mat-form-field appearance="outline">
              <mat-label>Entity Type</mat-label>
              <mat-select [(ngModel)]="entityType" (selectionChange)="loadData()">
                <mat-option [value]="null">All Types</mat-option>
                <mat-option value="Organization">Organization</mat-option>
                <mat-option value="SubOrganization">Sub-Organization</mat-option>
                <mat-option value="User">User</mat-option>
                <mat-option value="Role">Role</mat-option>
              </mat-select>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Action</mat-label>
              <mat-select [(ngModel)]="action" (selectionChange)="loadData()">
                <mat-option [value]="null">All Actions</mat-option>
                <mat-option value="Create">Create</mat-option>
                <mat-option value="Update">Update</mat-option>
                <mat-option value="Delete">Delete</mat-option>
                <mat-option value="Activate">Activate</mat-option>
                <mat-option value="Deactivate">Deactivate</mat-option>
                <mat-option value="Login">Login</mat-option>
                <mat-option value="Logout">Logout</mat-option>
                <mat-option value="PasswordChange">Password Change</mat-option>
              </mat-select>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Start Date</mat-label>
              <input matInput [matDatepicker]="startPicker" [(ngModel)]="startDate" (dateChange)="loadData()">
              <mat-datepicker-toggle matSuffix [for]="startPicker"></mat-datepicker-toggle>
              <mat-datepicker #startPicker></mat-datepicker>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>End Date</mat-label>
              <input matInput [matDatepicker]="endPicker" [(ngModel)]="endDate" (dateChange)="loadData()">
              <mat-datepicker-toggle matSuffix [for]="endPicker"></mat-datepicker-toggle>
              <mat-datepicker #endPicker></mat-datepicker>
            </mat-form-field>

            <button mat-stroked-button (click)="clearFilters()">
              <mat-icon>clear</mat-icon>
              Clear Filters
            </button>
          </div>

          @if (loading()) {
            <div class="loading-container">
              <mat-spinner></mat-spinner>
            </div>
          } @else {
            <table mat-table [dataSource]="dataSource" matSort (matSortChange)="onSort($event)">
              <ng-container matColumnDef="createdAt">
                <th mat-header-cell *matHeaderCellDef mat-sort-header>Timestamp</th>
                <td mat-cell *matCellDef="let log">{{ log.createdAt | date:'medium' }}</td>
              </ng-container>

              <ng-container matColumnDef="action">
                <th mat-header-cell *matHeaderCellDef mat-sort-header>Action</th>
                <td mat-cell *matCellDef="let log">
                  <mat-chip [class]="getActionClass(log.action)">
                    {{ log.action }}
                  </mat-chip>
                </td>
              </ng-container>

              <ng-container matColumnDef="entityType">
                <th mat-header-cell *matHeaderCellDef mat-sort-header>Entity Type</th>
                <td mat-cell *matCellDef="let log">{{ log.entityType }}</td>
              </ng-container>

              <ng-container matColumnDef="entityName">
                <th mat-header-cell *matHeaderCellDef>Entity</th>
                <td mat-cell *matCellDef="let log">{{ log.entityName || log.entityId }}</td>
              </ng-container>

              <ng-container matColumnDef="userEmail">
                <th mat-header-cell *matHeaderCellDef mat-sort-header>User</th>
                <td mat-cell *matCellDef="let log">{{ log.userEmail }}</td>
              </ng-container>

              <ng-container matColumnDef="ipAddress">
                <th mat-header-cell *matHeaderCellDef>IP Address</th>
                <td mat-cell *matCellDef="let log">{{ log.ipAddress }}</td>
              </ng-container>

              <ng-container matColumnDef="details">
                <th mat-header-cell *matHeaderCellDef>Details</th>
                <td mat-cell *matCellDef="let log">
                  @if (log.changes) {
                    <button mat-icon-button (click)="toggleDetails(log.id)">
                      <mat-icon>{{ expandedId === log.id ? 'expand_less' : 'expand_more' }}</mat-icon>
                    </button>
                  }
                </td>
              </ng-container>

              <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: displayedColumns" [class.expanded]="expandedId === row.id"></tr>
            </table>

            @if (expandedId) {
              <mat-card class="details-card">
                <mat-card-header>
                  <mat-card-title>Change Details</mat-card-title>
                </mat-card-header>
                <mat-card-content>
                  <pre>{{ getExpandedChanges() | json }}</pre>
                </mat-card-content>
              </mat-card>
            }

            <mat-paginator
              [length]="totalCount"
              [pageSize]="pageSize"
              [pageIndex]="pageNumber - 1"
              [pageSizeOptions]="[10, 25, 50, 100]"
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
      align-items: center;
    }
    .loading-container {
      display: flex;
      justify-content: center;
      padding: 48px;
    }
    table {
      width: 100%;
    }
    .mat-chip.create { background: #e8f5e9; color: #2e7d32; }
    .mat-chip.update { background: #e3f2fd; color: #1976d2; }
    .mat-chip.delete { background: #ffebee; color: #c62828; }
    .mat-chip.activate { background: #e8f5e9; color: #2e7d32; }
    .mat-chip.deactivate { background: #fff3e0; color: #ef6c00; }
    .mat-chip.login { background: #f3e5f5; color: #7b1fa2; }
    .mat-chip.logout { background: #eceff1; color: #546e7a; }
    .expanded {
      background: #f5f5f5;
    }
    .details-card {
      margin: 16px 0;
    }
    pre {
      background: #f5f5f5;
      padding: 16px;
      border-radius: 4px;
      overflow-x: auto;
      font-size: 0.85em;
    }
  `]
})
export class AuditLogListComponent implements OnInit {
  private http = inject(HttpClient);

  displayedColumns = ['createdAt', 'action', 'entityType', 'entityName', 'userEmail', 'ipAddress', 'details'];
  dataSource = new MatTableDataSource<AuditLog>([]);

  loading = signal(true);
  entityType: string | null = null;
  action: string | null = null;
  startDate: Date | null = null;
  endDate: Date | null = null;
  pageNumber = 1;
  pageSize = 25;
  totalCount = 0;
  sortBy = 'CreatedAt';
  sortDescending = true;
  expandedId: string | null = null;

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

    if (this.entityType) {
      params = params.set('entityType', this.entityType);
    }
    if (this.action) {
      params = params.set('action', this.action);
    }
    if (this.startDate) {
      params = params.set('startDate', this.startDate.toISOString());
    }
    if (this.endDate) {
      params = params.set('endDate', this.endDate.toISOString());
    }

    this.http.get<PaginatedResult>(`${environment.apiUrl}/audit-logs`, { params }).subscribe({
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

  clearFilters(): void {
    this.entityType = null;
    this.action = null;
    this.startDate = null;
    this.endDate = null;
    this.loadData();
  }

  toggleDetails(id: string): void {
    this.expandedId = this.expandedId === id ? null : id;
  }

  getExpandedChanges(): any {
    if (!this.expandedId) return null;
    const log = this.dataSource.data.find(l => l.id === this.expandedId);
    if (!log?.changes) return null;
    try {
      return JSON.parse(log.changes);
    } catch {
      return log.changes;
    }
  }

  getActionClass(action: string): string {
    return action.toLowerCase();
  }
}
