import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Route guard that restricts navigation to operators carrying the <c>admin</c> role
 * on their gateway JWT. Non-admins are redirected to the dashboard rather than the
 * login screen — the user is logged in, just not entitled to the page.
 */
export const adminGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isLoggedIn()) {
    void router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
    return false;
  }

  if (!auth.isAdmin()) {
    void router.navigate(['/dashboard']);
    return false;
  }

  return true;
};
