import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatExpansionModule } from '@angular/material/expansion';
import { environment } from '../../../../environments/environment';

interface Permission {
  id: string;
  name: string;
  description: string;
  category: string;
}

interface GroupedPermissions {
  [category: string]: Permission[];
}

@Component({
  selector: 'app-role-form',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterModule,
    MatCardModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatCheckboxModule, MatProgressSpinnerModule,
    MatExpansionModule
  ],
  template: `
    <div class="form-container">
      <mat-card>
        <mat-card-header>
          <mat-card-title>{{ isEdit() ? 'Edit' : 'Create' }} Role</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          @if (error()) {
            <div class="error-message">{{ error() }}</div>
          }

          <form [formGroup]="form" (ngSubmit)="onSubmit()">
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Name</mat-label>
              <input matInput formControlName="name">
              @if (form.get('name')?.hasError('required')) {
                <mat-error>Name is required</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Description</mat-label>
              <textarea matInput formControlName="description" rows="3"></textarea>
            </mat-form-field>

            <h3>Permissions</h3>
            <mat-accordion>
              @for (category of getCategories(); track category) {
                <mat-expansion-panel>
                  <mat-expansion-panel-header>
                    <mat-panel-title>
                      {{ category }}
                    </mat-panel-title>
                    <mat-panel-description>
                      {{ getSelectedCount(category) }} / {{ groupedPermissions()[category].length }} selected
                    </mat-panel-description>
                  </mat-expansion-panel-header>

                  <div class="permission-list">
                    <div class="select-all">
                      <mat-checkbox
                        [checked]="isAllSelected(category)"
                        [indeterminate]="isIndeterminate(category)"
                        (change)="toggleCategory(category, $event.checked)">
                        Select All
                      </mat-checkbox>
                    </div>
                    @for (permission of groupedPermissions()[category]; track permission.id) {
                      <div class="permission-item">
                        <mat-checkbox
                          [checked]="isPermissionSelected(permission.id)"
                          (change)="togglePermission(permission.id, $event.checked)">
                          <div class="permission-info">
                            <strong>{{ permission.name }}</strong>
                            <p>{{ permission.description }}</p>
                          </div>
                        </mat-checkbox>
                      </div>
                    }
                  </div>
                </mat-expansion-panel>
              }
            </mat-accordion>

            <div class="form-actions">
              <button mat-button type="button" routerLink="/roles">Cancel</button>
              <button mat-raised-button color="primary" type="submit" [disabled]="loading() || form.invalid">
                @if (loading()) {
                  <mat-spinner diameter="20"></mat-spinner>
                } @else {
                  {{ isEdit() ? 'Update' : 'Create' }}
                }
              </button>
            </div>
          </form>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .form-container {
      max-width: 800px;
      margin: 0 auto;
    }
    .full-width {
      width: 100%;
      margin-bottom: 16px;
    }
    .error-message {
      background: #ffebee;
      color: #c62828;
      padding: 12px;
      border-radius: 4px;
      margin-bottom: 16px;
    }
    h3 {
      margin: 24px 0 16px;
    }
    .permission-list {
      padding: 8px 0;
    }
    .select-all {
      padding: 8px 0;
      border-bottom: 1px solid #eee;
      margin-bottom: 8px;
    }
    .permission-item {
      padding: 8px 0;
    }
    .permission-info {
      margin-left: 8px;
    }
    .permission-info p {
      margin: 4px 0 0;
      color: #666;
      font-size: 0.85em;
    }
    .form-actions {
      display: flex;
      justify-content: flex-end;
      gap: 8px;
      margin-top: 24px;
    }
  `]
})
export class RoleFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  form: FormGroup = this.fb.group({
    name: ['', Validators.required],
    description: ['']
  });

  isEdit = signal(false);
  loading = signal(false);
  error = signal<string | null>(null);
  groupedPermissions = signal<GroupedPermissions>({});
  selectedPermissions = signal<string[]>([]);
  private roleId: string | null = null;

  ngOnInit(): void {
    this.roleId = this.route.snapshot.paramMap.get('id');
    if (this.roleId) {
      this.isEdit.set(true);
    }

    this.loadPermissions();
    if (this.roleId) {
      this.loadRole();
    }
  }

  loadPermissions(): void {
    this.http.get<Permission[]>(`${environment.apiUrl}/permissions`).subscribe({
      next: (permissions) => {
        const grouped: GroupedPermissions = {};
        permissions.forEach(p => {
          if (!grouped[p.category]) {
            grouped[p.category] = [];
          }
          grouped[p.category].push(p);
        });
        this.groupedPermissions.set(grouped);
      }
    });
  }

  loadRole(): void {
    this.loading.set(true);
    this.http.get<any>(`${environment.apiUrl}/roles/${this.roleId}`).subscribe({
      next: (role) => {
        this.form.patchValue({
          name: role.name,
          description: role.description
        });
        this.selectedPermissions.set(role.permissionIds || []);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Failed to load role');
      }
    });
  }

  getCategories(): string[] {
    return Object.keys(this.groupedPermissions()).sort();
  }

  isPermissionSelected(permissionId: string): boolean {
    return this.selectedPermissions().includes(permissionId);
  }

  togglePermission(permissionId: string, checked: boolean): void {
    const current = this.selectedPermissions();
    if (checked) {
      this.selectedPermissions.set([...current, permissionId]);
    } else {
      this.selectedPermissions.set(current.filter(id => id !== permissionId));
    }
  }

  getSelectedCount(category: string): number {
    const permissions = this.groupedPermissions()[category] || [];
    return permissions.filter(p => this.isPermissionSelected(p.id)).length;
  }

  isAllSelected(category: string): boolean {
    const permissions = this.groupedPermissions()[category] || [];
    return permissions.length > 0 && permissions.every(p => this.isPermissionSelected(p.id));
  }

  isIndeterminate(category: string): boolean {
    const count = this.getSelectedCount(category);
    const total = (this.groupedPermissions()[category] || []).length;
    return count > 0 && count < total;
  }

  toggleCategory(category: string, checked: boolean): void {
    const permissions = this.groupedPermissions()[category] || [];
    const permissionIds = permissions.map(p => p.id);
    let current = this.selectedPermissions();

    if (checked) {
      const newIds = permissionIds.filter(id => !current.includes(id));
      this.selectedPermissions.set([...current, ...newIds]);
    } else {
      this.selectedPermissions.set(current.filter(id => !permissionIds.includes(id)));
    }
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    this.loading.set(true);
    this.error.set(null);

    const data = {
      ...this.form.value,
      permissionIds: this.selectedPermissions()
    };

    const request = this.isEdit()
      ? this.http.put(`${environment.apiUrl}/roles/${this.roleId}`, { id: this.roleId, ...data })
      : this.http.post<{ id: string }>(`${environment.apiUrl}/roles`, data);

    request.subscribe({
      next: () => {
        this.router.navigate(['/roles']);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.error || 'Failed to save role');
      }
    });
  }
}
