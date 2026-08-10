import { InjectionToken, isDevMode } from '@angular/core';

/** Base URL of the الفارس API. Empty in prod = same-origin (SPA served by the API). */
export const API_BASE = new InjectionToken<string>('API_BASE', {
  providedIn: 'root',
  factory: () => (isDevMode() ? 'http://localhost:5113' : ''),
});
