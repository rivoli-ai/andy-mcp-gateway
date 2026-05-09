import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';
import { APP_CONFIG, AppConfig, DEFAULT_CONFIG } from './app/core/services/config.service';

fetch('/assets/config/config.json')
  .then((res) => res.json())
  .catch(() => DEFAULT_CONFIG)
  .then((cfg: Partial<AppConfig>) => {
    const config: AppConfig = { ...DEFAULT_CONFIG, ...cfg };

    return bootstrapApplication(AppComponent, {
      ...appConfig,
      providers: [...appConfig.providers, { provide: APP_CONFIG, useValue: config }],
    });
  })
  .catch((err) => console.error(err));
