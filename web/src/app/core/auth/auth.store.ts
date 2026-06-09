import { Injectable, computed, signal } from '@angular/core';
import { AuthTokens } from '../models';

interface JwtPayload {
  sub?: string;
  email?: string;
  role?: string | string[];
  perm?: string | string[];
  tenant_id?: string;
  exp?: number;
}

const ACCESS_KEY = 'alfaris.access';
const REFRESH_KEY = 'alfaris.refresh';

/** Holds auth tokens + decoded permissions; persists across reloads. */
@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly accessToken = signal<string | null>(localStorage.getItem(ACCESS_KEY));
  private readonly refreshToken = signal<string | null>(localStorage.getItem(REFRESH_KEY));

  readonly payload = computed<JwtPayload | null>(() => decode(this.accessToken()));
  readonly isAuthenticated = computed(() => !!this.payload());
  readonly email = computed(() => this.payload()?.email ?? '');
  readonly roles = computed(() => asArray(this.payload()?.role));
  readonly permissions = computed(() => new Set(asArray(this.payload()?.perm)));

  get access(): string | null { return this.accessToken(); }
  get refresh(): string | null { return this.refreshToken(); }

  set(tokens: AuthTokens): void {
    this.accessToken.set(tokens.accessToken);
    this.refreshToken.set(tokens.refreshToken);
    localStorage.setItem(ACCESS_KEY, tokens.accessToken);
    localStorage.setItem(REFRESH_KEY, tokens.refreshToken);
  }

  clear(): void {
    this.accessToken.set(null);
    this.refreshToken.set(null);
    localStorage.removeItem(ACCESS_KEY);
    localStorage.removeItem(REFRESH_KEY);
  }

  /** True when the user holds the permission (or any when none is required). */
  has(permission?: string | null): boolean {
    if (!permission) return true;
    return this.permissions().has(permission);
  }
}

function asArray(value: string | string[] | undefined): string[] {
  if (!value) return [];
  return Array.isArray(value) ? value : [value];
}

function decode(token: string | null): JwtPayload | null {
  if (!token) return null;
  try {
    const part = token.split('.')[1];
    const json = atob(part.replace(/-/g, '+').replace(/_/g, '/'));
    const payload = JSON.parse(decodeURIComponent(escape(json))) as JwtPayload;
    if (payload.exp && payload.exp * 1000 < Date.now()) return null;
    return payload;
  } catch {
    return null;
  }
}
