import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  template: `
    <div class="forgot-container">
      <mat-card class="forgot-card">
        <mat-card-header>
          <mat-card-title>Forgot Password</mat-card-title>
          <mat-card-subtitle>Enter your email to receive a reset link</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          @if (success()) {
            <div class="success-message">
              If an account exists with this email, you will receive a password reset link.
            </div>
          } @else {
            <form [formGroup]="form" (ngSubmit)="onSubmit()">
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Email</mat-label>
                <input matInput formControlName="email" type="email">
                <mat-icon matSuffix>email</mat-icon>
              </mat-form-field>

              <button mat-raised-button color="primary" type="submit" class="full-width" [disabled]="loading() || form.invalid">
                @if (loading()) {
                  <mat-spinner diameter="20"></mat-spinner>
                } @else {
                  Send Reset Link
                }
              </button>
            </form>
          }
          <div class="back-link">
            <a routerLink="/auth/login">Back to Login</a>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .forgot-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    }
    .forgot-card {
      width: 100%;
      max-width: 400px;
      padding: 24px;
    }
    mat-card-header {
      justify-content: center;
      margin-bottom: 24px;
    }
    .full-width {
      width: 100%;
      margin-bottom: 16px;
    }
    .success-message {
      background: #e8f5e9;
      color: #2e7d32;
      padding: 16px;
      border-radius: 4px;
      margin-bottom: 16px;
    }
    .back-link {
      text-align: center;
      margin-top: 16px;
    }
  `]
})
export class ForgotPasswordComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);

  form: FormGroup = this.fb.group({
    email: ['', [Validators.required, Validators.email]]
  });

  loading = signal(false);
  success = signal(false);

  onSubmit(): void {
    if (this.form.invalid) return;

    this.loading.set(true);
    this.authService.forgotPassword(this.form.value.email).subscribe({
      next: () => {
        this.loading.set(false);
        this.success.set(true);
      },
      error: () => {
        this.loading.set(false);
        this.success.set(true); // Always show success to prevent email enumeration
      }
    });
  }
}
