import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-organization-form',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterModule,
    MatCardModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatProgressSpinnerModule
  ],
  template: `
    <div class="form-container">
      <mat-card>
        <mat-card-header>
          <mat-card-title>{{ isEdit() ? 'Edit' : 'Create' }} Organization</mat-card-title>
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
              <mat-label>Code</mat-label>
              <input matInput formControlName="code">
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Description</mat-label>
              <textarea matInput formControlName="description" rows="4"></textarea>
            </mat-form-field>

            <div class="form-actions">
              <button mat-button type="button" routerLink="/organizations">Cancel</button>
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
      max-width: 600px;
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
    .form-actions {
      display: flex;
      justify-content: flex-end;
      gap: 8px;
    }
  `]
})
export class OrganizationFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  form: FormGroup = this.fb.group({
    name: ['', Validators.required],
    code: [''],
    description: ['']
  });

  isEdit = signal(false);
  loading = signal(false);
  error = signal<string | null>(null);
  private orgId: string | null = null;

  ngOnInit(): void {
    this.orgId = this.route.snapshot.paramMap.get('id');
    if (this.orgId) {
      this.isEdit.set(true);
      this.loadOrganization();
    }
  }

  loadOrganization(): void {
    this.loading.set(true);
    this.http.get<any>(`${environment.apiUrl}/organizations/${this.orgId}`).subscribe({
      next: (org) => {
        this.form.patchValue({
          name: org.name,
          code: org.code,
          description: org.description
        });
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Failed to load organization');
      }
    });
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    this.loading.set(true);
    this.error.set(null);

    const data = this.form.value;
    const request = this.isEdit()
      ? this.http.put(`${environment.apiUrl}/organizations/${this.orgId}`, { id: this.orgId, ...data })
      : this.http.post<{ id: string }>(`${environment.apiUrl}/organizations`, data);

    request.subscribe({
      next: () => {
        this.router.navigate(['/organizations']);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.error || 'Failed to save organization');
      }
    });
  }
}
