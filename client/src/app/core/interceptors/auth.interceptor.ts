import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

/**
 * Adds the gateway JWT to outbound requests (when the caller hasn't already set Authorization).
 * On 401 for app requests, clears the session and redirects to /login with returnUrl.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const token = authService.getToken();
  const usesAppJwt = !req.headers.has('Authorization');
  const attachJwt =
    Boolean(token && usesAppJwt && !shouldSkipBearerForAnonymousAuth(req.url));

  const outgoing = attachJwt ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(outgoing).pipe(
    catchError((err: unknown) => {
      if (
        err instanceof HttpErrorResponse &&
        err.status === 401 &&
        attachJwt &&
        shouldRedirectOnUnauthorized(req.url, authService)
      ) {
        handleSessionExpired(authService, router);
      }
      return throwError(() => err);
    })
  );
};

function shouldRedirectOnUnauthorized(url: string, authService: AuthService): boolean {
  if (!authService.isLoggedIn()) return false;
  // Don't treat failures on anonymous auth endpoints as "session expired".
  if (/\/api\/auth\/config(\?|$|#)/i.test(url)) return false;
  if (/\/api\/auth\/[^/]+\/token(\?|$|#)/i.test(url)) return false;
  if (/\/api\/auth\/(login|register|callback|providers|authorize)(\/|$|\?|#)/i.test(url)) return false;
  return true;
}

/**
 * OIDC token exchange and public config must not send a stale gateway JWT: ASP.NET JWT bearer
 * may reject invalid/expired Bearer tokens before [AllowAnonymous] runs.
 */
function shouldSkipBearerForAnonymousAuth(url: string): boolean {
  return /\/api\/auth\/config(\?|$|#)/i.test(url) || /\/api\/auth\/[^/]+\/token(\?|$|#)/i.test(url);
}

function handleSessionExpired(authService: AuthService, router: Router): void {
  authService.clearSession();
  const current = router.url || '/';
  if (current.startsWith('/login')) return;
  void router.navigate(['/login'], {
    replaceUrl: true,
    queryParams: { returnUrl: current, sessionExpired: '1' }
  });
}

 