import { Injectable, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { jwtDecode } from 'jwt-decode';
import { environment } from '../../environments/environment';

const TOKEN_KEY = 'etf_auth_token';
const API_BASE  = `${environment.apiBase}/api/auth`;

export interface CurrentUser {
  userId: string;
  email: string;
  name: string;
  avatarUrl: string;
  githubUsername: string;
  lastLoginAt: string | null;
}

interface JwtPayload {
  userId: string;
  email: string;
  name: string;
  avatarUrl: string;
  githubUsername: string;
  lastLoginAt?: string;
  exp: number;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private _user = signal<CurrentUser | null>(this.loadUserFromToken());
  readonly user = this._user.asReadonly();
  readonly isAuthenticated = computed(() => this._user() !== null);

  constructor(private router: Router) {}

  loginWithGitHub(): void {
    window.location.href = `${API_BASE}/github`;
  }

  loginWithGoogle(): void {
    window.location.href = `${API_BASE}/google`;
  }


  /** Called by auth-callback page after the OAuth redirect lands */
  handleToken(token: string): void {
    localStorage.setItem(TOKEN_KEY, token);
    this._user.set(this.decodeToken(token));
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    this._user.set(null);
    this.router.navigate(['/']);
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  private loadUserFromToken(): CurrentUser | null {
    const token = localStorage.getItem(TOKEN_KEY);
    return token ? this.decodeToken(token) : null;
  }

  private decodeToken(token: string): CurrentUser | null {
    try {
      const payload = jwtDecode<JwtPayload>(token);
      if (payload.exp * 1000 < Date.now()) {
        localStorage.removeItem(TOKEN_KEY);
        return null;
      }
      return {
        userId:        payload.userId,
        email:         payload.email,
        name:          payload.name,
        avatarUrl:     payload.avatarUrl,
        githubUsername: payload.githubUsername,
        lastLoginAt:   payload.lastLoginAt || null,
      };
    } catch {
      localStorage.removeItem(TOKEN_KEY);
      return null;
    }
  }
}

