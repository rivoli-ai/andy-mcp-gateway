import { InjectionToken } from '@angular/core';

export interface AppConfig {
  apiUrl: string;
}

/**
 * Runtime app configuration loaded from `/assets/config/config.json`.
 * Provided in `main.ts` before bootstrapping so it's available everywhere via
 * `@Inject(APP_CONFIG)`.
 */
export const APP_CONFIG = new InjectionToken<AppConfig>('APP_CONFIG');

export const DEFAULT_CONFIG: AppConfig = {
  apiUrl: 'http://localhost:5080',
};

