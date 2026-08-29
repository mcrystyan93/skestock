import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthStore } from '@ske/auth';
import { toObservable } from '@angular/core/rxjs-interop';
import { filter, map, take } from 'rxjs';

export const guestGuard: CanActivateFn = () => {
  const authStore = inject(AuthStore);
  const router = inject(Router);

  return toObservable(authStore.authLoading).pipe(
    filter((isLoading) => !isLoading),
    take(1),
    map(() => (authStore.isAuthenticated() ? router.createUrlTree(['/home']) : true))
  );
};
