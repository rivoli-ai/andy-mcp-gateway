import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { MsalService } from '@azure/msal-angular';

export const authGuard: CanActivateFn = (route, state) => {
  const msalService = inject(MsalService);
  const router = inject(Router);

  try {
    // MSAL should already be initialized by APP_INITIALIZER
    const account = msalService.instance.getActiveAccount();

    console.log('Auth guard check - Active account:', account?.username);

    if (!account) {
      // Check if an interaction is already in progress to avoid multiple redirects
      const interactionInProgress = localStorage.getItem('msal.interaction.status') === 'interaction_in_progress' ||
                                   sessionStorage.getItem('msal.interaction.status') === 'interaction_in_progress';

      console.log('Auth guard - No active account, interaction in progress:', interactionInProgress);

      if (!interactionInProgress) {
        console.log('Auth guard - Starting login redirect');
        // Redirect to login only if no interaction is in progress
        msalService.loginRedirect().subscribe({
          next: () => {
            console.log('Auth guard - Login redirect initiated');
          },
          error: (error) => {
            console.error('Auth guard - Login redirect error:', error);
            if (error.errorCode === 'interaction_in_progress') {
              console.log('Auth guard - Interaction already in progress, this is expected');
            }
          }
        });
      } else {
        console.log('Auth guard - Interaction already in progress, waiting...');
      }
      return false;
    }

    console.log('Auth guard - User authenticated, allowing access');
    return true;
  } catch (error) {
    console.error('Auth guard error:', error);
    return false;
  }
};
