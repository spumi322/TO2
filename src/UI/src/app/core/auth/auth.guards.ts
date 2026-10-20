import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SessionStore } from './session.store';

// The session is restored (refresh) before the first navigation, see app.config.ts

export const authGuard: CanActivateFn = (_route, state) => {
  if (inject(SessionStore).isLoggedIn()) return true;
  return inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

export const guestGuard: CanActivateFn = () => {
  if (!inject(SessionStore).isLoggedIn()) return true;
  return inject(Router).createUrlTree(['/tournaments']);
};
