import { Injectable, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap, firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { AuthProviderConfig, AuthConfigResponse } from '../auth/oidc-config.loader';

export interface AuthUser {
  id?: string;
  email: string;
  name?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly tokenSignal = signal<string | null>(null);
  private readonly userSignal = signal<{ email?: string; name?: string } | null>(null);
  readonly isAuthenticated = signal<boolean>(false);

  /** Use in templates so the app shell updates when login completes (signal-backed). */
  readonly loggedIn = computed(() => this.isAuthenticated() && this.tokenSignal() !== null);

  readonly user = this.userSignal.asReadonly();

  private readonly _providerConfigs = signal<AuthProviderConfig[]>([]);
  private _configLoaded = false;

  readonly providerConfigs = this._providerConfigs.asReadonly();
  readonly frontendOidcProviders = computed(() =>
    this._providerConfigs().filter((p) => p.type === 'FrontendOidc')
  );

  readonly localProviders = computed(() => this._providerConfigs().filter((p) => p.type === 'Local'));
  readonly backendOAuthProviders = computed(() =>
    this._providerConfigs().filter((p) => p.type === 'BackendOAuth')
  );
  readonly isLocalEnabled = computed(() => this.localProviders().length > 0);

  constructor(
    private apiService: ApiService,
    private router: Router
  ) {
    const savedToken = localStorage.getItem('auth_token');
    const savedUser = localStorage.getItem('auth_user');
    if (savedToken) {
      if (this.isJwtExpired(savedToken)) {
        localStorage.removeItem('auth_token');
        localStorage.removeItem('auth_user');
      } else {
        this.tokenSignal.set(savedToken);
        this.isAuthenticated.set(true);
      }
    }
    if (savedUser && this.tokenSignal() !== null) {
      try {
        this.userSignal.set(JSON.parse(savedUser));
      } catch {
        // ignore
      }
    }
  }

  /** True when JWT is missing/malformed or `exp` is in the past (DevPilot parity). */
  private isJwtExpired(token: string): boolean {
    try {
      const parts = token.split('.');
      if (parts.length < 2) return true;
      const payload = JSON.parse(atob(parts[1]));
      const exp: unknown = payload?.exp;
      if (typeof exp !== 'number') return false;
      const skewSeconds = 5;
      return exp <= Math.floor(Date.now() / 1000) - skewSeconds;
    } catch {
      return true;
    }
  }

  getToken(): string | null {
    return this.tokenSignal();
  }

  isLoggedIn(): boolean {
    return this.loggedIn();
  }

  /** Clears tokens and user state only (no navigation). */
  clearSession(): void {
    localStorage.removeItem('auth_token');
    localStorage.removeItem('auth_user');
    this.tokenSignal.set(null);
    this.userSignal.set(null);
    this.isAuthenticated.set(false);
  }

  logout(): void {
    this.clearSession();
    void this.router.navigate(['/login'], { replaceUrl: true });
  }

  async loadProviderConfig(): Promise<AuthProviderConfig[]> {
    if (this._configLoaded) return this._providerConfigs();
    try {
      const response = await firstValueFrom(this.apiService.get<AuthConfigResponse>('/api/auth/config'));
      this._providerConfigs.set(response.providers);
      this._configLoaded = true;
      return response.providers;
    } catch (err) {
      console.error('Failed to load auth provider config', err);
      return [];
    }
  }

  getProviderConfig(name: string): AuthProviderConfig | undefined {
    return this._providerConfigs().find((p) => p.name.toLowerCase() === name.toLowerCase());
  }

  handleOidcTokenLogin(provider: string, idToken: string, accessToken?: string): Observable<{ token: string; user?: any }> {
    return this.apiService
      .post<{ token: string; user?: any }>(`/api/auth/${provider}/token`, { idToken, accessToken })
      .pipe(tap((response) => this.setAuthState(response)));
  }

  /** Backend-driven OAuth redirect (when `BackendOAuth` providers exist). */
  async loginWithProvider(provider: string): Promise<void> {
    const response = await firstValueFrom(
      this.apiService.get<{ authorizationUrl: string }>(`/api/auth/${provider}/authorize`)
    );
    if (response.authorizationUrl) {
      window.location.href = response.authorizationUrl;
    }
  }

  async registerAccount(email: string, password: string, name?: string): Promise<void> {
    const response = await firstValueFrom(
      this.apiService.post<{ token: string; user?: AuthUser }>('/api/auth/register', { email, password, name })
    );
    this.setAuthState(response);
  }

  async signInWithPassword(email: string, password: string): Promise<void> {
    const response = await firstValueFrom(
      this.apiService.post<{ token: string; user?: AuthUser }>('/api/auth/login', { email, password })
    );
    this.setAuthState(response);
  }

  private setAuthState(response: { token: string; user?: any }): void {
    this.tokenSignal.set(response.token);
    this.isAuthenticated.set(true);
    localStorage.setItem('auth_token', response.token);
    if (response.user) {
      this.userSignal.set(response.user);
      localStorage.setItem('auth_user', JSON.stringify(response.user));
    }
  }
}
