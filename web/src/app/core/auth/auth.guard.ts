import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from './auth.store';

/** Blocks routes unless authenticated and (optionally) holding `data.permission`. */
export const authGuard: CanActivateFn = (route) => {
  const store = inject(AuthStore);
  const router = inject(Router);

  if (!store.isAuthenticated()) {
    return router.createUrlTree(['/login']);
  }

  const required = route.data?.['permission'] as string | undefined;
  if (required && !store.has(required)) {
    return router.createUrlTree(['/dashboard']);
  }
  return true;
};
