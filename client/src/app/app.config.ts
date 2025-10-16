import { ApplicationConfig, importProvidersFrom, APP_INITIALIZER } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptorsFromDi, HTTP_INTERCEPTORS } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { MsalService, MsalGuard, MsalInterceptor, MsalBroadcastService, MSAL_INSTANCE, MSAL_GUARD_CONFIG, MSAL_INTERCEPTOR_CONFIG } from '@azure/msal-angular';
import { PublicClientApplication, InteractionType, BrowserCacheLocation, LogLevel } from '@azure/msal-browser';
import { MarkdownModule } from 'ngx-markdown';

import { routes } from './app.routes';
import { msalConfig, protectedResources } from '../auth-config';
import { environment } from '../environments/environment';

export function MSALInstanceFactory(): PublicClientApplication {
  return new PublicClientApplication(msalConfig);
}

export function MSALGuardConfigFactory() {
  return {
    interactionType: InteractionType.Redirect,
    authRequest: {
      scopes: [environment.azureAd.scopes.apiAccess, environment.azureAd.scopes.userRead]
    }
  };
}

export function MSALInterceptorConfigFactory() {
  const protectedResourceMap = new Map<string, Array<string>>();
  protectedResourceMap.set(protectedResources.api.endpoint, protectedResources.api.scopes);

  console.log('MSAL Interceptor Config - Protected Resources:', {
    endpoint: protectedResources.api.endpoint,
    scopes: protectedResources.api.scopes,
    mapSize: protectedResourceMap.size
  });

  // Log what URLs will be protected
  protectedResourceMap.forEach((scopes, endpoint) => {
    console.log(`Protected endpoint: ${endpoint} with scopes:`, scopes);
  });

  return {
    interactionType: InteractionType.Redirect,
    protectedResourceMap,
  };
}

export function initializeMsal(msalService: MsalService, msalBroadcastService: MsalBroadcastService) {
  return (): Promise<void> => {
    return msalService.instance.initialize().then(() => {
      console.log('MSAL initialized successfully');
      
      // Check for existing accounts
      const accounts = msalService.instance.getAllAccounts();
      console.log('Existing accounts:', accounts.length);
      if (accounts.length > 0) {
        console.log('Setting active account:', accounts[0].username);
        msalService.instance.setActiveAccount(accounts[0]);
      }
      
      // Handle redirect promise if returning from login
      msalService.handleRedirectObservable().subscribe({
        next: (response) => {
          if (response) {
            console.log('Login redirect successful:', response);
            msalService.instance.setActiveAccount(response.account);
            // Clear any interaction status after successful login
            localStorage.removeItem('msal.interaction.status');
            sessionStorage.removeItem('msal.interaction.status');
          }
        },
        error: (error) => {
          console.error('Login redirect error:', error);
          // Clear interaction status on error as well
          localStorage.removeItem('msal.interaction.status');
          sessionStorage.removeItem('msal.interaction.status');
        }
      });
    }).catch((error) => {
      console.error('MSAL initialization failed:', error);
      // Clear interaction status on initialization failure
      localStorage.removeItem('msal.interaction.status');
      sessionStorage.removeItem('msal.interaction.status');
      throw error;
    });
  };
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptorsFromDi()),
    importProvidersFrom(MarkdownModule.forRoot()),
    {
      provide: MSAL_INSTANCE,
      useFactory: MSALInstanceFactory,
    },
    {
      provide: MSAL_GUARD_CONFIG,
      useFactory: MSALGuardConfigFactory,
    },
    {
      provide: MSAL_INTERCEPTOR_CONFIG,
      useFactory: MSALInterceptorConfigFactory,
    },
    MsalService,
    MsalGuard,
    MsalBroadcastService,
    {
      provide: HTTP_INTERCEPTORS,
      useClass: MsalInterceptor,
      multi: true,
    },
    {
      provide: APP_INITIALIZER,
      useFactory: initializeMsal,
      deps: [MsalService, MsalBroadcastService],
      multi: true,
    },
  ]
};