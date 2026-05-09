import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { AuthService } from '../../../core/services/auth.service';
import { take, finalize } from 'rxjs/operators';

/**
 * OIDC callback — aligned with DevPilot: do not strip ?code/?state before checkAuth();
 * angular-auth-oidc-client may rely on the current URL during the authorization_code exchange.
 * After tokens are returned, strip the query string so refresh cannot re-send a redeemed code.
 */
@Component({
  selector: 'app-callback',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="callback-container">
      <div class="callback-card">
        @if (loading()) {
          <div class="spinner"></div>
          <h2>{{ statusMessage() }}</h2>
          <p class="subtitle">Please wait...</p>
        } @else if (error()) {
          <div class="error-icon">
            <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M12 8V12M12 16H12.01M21 12C21 16.9706 16.9706 21 12 21C7.02944 21 3 16.9706 3 12C3 7.02944 7.02944 3 12 3C16.9706 3 21 7.02944 21 12Z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
          </div>
          <h2>Authentication failed</h2>
          <p class="error-message">{{ error() }}</p>
          <button (click)="goToLogin()" class="back-button">Back to Login</button>
        } @else {
          <div class="success-icon">
            <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M9 12L11 14L15 10M21 12C21 16.9706 16.9706 21 12 21C7.02944 21 3 16.9706 3 12C3 7.02944 7.02944 3 12 3C16.9706 3 21 7.02944 21 12Z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
          </div>
          <h2>Login successful!</h2>
          <p class="subtitle">Redirecting...</p>
        }
      </div>
    </div>
  `,
  styles: [`
    .callback-container {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--surface-ground);
      padding: 2rem;
    }
    .callback-card {
      background: var(--surface-card);
      border: 1px solid var(--border-default);
      border-radius: 16px;
      padding: 3rem;
      box-shadow: 0 20px 60px rgba(0, 0, 0, 0.15);
      max-width: 400px;
      width: 100%;
      text-align: center;
    }
    h2 {
      margin: 0 0 0.5rem 0;
      color: var(--text-primary);
      font-size: 1.5rem;
      font-weight: 600;
    }
    .subtitle {
      color: var(--text-secondary);
      margin: 0;
    }
    .spinner {
      width: 48px;
      height: 48px;
      border: 4px solid var(--border-light);
      border-top-color: var(--brand-primary);
      border-radius: 50%;
      animation: spin 1s linear infinite;
      margin: 0 auto 1.5rem;
    }
    @keyframes spin {
      to { transform: rotate(360deg); }
    }
    .error-icon, .success-icon {
      width: 56px;
      height: 56px;
      margin: 0 auto 1rem;
    }
    .error-icon svg {
      width: 100%;
      height: 100%;
      color: #ef4444;
    }
    .success-icon svg {
      width: 100%;
      height: 100%;
      color: #10b981;
    }
    .error-message {
      color: #ef4444;
      margin: 1rem 0;
      font-size: 0.875rem;
    }
    .back-button {
      margin-top: 1.5rem;
      padding: 0.75rem 1.5rem;
      background: linear-gradient(135deg, var(--brand-primary) 0%, #4f46e5 100%);
      color: white;
      border: none;
      border-radius: 8px;
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }
    .back-button:hover {
      transform: translateY(-1px);
      box-shadow: 0 4px 12px rgba(99, 102, 241, 0.4);
    }
  `]
})
export class CallbackComponent implements OnInit {
  /** Prevents double token exchange for the same auth code (AADSTS54005). */
  private static readonly oidcCodeLockPrefix = 'mcpgateway.oidc.code.';

  loading = signal<boolean>(true);
  error = signal<string | null>(null);
  statusMessage = signal<string>('Completing login...');

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private oidcSecurityService: OidcSecurityService,
    private authService: AuthService
  ) {}

  async ngOnInit(): Promise<void> {
    await this.authService.loadProviderConfig();

    const provider = this.route.snapshot.paramMap.get('provider') ?? '';
    const config = this.authService.getProviderConfig(provider);

    if (!config || config.type !== 'FrontendOidc') {
      this.error.set(`Unknown provider: ${provider}`);
      this.loading.set(false);
      return;
    }

    this.route.queryParams.pipe(take(1)).subscribe((params) => {
      const errorParam = params['error'];
      const errorDescription = params['error_description'];
      if (errorParam) {
        this.error.set(`${provider} authorization failed: ${errorDescription || errorParam}`);
        this.loading.set(false);
        return;
      }

      const hasCode = typeof window !== 'undefined' && window.location.search.includes('code=');
      const hasState = typeof window !== 'undefined' && window.location.search.includes('state=');
      if (typeof window !== 'undefined' && !hasCode && !hasState) {
        this.error.set('Missing authorization response in URL. Please try signing in again.');
        this.loading.set(false);
        return;
      }

      this.handleOidcCallback(provider);
    });
  }

  private handleOidcCallback(provider: string): void {
    this.statusMessage.set(`Completing ${provider} sign-in...`);
    const url = typeof window !== 'undefined' ? window.location.href : this.router.url;

    const authCode =
      typeof window !== 'undefined' ? new URLSearchParams(window.location.search).get('code') : null;

    const lockTtlMs = 3 * 60 * 1000;
    let codeLockKey: string | null = null;
    if (authCode) {
      codeLockKey = `${CallbackComponent.oidcCodeLockPrefix}${authCode}`;
      const raw = sessionStorage.getItem(codeLockKey);
      if (raw) {
        const ts = parseInt(raw, 10);
        const fresh = !Number.isNaN(ts) && Date.now() - ts < lockTtlMs;
        if (fresh) {
          console.warn(
            '[Auth callback] Same authorization code is already being processed; refusing second exchange (AADSTS54005 guard).'
          );
          if (this.authService.isLoggedIn()) {
            void this.router.navigateByUrl('/');
            return;
          }
          this.error.set(
            'This sign-in link was already used. Return to login and start a new sign-in.'
          );
          this.loading.set(false);
          return;
        }
        sessionStorage.removeItem(codeLockKey);
      }
      sessionStorage.setItem(codeLockKey, String(Date.now()));
    }

    const hasCode = typeof window !== 'undefined' && window.location.search.includes('code=');
    const hasState = typeof window !== 'undefined' && window.location.search.includes('state=');
    console.debug('[Auth callback]', {
      provider,
      configId: provider,
      pathname: typeof window !== 'undefined' ? window.location.pathname : '',
      hasCode,
      hasState,
      hashLength: typeof window !== 'undefined' ? (window.location.hash?.length ?? 0) : 0,
    });

    // take(1): checkAuth must not run two token exchanges for one callback URL (second = invalid_grant / 54005).
    this.oidcSecurityService
      .checkAuth(url, provider)
      .pipe(
        take(1),
        finalize(() => {
          if (codeLockKey) sessionStorage.removeItem(codeLockKey);
        })
      )
      .subscribe({
        next: (loginResponse) => {
          if (!loginResponse.isAuthenticated || !loginResponse.accessToken) {
            console.warn('[Auth callback] OIDC checkAuth returned unsuccessful:', {
              isAuthenticated: loginResponse.isAuthenticated,
              hasAccessToken: !!loginResponse.accessToken,
              hasIdToken: !!loginResponse.idToken,
              errorMessage: loginResponse.errorMessage ?? null,
              configId: loginResponse.configId ?? null,
            });
            this.error.set(loginResponse.errorMessage || `${provider} sign-in did not return a token`);
            this.loading.set(false);
            return;
          }

          // Code is redeemed; remove query from address bar before backend exchange / navigation.
          if (typeof window !== 'undefined' && window.history?.replaceState) {
            const cleanUrl = `${window.location.origin}${window.location.pathname}`;
            window.history.replaceState({}, '', cleanUrl);
          }

          const idToken = loginResponse.idToken ?? '';
          const accessToken = loginResponse.accessToken ?? '';

          this.authService.handleOidcTokenLogin(provider, idToken, accessToken).subscribe({
            next: () => {
              this.loading.set(false);
              void this.router.navigateByUrl('/');
            },
            error: (err: unknown) => {
              const anyErr = err as { error?: { message?: string }; message?: string };
              this.error.set(anyErr?.error?.message || anyErr?.message || 'Token login failed');
              this.loading.set(false);
            }
          });
        },
        error: (err: unknown) => {
          const msg = err instanceof Error ? err.message : String(err);
          console.error('[Auth callback] OIDC checkAuth failed:', msg, err);
          if (typeof msg === 'string' && /invalid_grant|54005|already redeemed/i.test(msg)) {
            this.error.set(
              'Microsoft sign-in was already completed with this link. Go back to login and sign in again.'
            );
          } else {
            this.error.set(msg || `Failed to complete ${provider} sign-in`);
          }
          this.loading.set(false);
        }
      });
  }

  goToLogin(): void {
    void this.router.navigate(['/login']);
  }
}
