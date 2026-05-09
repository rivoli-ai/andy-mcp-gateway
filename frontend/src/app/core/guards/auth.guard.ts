import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  try {
    if (!authService.isLoggedIn()) {
      void router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
      return false;
    }
    return true;
  } catch (error) {
    console.error('Auth guard error:', error);
    return false;
  }
};
