import { ApplicationConfig, importProvidersFrom } from '@angular/core';
import { provideRouter } from '@angular/router';
import { HttpClient, provideHttpClient, withInterceptors, withInterceptorsFromDi } from '@angular/common/http';
import { provideAuth, StsConfigHttpLoader, StsConfigLoader } from 'angular-auth-oidc-client';
import { MarkdownModule } from 'ngx-markdown';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { loadOidcConfigs } from './core/auth/oidc-config.loader';
import { APP_CONFIG, AppConfig } from './core/services/config.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptorsFromDi(), withInterceptors([authInterceptor])),
    importProvidersFrom(MarkdownModule.forRoot()),
    provideAuth({
      loader: {
        provide: StsConfigLoader,
        useFactory: (http: HttpClient, config: AppConfig) =>
          new StsConfigHttpLoader(loadOidcConfigs(http, config.apiUrl)),
        deps: [HttpClient, APP_CONFIG],
      },
    }),
  ]
};