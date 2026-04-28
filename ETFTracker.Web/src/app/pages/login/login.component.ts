import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom, timeout } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { SeoService } from '../../services/seo.service';
import { environment } from '../../../environments/environment';

const AUTH_API = `${environment.apiBase}/api/auth`;

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="login-container">
      <div class="login-card">

        <div class="brand" (click)="goToLanding()" (keydown.enter)="goToLanding()" (keydown.space)="goToLanding()" tabindex="0" role="button" aria-label="Go to landing page">
          <img src="logo_light.png" alt="Portify logo" class="brand-logo" />
        </div>

        <div *ngIf="errorMessage" class="error-banner">{{ errorMessage }}</div>
        <div *ngIf="successMessage" class="success-banner">{{ successMessage }}</div>

        <!-- Email / Password form -->
        <form (ngSubmit)="submitEmailForm()" #emailForm="ngForm" class="email-form">
          <div *ngIf="isRegister" class="name-row">
            <input type="text" [(ngModel)]="firstName" name="firstName" placeholder="First name" class="input" />
            <input type="text" [(ngModel)]="lastName"  name="lastName"  placeholder="Last name"  class="input" />
          </div>
          <input *ngIf="isRegister"
                 type="text" [(ngModel)]="username" name="username"
                 placeholder="Username (required)" required class="input"
                 autocomplete="username" />
          <input *ngIf="isRegister"
                 type="email" [(ngModel)]="email" name="email"
                 placeholder="Email (optional)" class="input"
                 autocomplete="email" />
          <input *ngIf="!isRegister"
                 type="text" [(ngModel)]="emailOrUsername" name="emailOrUsername"
                 placeholder="Email or Username" required class="input"
                 autocomplete="username" />
          <input type="password" [(ngModel)]="password" name="password" placeholder="Password" required class="input"
                 [minlength]="isRegister ? 8 : 1" />
          <button type="submit" class="btn btn-primary" [disabled]="loading || !canSubmit()">
            {{ loading ? 'Please wait…' : (isRegister ? 'Create account' : 'Sign in') }}
          </button>
        </form>

        <p class="toggle-link">
          {{ isRegister ? 'Already have an account?' : "Don't have an account?" }}
          <a (click)="toggleMode()">{{ isRegister ? 'Sign in' : 'Register' }}</a>
        </p>

        <div class="divider"><span>or continue with</span></div>

        <div class="btn-group">
          <button *ngIf="showGitHubAuth" class="btn btn-github" (click)="loginWithGitHub()">
            <svg viewBox="0 0 24 24" width="20" height="20" fill="currentColor" aria-hidden="true">
              <path d="M12 0C5.37 0 0 5.37 0 12c0 5.3 3.44 9.8 8.21 11.39.6.11.82-.26.82-.58
                       0-.28-.01-1.04-.02-2.04-3.34.73-4.04-1.61-4.04-1.61-.55-1.39-1.34-1.76
                       -1.34-1.76-1.09-.75.08-.74.08-.74 1.21.09 1.85 1.24 1.85 1.24 1.07 1.84
                       2.81 1.31 3.5 1 .11-.78.42-1.31.76-1.61-2.67-.3-5.47-1.33-5.47-5.93
                       0-1.31.47-2.38 1.24-3.22-.12-.3-.54-1.52.12-3.18 0 0 1.01-.32 3.3 1.23
                       a11.5 11.5 0 0 1 3-.4c1.02.005 2.04.14 3 .4 2.28-1.55 3.29-1.23 3.29
                       -1.23.66 1.66.24 2.88.12 3.18.77.84 1.24 1.91 1.24 3.22 0 4.61-2.81
                       5.63-5.48 5.92.43.37.81 1.1.81 2.22 0 1.61-.01 2.9-.01 3.3 0 .32.21
                       .69.82.57C20.57 21.8 24 17.3 24 12c0-6.63-5.37-12-12-12z"/>
            </svg>
            Continue with GitHub
          </button>

          <button class="btn btn-google" (click)="loginWithGoogle()">
            <svg viewBox="0 0 24 24" width="20" height="20" aria-hidden="true">
              <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92
                c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
              <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77
                c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84
                C3.99 20.53 7.7 23 12 23z"/>
              <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35
                -2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l3.66-2.84z"/>
              <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15
                C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84
                c.87-2.6 3.3-4.53 6.16-4.53z"/>
            </svg>
            Continue with Google
          </button>
        </div>

      </div>
    </div>
  `,
  styles: [`
    .login-container {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--bg-surface);
    }
    .login-card {
      background: var(--bg-card);
      border: 1px solid var(--border-light);
      border-radius: 16px;
      padding: 2.5rem 2rem;
      width: 380px;
      text-align: center;
      box-shadow: var(--shadow-lg);
    }
    .brand {
      margin-bottom: 1.5rem;
      cursor: pointer;
      width: fit-content;
      margin-left: auto;
      margin-right: auto;
    }
    .brand-logo {
      height: 56px;
      width: auto;
      display: block;
      margin: 0 auto 0.75rem;
      object-fit: contain;
    }
    h1 { font-size: 1.35rem; font-weight: 700; margin: 0 0 0.4rem; color: var(--text-primary); }
    .subtitle { color: var(--text-muted); font-size: 0.88rem; margin: 0; }

    .error-banner {
      background: var(--negative-bg);
      color: var(--negative);
      border: 1px solid rgba(240,82,82,0.3);
      border-radius: 8px;
      padding: 0.75rem;
      margin-bottom: 1rem;
      font-size: 0.88rem;
    }
    .success-banner {
      background: rgba(52,211,153,0.1);
      color: #34d399;
      border: 1px solid rgba(52,211,153,0.3);
      border-radius: 8px;
      padding: 0.75rem;
      margin-bottom: 1rem;
      font-size: 0.88rem;
    }

    .email-form {
      display: flex;
      flex-direction: column;
      gap: 0.65rem;
      margin-bottom: 0.75rem;
    }
    .name-row { display: flex; gap: 0.5rem; }
    .name-row .input { flex: 1; }
    .input {
      width: 100%;
      padding: 0.65rem 0.85rem;
      border: 1px solid var(--border-light);
      border-radius: 8px;
      background: var(--bg-elevated);
      color: var(--text-primary);
      font-size: 0.9rem;
      box-sizing: border-box;
      outline: none;
      &:focus { border-color: var(--accent); }
    }

    .btn-primary {
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 0.75rem;
      border-radius: 10px;
      border: none;
      background: var(--accent);
      color: #fff;
      font-size: 0.95rem;
      font-weight: 600;
      cursor: pointer;
      transition: opacity 0.15s;
      &:hover:not(:disabled) { opacity: 0.88; }
      &:disabled { opacity: 0.55; cursor: default; }
    }

    .toggle-link {
      font-size: 0.85rem;
      color: var(--text-muted);
      margin: 0.5rem 0 0.75rem;
      a { color: var(--accent); cursor: pointer; margin-left: 4px; text-decoration: underline; }
    }

    .divider {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      margin: 0.75rem 0;
      color: var(--text-muted);
      font-size: 0.8rem;
      &::before, &::after { content: ''; flex: 1; height: 1px; background: var(--border-light); }
    }

    .btn-group { display: flex; flex-direction: column; gap: 0.75rem; }
    .btn {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.75rem;
      padding: 0.75rem 1.25rem;
      border-radius: 10px;
      border: 1px solid var(--border-light);
      font-size: 0.95rem;
      font-weight: 600;
      cursor: pointer;
      transition: opacity 0.15s, transform 0.1s;
      &:hover { opacity: 0.88; transform: translateY(-1px); }
      &:active { transform: translateY(0); }
    }
    .btn-github { background: #24292e; color: #fff; border-color: #24292e; }
    .btn-google { background: var(--bg-elevated); color: var(--text-primary); }
  `]
})
export class LoginComponent implements OnInit {
  errorMessage   = '';
  successMessage = '';
  isRegister     = false;
  loading        = false;

  // Keep GitHub auth code path available while hiding it from the UI for now.
  readonly showGitHubAuth = false;

  // Register fields
  username  = '';
  email     = '';
  password  = '';
  firstName = '';
  lastName  = '';

  // Login field
  emailOrUsername = '';

  private readonly requestTimeoutMs = 15000;

  constructor(private auth: AuthService, private route: ActivatedRoute, private http: HttpClient, private router: Router, private seo: SeoService) {}

  ngOnInit(): void {
    this.seo.update({ title: 'Sign In – Portify', description: 'Sign in to your Portify investment portfolio tracker account.', url: '/login', noIndex: true });
    const err = this.route.snapshot.queryParamMap.get('error');
    if (err === 'oauth_failed') this.errorMessage = 'Authentication failed. Please try again.';
  }

  toggleMode(): void {
    this.isRegister     = !this.isRegister;
    this.errorMessage   = '';
    this.successMessage = '';
  }

  canSubmit(): boolean {
    if (this.isRegister) {
      return this.isValidUsername(this.username) && this.password.length >= 8;
    }
    return this.emailOrUsername.trim().length >= 1 && this.password.length >= 1;
  }

  async submitEmailForm(): Promise<void> {
    this.errorMessage   = '';
    this.successMessage = '';

    if (!this.canSubmit()) {
      if (this.isRegister) {
        this.errorMessage = !this.isValidUsername(this.username)
          ? 'Username must be 3–50 characters (letters, numbers, _ or -).'
          : 'Password must be at least 8 characters.';
      } else {
        this.errorMessage = 'Please enter your email or username and password.';
      }
      return;
    }

    this.loading = true;
    try {
      let token: string;
      if (this.isRegister) {
        const res = await firstValueFrom(
          this.http.post<{ token: string }>(`${AUTH_API}/register`, {
            username: this.username.trim(),
            email: this.email.trim() || null,
            password: this.password,
            firstName: this.firstName || null,
            lastName: this.lastName || null
          }).pipe(timeout(this.requestTimeoutMs))
        );
        token = res.token;
      } else {
        const res = await firstValueFrom(
          this.http.post<{ token: string }>(`${AUTH_API}/login`, {
            emailOrUsername: this.emailOrUsername.trim(),
            password: this.password
          }).pipe(timeout(this.requestTimeoutMs))
        );
        token = res.token;
      }
      this.auth.handleToken(token);
      await this.router.navigate(['/dashboard']);
    } catch (err: any) {
      const msg = err?.error?.message;
      if (err?.status === 409) {
        const errorCode = err?.error?.error;
        this.errorMessage = errorCode === 'username_taken'
          ? 'This username is already taken.'
          : 'An account with this email already exists.';
      }
      else if (err?.status === 401)  this.errorMessage = 'Invalid email/username or password.';
      else if (err?.name === 'TimeoutError') this.errorMessage = 'Request timed out. Please try again.';
      else if (msg)                  this.errorMessage = msg;
      else                           this.errorMessage = 'Something went wrong. Please try again.';
    } finally {
      this.loading = false;
    }
  }

  loginWithGitHub(): void { this.auth.loginWithGitHub(); }
  loginWithGoogle(): void  { this.auth.loginWithGoogle(); }

  goToLanding(): void {
    this.router.navigate(['/']);
  }

  private isValidUsername(value: string): boolean {
    return /^[a-zA-Z0-9_\-]{3,50}$/.test(value.trim());
  }

  private isValidEmail(value: string): boolean {
    const email = value.trim();
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
  }
}
