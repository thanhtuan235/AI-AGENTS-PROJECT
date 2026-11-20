import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { AuthUser, LoginCredentials, LoginResponse } from '../models/auth.models';

const TOKEN_KEY = 'token';

@Injectable()
export class AuthService {
  constructor(private readonly http: HttpClient) {}

  login(credentials: LoginCredentials): Observable<AuthUser> {
    return this.http
      .post<LoginResponse>(`${environment.eofficeUrl}/auth/login`, credentials)
      .pipe(
        map((response) => response.token),
        tap((token) => sessionStorage.setItem(TOKEN_KEY, token)),
        map((token) => this.decodeToken(token))
      );
  }

  logout(): void {
    sessionStorage.removeItem(TOKEN_KEY);
  }

  isLoggedIn(): boolean {
    const user = this.getUser();
    return !!user && user.exp * 1000 > Date.now();
  }

  getToken(): string | null {
    return sessionStorage.getItem(TOKEN_KEY);
  }

  getUser(): AuthUser | null {
    const token = this.getToken();
    if (!token) {
      return null;
    }

    try {
      return this.decodeToken(token);
    } catch {
      return null;
    }
  }

  private decodeToken(token: string): AuthUser {
    const parts = token.split('.');
    if (parts.length !== 3) {
      throw new Error('Invalid JWT token');
    }

    const payload = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const paddedPayload = payload + '='.repeat((4 - (payload.length % 4)) % 4);
    const claims = JSON.parse(atob(paddedPayload));

    return {
      sub: claims.sub,
      name: claims.name,
      role: claims.role,
      dept_id: claims.dept_id,
      exp: claims.exp,
    };
  }
}
