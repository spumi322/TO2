import { provideHttpClient } from '@angular/common/http';
import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { MessageService } from 'primeng/api';
import { providePrimeNG } from 'primeng/config';
import { routes } from './app.routes';
import { To2Preset } from './core/ui/theme-preset';
import { ThemeService } from './core/ui/theme.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(),
    providePrimeNG({
      theme: { preset: To2Preset, options: { darkModeSelector: "[data-theme='dark']" } },
    }),
    MessageService,
    provideAppInitializer(() => inject(ThemeService).init()),
  ],
};
