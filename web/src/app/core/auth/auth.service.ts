import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { API_BASE } from '../config';
import { AuthTokens, LoginRequest } from '../models';
import { TenantInfo } from '../api/tenant.service';
import { AuthStore } from './auth.store';

/** Authentication flows: default-tenant lookup, login, refresh, logout. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE);
  private readonly store = inject(AuthStore);

  defaultTenant(): Observable<TenantInfo> {
    return this.http.get<TenantInfo>(`${this.base}/api/tenants/default`);
  }

  login(request: LoginRequest): Observable<AuthTokens> {
    return this.http.post<AuthTokens>(`${this.base}/api/auth/login`, request)
      .pipe(tap((tokens) => this.store.set(tokens)));
  }

  refresh(): Observable<AuthTokens> {
    return this.http.post<AuthTokens>(`${this.base}/api/auth/refresh`, { refreshToken: this.store.refresh })
      .pipe(tap((tokens) => this.store.set(tokens)));
  }

  logout(): void {
    const refreshToken = this.store.refresh;
    if (refreshToken) {
      this.http.post(`${this.base}/api/auth/logout`, { refreshToken }).subscribe({ error: () => {} });
    }
    this.store.clear();
  }
}
