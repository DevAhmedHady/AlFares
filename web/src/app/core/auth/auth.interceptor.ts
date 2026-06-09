import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { API_BASE } from '../config';
import { AuthService } from './auth.service';
import { AuthStore } from './auth.store';

/** Attaches the bearer token and transparently refreshes once on 401. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const store = inject(AuthStore);
  const auth = inject(AuthService);
  const router = inject(Router);
  const base = inject(API_BASE);

  const isAuthCall = req.url.includes('/api/auth/') || req.url.includes('/api/tenants/default');
  const token = store.access;
  const authReq = token && !isAuthCall
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !isAuthCall && store.refresh) {
        return auth.refresh().pipe(
          switchMap((tokens) =>
            next(req.clone({ setHeaders: { Authorization: `Bearer ${tokens.accessToken}` } }))),
          catchError((refreshError) => {
            store.clear();
            router.navigate(['/login']);
            return throwError(() => refreshError);
          }),
        );
      }
      if (error.status === 401) {
        store.clear();
        router.navigate(['/login']);
      }
      void base;
      return throwError(() => error);
    }),
  );
};
