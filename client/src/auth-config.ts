import { Configuration, PopupRequest } from '@azure/msal-browser';
import { environment } from './environments/environment';

export const msalConfig: Configuration = {
  auth: {
    clientId: environment.azureAd.clientId,
    authority: environment.azureAd.authority,
    redirectUri: environment.azureAd.redirectUri,
    postLogoutRedirectUri: environment.azureAd.postLogoutRedirectUri,
  },
  cache: {
    cacheLocation: 'localStorage',
    storeAuthStateInCookie: false,
  },
  system: {
    loggerOptions: {
      loggerCallback: (level, message) => {
        if (level === 3) { // Error level
          console.info(message);
        }
      },
      logLevel: 3, // Error
      piiLoggingEnabled: false,
    },
  },
};

export const protectedResources = environment.azureAd.protectedResources;

export const loginRequest: PopupRequest = {
  scopes: [environment.azureAd.scopes.userRead, environment.azureAd.scopes.apiAccess],
};
