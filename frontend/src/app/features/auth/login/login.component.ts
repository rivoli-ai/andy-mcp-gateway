import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { AuthService } from '../../../core/services/auth.service';
import { AuthProviderConfig } from '../../../core/auth/oidc-config.loader';
import { CardComponent } from '../../../shared/components/card/card.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, CardComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent implements OnInit {
  private oidcSecurityService = inject(OidcSecurityService);

  loading = signal<boolean>(false);
  error = signal<string | null>(null);
  isRegisterMode = signal<boolean>(false);
  /** Set when the auth interceptor redirects here after a 401 (expired/invalid token). */
  sessionExpired = signal<boolean>(false);
  private returnUrl: string | null = null;

  email = '';
  password = '';
  name = '';
  confirmPassword = '';

  readonly isLocalEnabled = computed(() => this.authService.isLocalEnabled());
  readonly backendOAuthProviders = computed(() => this.authService.backendOAuthProviders());
  readonly frontendOidcProviders = computed(() => this.authService.frontendOidcProviders());
  readonly hasExternalProviders = computed(
    () => this.backendOAuthProviders().length > 0 || this.frontendOidcProviders().length > 0
  );

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  async ngOnInit(): Promise<void> {
    const params = this.route.snapshot.queryParamMap;
    const ret = params.get('returnUrl');
    this.returnUrl = ret && ret.startsWith('/') && !ret.startsWith('//') ? ret : null;
    if (params.get('sessionExpired') === '1') {
      this.sessionExpired.set(true);
    }

    if (this.authService.isLoggedIn()) {
      this.navigateAfterLogin();
      return;
    }

    await this.authService.loadProviderConfig();
  }

  private navigateAfterLogin(): void {
    void this.router.navigateByUrl(this.returnUrl ?? '/dashboard');
  }

  toggleMode(): void {
    this.isRegisterMode.update((v) => !v);
    this.error.set(null);
    this.password = '';
    this.confirmPassword = '';
  }

  async submitForm(): Promise<void> {
    if (!this.email || !this.password) {
      this.error.set('Email and password are required');
      return;
    }

    if (this.isRegisterMode()) {
      if (this.password !== this.confirmPassword) {
        this.error.set('Passwords do not match');
        return;
      }
      if (this.password.length < 8) {
        this.error.set('Password must be at least 8 characters');
        return;
      }
    }

    this.loading.set(true);
    this.error.set(null);

    try {
      if (this.isRegisterMode()) {
        await this.authService.registerAccount(this.email, this.password, this.name || undefined);
      } else {
        await this.authService.signInWithPassword(this.email, this.password);
      }
      this.navigateAfterLogin();
    } catch (err: unknown) {
      const anyErr = err as { error?: { message?: string }; message?: string };
      this.error.set(anyErr.error?.message || anyErr.message || 'Authentication failed');
    } finally {
      this.loading.set(false);
    }
  }

  loginWithBackendOAuth(provider: AuthProviderConfig): void {
    this.loading.set(true);
    this.error.set(null);

    void this.authService.loginWithProvider(provider.name).catch((err: unknown) => {
      const msg = err instanceof Error ? err.message : `Failed to initiate ${provider.name} login`;
      this.error.set(msg);
      this.loading.set(false);
    });
  }

  loginWithOidc(provider: AuthProviderConfig): void {
    this.loading.set(true);
    this.error.set(null);

    try {
      this.oidcSecurityService.authorize(provider.name);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : `Failed to initiate ${provider.name} login`;
      this.error.set(msg);
      this.loading.set(false);
    }
  }

  getProviderDisplayName(provider: AuthProviderConfig): string {
    const nameMap: Record<string, string> = {
      github: 'GitHub',
      azuread: 'Microsoft',
      duende: 'Duende'
    };
    return nameMap[provider.name.toLowerCase()] ?? provider.name;
  }
}
