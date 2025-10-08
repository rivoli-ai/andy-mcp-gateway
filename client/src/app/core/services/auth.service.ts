import { Injectable, inject, signal } from '@angular/core';
import { MsalService } from '@azure/msal-angular';
import { AccountInfo, AuthenticationResult } from '@azure/msal-browser';
import { BehaviorSubject, Observable, from } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private msalService = inject(MsalService);
  private accountSubject = new BehaviorSubject<AccountInfo | null>(null);
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  private isInitialized = false;

  // Signals for reactive programming
  public readonly isAuthenticated = signal(false);
  public readonly currentUser = signal<AccountInfo | null>(null);

  constructor() {
    // Don't initialize immediately to avoid race conditions
    // Initialize when explicitly called
  }

  async initialize(): Promise<void> {
    if (this.isInitialized) {
      return;
    }

    try {
      // MSAL should already be initialized by APP_INITIALIZER
      // Just check for existing accounts
      const accounts = this.msalService.instance.getAllAccounts();
      if (accounts.length > 0) {
        this.msalService.instance.setActiveAccount(accounts[0]);
        this.updateAuthState(accounts[0]);
      } else {
        // No accounts found, set initial state
        this.updateAuthState(null);
      }
      this.isInitialized = true;
    } catch (error) {
      console.error('Auth initialization failed:', error);
    }
  }

  public readonly isAuthenticated$ = this.isAuthenticatedSubject.asObservable();
  public readonly currentUser$ = this.accountSubject.asObservable();

  login(): void {
    try {
      // MSAL should already be initialized by APP_INITIALIZER
      const activeAccount = this.msalService.instance.getActiveAccount();
      const interactionInProgress = this.isInteractionInProgress();
      
      console.log('Login attempt - Active account:', activeAccount?.username, 'Interaction in progress:', interactionInProgress);
      
      if (activeAccount) {
        console.log('User already logged in');
        return;
      }
      
      if (interactionInProgress) {
        console.log('Login interaction already in progress, waiting...');
        return;
      }
      
      // Only start login if no interaction is in progress
      this.msalService.loginRedirect().subscribe({
        next: () => {
          console.log('Login redirect initiated');
        },
        error: (error) => {
          console.error('Login failed:', error);
          if (error.errorCode === 'interaction_in_progress') {
            console.log('Interaction already in progress, this is expected');
          }
        }
      });
    } catch (error) {
      console.error('Login failed:', error);
    }
  }

  logout(): void {
    this.msalService.logoutRedirect();
  }

  getAccessToken(): Observable<string> {
    const account = this.msalService.instance.getActiveAccount();
    if (!account) {
      throw new Error('No active account');
    }

    return from(this.msalService.acquireTokenSilent({
      scopes: [environment.azureAd.scopes.apiAccess, environment.azureAd.scopes.userRead],
      account: account
    })).pipe(
      map(response => response.accessToken),
      catchError(error => {
        console.error('Token acquisition failed:', error);
        throw error;
      })
    );
  }

  private updateAuthState(account: AccountInfo | null): void {
    this.accountSubject.next(account);
    const isAuth = account !== null;
    this.isAuthenticatedSubject.next(isAuth);
    this.isAuthenticated.set(isAuth);
    this.currentUser.set(account);
  }

  private isInteractionInProgress(): boolean {
    try {
      return localStorage.getItem('msal.interaction.status') === 'interaction_in_progress' ||
             sessionStorage.getItem('msal.interaction.status') === 'interaction_in_progress';
    } catch {
      return false;
    }
  }

  public clearInteractionStatus(): void {
    try {
      localStorage.removeItem('msal.interaction.status');
      sessionStorage.removeItem('msal.interaction.status');
      console.log('Cleared interaction status');
    } catch (error) {
      console.error('Failed to clear interaction status:', error);
    }
  }
}
