import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatStepperModule } from '@angular/material/stepper';
import { environment } from '../../../../environments/environment';

interface Organization {
  id: string;
  name: string;
}

interface SubOrganization {
  id: string;
  name: string;
  level: number;
}

interface Role {
  id: string;
  name: string;
  description: string;
  isSystemRole: boolean;
}

@Component({
  selector: 'app-user-form',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterModule,
    MatCardModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatButtonModule, MatCheckboxModule,
    MatProgressSpinnerModule, MatStepperModule
  ],
  template: `
    <div class="form-container">
      <mat-card>
        <mat-card-header>
          <mat-card-title>{{ isEdit() ? 'Edit' : 'Create' }} User</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          @if (error()) {
            <div class="error-message">{{ error() }}</div>
          }

          <mat-stepper [linear]="!isEdit()" #stepper>
            <mat-step [stepControl]="basicInfoForm">
              <ng-template matStepLabel>Basic Information</ng-template>
              <form [formGroup]="basicInfoForm">
                <div class="form-row">
                  <mat-form-field appearance="outline">
                    <mat-label>First Name</mat-label>
                    <input matInput formControlName="firstName">
                    @if (basicInfoForm.get('firstName')?.hasError('required')) {
                      <mat-error>First name is required</mat-error>
                    }
                  </mat-form-field>

                  <mat-form-field appearance="outline">
                    <mat-label>Last Name</mat-label>
                    <input matInput formControlName="lastName">
                    @if (basicInfoForm.get('lastName')?.hasError('required')) {
                      <mat-error>Last name is required</mat-error>
                    }
                  </mat-form-field>
                </div>

                <mat-form-field appearance="outline" class="full-width">
                  <mat-label>Email</mat-label>
                  <input matInput formControlName="email" type="email">
                  @if (basicInfoForm.get('email')?.hasError('required')) {
                    <mat-error>Email is required</mat-error>
                  }
                  @if (basicInfoForm.get('email')?.hasError('email')) {
                    <mat-error>Invalid email format</mat-error>
                  }
                </mat-form-field>

                @if (!isEdit()) {
                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Password</mat-label>
                    <input matInput formControlName="password" type="password">
                    @if (basicInfoForm.get('password')?.hasError('required')) {
                      <mat-error>Password is required</mat-error>
                    }
                    @if (basicInfoForm.get('password')?.hasError('minlength')) {
                      <mat-error>Password must be at least 8 characters</mat-error>
                    }
                  </mat-form-field>
                }

                <div class="step-actions">
                  <button mat-button type="button" routerLink="/users">Cancel</button>
                  <button mat-raised-button color="primary" matStepperNext [disabled]="basicInfoForm.invalid">Next</button>
                </div>
              </form>
            </mat-step>

            <mat-step [stepControl]="organizationForm">
              <ng-template matStepLabel>Organization</ng-template>
              <form [formGroup]="organizationForm">
                <mat-form-field appearance="outline" class="full-width">
                  <mat-label>Organization</mat-label>
                  <mat-select formControlName="organizationId" (selectionChange)="onOrganizationChange()">
                    @for (org of organizations(); track org.id) {
                      <mat-option [value]="org.id">{{ org.name }}</mat-option>
                    }
                  </mat-select>
                  @if (organizationForm.get('organizationId')?.hasError('required')) {
                    <mat-error>Organization is required</mat-error>
                  }
                </mat-form-field>

                @if (subOrganizations().length > 0) {
                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Sub-Organization (Optional)</mat-label>
                    <mat-select formControlName="subOrganizationId">
                      <mat-option [value]="null">None</mat-option>
                      @for (subOrg of subOrganizations(); track subOrg.id) {
                        <mat-option [value]="subOrg.id">
                          {{ '—'.repeat(subOrg.level - 1) }} {{ subOrg.name }}
                        </mat-option>
                      }
                    </mat-select>
                  </mat-form-field>
                }

                <div class="step-actions">
                  <button mat-button matStepperPrevious>Back</button>
                  <button mat-raised-button color="primary" matStepperNext [disabled]="organizationForm.invalid">Next</button>
                </div>
              </form>
            </mat-step>

            <mat-step>
              <ng-template matStepLabel>Roles</ng-template>
              <form [formGroup]="rolesForm">
                <div class="roles-list">
                  @for (role of roles(); track role.id) {
                    <div class="role-item">
                      <mat-checkbox
                        [checked]="isRoleSelected(role.id)"
                        (change)="toggleRole(role.id, $event.checked)"
                        [disabled]="role.isSystemRole && role.name === 'Super Admin'">
                        <div class="role-info">
                          <strong>{{ role.name }}</strong>
                          @if (role.isSystemRole) {
                            <span class="system-badge">System</span>
                          }
                          <p>{{ role.description }}</p>
                        </div>
                      </mat-checkbox>
                    </div>
                  }
                </div>

                <div class="step-actions">
                  <button mat-button matStepperPrevious>Back</button>
                  <button mat-raised-button color="primary" (click)="onSubmit()" [disabled]="loading() || selectedRoles().length === 0">
                    @if (loading()) {
                      <mat-spinner diameter="20"></mat-spinner>
                    } @else {
                      {{ isEdit() ? 'Update' : 'Create' }} User
                    }
                  </button>
                </div>
              </form>
            </mat-step>
          </mat-stepper>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .form-container {
      max-width: 800px;
      margin: 0 auto;
    }
    .form-row {
      display: flex;
      gap: 16px;
    }
    .form-row mat-form-field {
      flex: 1;
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
    .step-actions {
      display: flex;
      justify-content: flex-end;
      gap: 8px;
      margin-top: 24px;
    }
    .roles-list {
      max-height: 400px;
      overflow-y: auto;
    }
    .role-item {
      padding: 12px;
      border-bottom: 1px solid #eee;
    }
    .role-info {
      margin-left: 8px;
    }
    .role-info p {
      margin: 4px 0 0;
      color: #666;
      font-size: 0.85em;
    }
    .system-badge {
      background: #e3f2fd;
      color: #1976d2;
      font-size: 0.7em;
      padding: 2px 6px;
      border-radius: 4px;
      margin-left: 8px;
    }
  `]
})
export class UserFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  basicInfoForm: FormGroup = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  organizationForm: FormGroup = this.fb.group({
    organizationId: ['', Validators.required],
    subOrganizationId: [null]
  });

  rolesForm: FormGroup = this.fb.group({});

  isEdit = signal(false);
  loading = signal(false);
  error = signal<string | null>(null);
  organizations = signal<Organization[]>([]);
  subOrganizations = signal<SubOrganization[]>([]);
  roles = signal<Role[]>([]);
  selectedRoles = signal<string[]>([]);
  private userId: string | null = null;

  ngOnInit(): void {
    this.userId = this.route.snapshot.paramMap.get('id');
    if (this.userId) {
      this.isEdit.set(true);
      this.basicInfoForm.get('password')?.clearValidators();
      this.basicInfoForm.get('password')?.updateValueAndValidity();
    }

    this.loadOrganizations();
    this.loadRoles();

    if (this.userId) {
      this.loadUser();
    }
  }

  loadOrganizations(): void {
    this.http.get<{ items: Organization[] }>(`${environment.apiUrl}/organizations?pageSize=1000`).subscribe({
      next: (result) => {
        this.organizations.set(result.items);
      }
    });
  }

  loadRoles(): void {
    this.http.get<{ items: Role[] }>(`${environment.apiUrl}/roles?pageSize=100`).subscribe({
      next: (result) => {
        this.roles.set(result.items);
      }
    });
  }

  loadUser(): void {
    this.loading.set(true);
    this.http.get<any>(`${environment.apiUrl}/users/${this.userId}`).subscribe({
      next: (user) => {
        this.basicInfoForm.patchValue({
          firstName: user.firstName,
          lastName: user.lastName,
          email: user.email
        });
        this.organizationForm.patchValue({
          organizationId: user.organizationId,
          subOrganizationId: user.subOrganizationId
        });
        this.selectedRoles.set(user.roleIds || []);
        if (user.organizationId) {
          this.loadSubOrganizations(user.organizationId);
        }
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Failed to load user');
      }
    });
  }

  onOrganizationChange(): void {
    const orgId = this.organizationForm.get('organizationId')?.value;
    this.organizationForm.get('subOrganizationId')?.setValue(null);
    if (orgId) {
      this.loadSubOrganizations(orgId);
    } else {
      this.subOrganizations.set([]);
    }
  }

  loadSubOrganizations(organizationId: string): void {
    this.http.get<SubOrganization[]>(`${environment.apiUrl}/organizations/${organizationId}/sub-organizations`).subscribe({
      next: (subOrgs) => {
        this.subOrganizations.set(subOrgs);
      }
    });
  }

  isRoleSelected(roleId: string): boolean {
    return this.selectedRoles().includes(roleId);
  }

  toggleRole(roleId: string, checked: boolean): void {
    const current = this.selectedRoles();
    if (checked) {
      this.selectedRoles.set([...current, roleId]);
    } else {
      this.selectedRoles.set(current.filter(id => id !== roleId));
    }
  }

  onSubmit(): void {
    if (this.basicInfoForm.invalid || this.organizationForm.invalid || this.selectedRoles().length === 0) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    const data = {
      ...this.basicInfoForm.value,
      ...this.organizationForm.value,
      roleIds: this.selectedRoles()
    };

    if (this.isEdit()) {
      delete data.password;
    }

    const request = this.isEdit()
      ? this.http.put(`${environment.apiUrl}/users/${this.userId}`, { id: this.userId, ...data })
      : this.http.post<{ id: string }>(`${environment.apiUrl}/users`, data);

    request.subscribe({
      next: () => {
        this.router.navigate(['/users']);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.error || 'Failed to save user');
      }
    });
  }
}
