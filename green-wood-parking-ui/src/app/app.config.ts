import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { ApplicationConfig, InjectionToken, isDevMode, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';

export const BASE_URL = new InjectionToken<string>('BASE_URL');

const apiUrl = isDevMode()
  ? 'https://localhost:7196'
  : 'https://custplace.ru/api';

export const appConfig: ApplicationConfig = {
  providers: [
    provideHttpClient(withInterceptorsFromDi()),
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    { provide: BASE_URL, useValue: apiUrl }
  ]
};
