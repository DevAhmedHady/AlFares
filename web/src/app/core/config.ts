import { InjectionToken } from '@angular/core';

/** Base URL of the الفارس API. Overridable for different environments. */
export const API_BASE = new InjectionToken<string>('API_BASE', {
  providedIn: 'root',
  factory: () => 'http://localhost:5113',
});
