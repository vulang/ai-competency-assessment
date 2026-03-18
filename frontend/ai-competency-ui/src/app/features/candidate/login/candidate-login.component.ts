import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClientModule, HttpClient, HttpHeaders } from '@angular/common/http';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-candidate-login',
  standalone: true,
  imports: [CommonModule, FormsModule, HttpClientModule],
  template: `
    <div class="login-container">
      <div class="login-card">
        <div class="login-header">
          <h2>Candidate Login</h2>
          <p>Sign in to take your assessment</p>
        </div>
        
        <form (ngSubmit)="onSubmit()" #loginForm="ngForm" class="login-form">
          <div class="form-group">
            <label for="username">Email</label>
            <input 
              type="email" 
              id="username" 
              name="username" 
              [(ngModel)]="username" 
              required 
              class="form-control"
              placeholder="Enter your email"
            >
          </div>
          
          <div class="form-group">
            <label for="password">Password</label>
            <input 
              type="password" 
              id="password" 
              name="password" 
              [(ngModel)]="password" 
              required 
              class="form-control"
              placeholder="Enter your password"
            >
          </div>

          <div *ngIf="errorMessage" class="error-message">
            {{ errorMessage }}
          </div>
          
          <button type="submit" class="btn btn-primary btn-block" [disabled]="!loginForm.form.valid || isLoading">
            <span *ngIf="isLoading" class="spinner"></span>
            <span *ngIf="!isLoading">Sign In</span>
          </button>
        </form>

        <div class="divider">
          <span>OR</span>
        </div>

        <button type="button" class="btn btn-secondary btn-block" (click)="ssoLogin()" [disabled]="isLoading">
          Sign In with SSO
        </button>
      </div>
    </div>
  `,
  styles: [`
    .login-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      background-color: #f5f7fa;
      font-family: 'Inter', sans-serif;
    }
    .login-card {
      background-color: #ffffff;
      border-radius: 8px;
      box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
      padding: 40px;
      width: 100%;
      max-width: 400px;
    }
    .login-header {
      text-align: center;
      margin-bottom: 30px;
    }
    .login-header h2 {
      margin: 0;
      color: #1a202c;
      font-size: 1.5rem;
    }
    .login-header p {
      margin: 8px 0 0;
      color: #718096;
      font-size: 0.9rem;
    }
    .form-group {
      margin-bottom: 20px;
    }
    .form-group label {
      display: block;
      margin-bottom: 8px;
      color: #4a5568;
      font-size: 0.9rem;
      font-weight: 500;
    }
    .form-control {
      width: 100%;
      padding: 10px 12px;
      border: 1px solid #e2e8f0;
      border-radius: 4px;
      font-size: 1rem;
      color: #1a202c;
      box-sizing: border-box;
      transition: border-color 0.2s;
    }
    .form-control:focus {
      outline: none;
      border-color: #3182ce;
      box-shadow: 0 0 0 3px rgba(49, 130, 206, 0.1);
    }
    .btn {
      display: inline-flex;
      justify-content: center;
      align-items: center;
      padding: 10px 16px;
      font-size: 1rem;
      font-weight: 500;
      border-radius: 4px;
      cursor: pointer;
      border: none;
      transition: all 0.2s;
    }
    .btn:disabled {
      opacity: 0.7;
      cursor: not-allowed;
    }
    .btn-block {
      width: 100%;
    }
    .btn-primary {
      background-color: #3182ce;
      color: #ffffff;
    }
    .btn-primary:hover:not(:disabled) {
      background-color: #2b6cb0;
    }
    .btn-secondary {
      background-color: #edf2f7;
      color: #4a5568;
    }
    .btn-secondary:hover:not(:disabled) {
      background-color: #e2e8f0;
    }
    .divider {
      display: flex;
      align-items: center;
      text-align: center;
      margin: 24px 0;
      color: #a0aec0;
      font-size: 0.875rem;
    }
    .divider::before, .divider::after {
      content: '';
      flex: 1;
      border-bottom: 1px solid #e2e8f0;
    }
    .divider span {
      padding: 0 10px;
    }
    .error-message {
      color: #e53e3e;
      font-size: 0.875rem;
      margin-bottom: 16px;
      text-align: center;
    }
    .spinner {
      border: 2px solid rgba(255,255,255,0.3);
      width: 16px;
      height: 16px;
      border-radius: 50%;
      border-left-color: #fff;
      animation: spin 1s ease infinite;
    }
    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }
  `]
})
export class CandidateLoginComponent {
  username = '';
  password = '';
  isLoading = false;
  errorMessage = '';

  private authService = inject(AuthService);
  private http = inject(HttpClient);
  private router = inject(Router);

  onSubmit() {
    if (!this.username || !this.password) return;
    
    this.isLoading = true;
    this.errorMessage = '';

    const body = new URLSearchParams();
    body.set('grant_type', 'password');
    body.set('username', this.username);
    body.set('password', this.password);
    body.set('scope', 'openid profile email roles'); 
    
    // You should technically have client_id. Since we're using OIDC with Identity Server / OpenIddict:
    body.set('client_id', environment.oidc.client_id); 
    // And if there's a client secret we shouldn't really use ropc from SPA, but this is a prototype.
    
    const headers = new HttpHeaders({
      'Content-Type': 'application/x-www-form-urlencoded'
    });

    this.http.post<any>(`${environment.oidc.authority}/connect/token`, body.toString(), { headers })
      .subscribe({
        next: (response) => {
          // Store token somehow, typically an OIDC client handles this, but for ROPC we might need to do it manually.
          // Since it's a prototype, we'll store it manually into sessionStorage and set the currentUser using the response id_token payload
          sessionStorage.setItem('access_token', response.access_token);
          if (response.id_token) {
            sessionStorage.setItem('id_token', response.id_token);
          }
          
          this.authService.loginWithToken(response.access_token, response.id_token);
          this.router.navigate(['/candidate/home']);
          this.isLoading = false;
        },
        error: (err) => {
          console.error(err);
          this.errorMessage = err.error?.error_description || 'Login failed. Please check your credentials.';
          this.isLoading = false;
        }
      });
  }

  ssoLogin() {
    this.authService.login();
  }
}
