import {
  ApplicationConfig,
  DEFAULT_CURRENCY_CODE, LOCALE_ID,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners
} from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { provideNzI18n, ro_RO } from 'ng-zorro-antd/i18n';
import { registerLocaleData } from '@angular/common';
import ro from '@angular/common/locales/ro';
import { provideNzDateFnsAdapter } from 'ng-zorro-antd/core/time';
import { provideHttpClient, withInterceptors, withXsrfConfiguration } from '@angular/common/http';
import { appInitializer } from './app.init';
import { authInterceptor } from '@ske/auth';
import { provideSignalR, realtimeEventNames, realtimeEvents } from '@ske/signalr';

registerLocaleData(ro);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    // Cookie/header names match Angular's own defaults; named explicitly for clarity and to stay
    // in lockstep with AddAntiforgery(...) in src/Web/DependencyInjection.cs.
    provideHttpClient(
      withXsrfConfiguration({ cookieName: 'XSRF-TOKEN', headerName: 'X-XSRF-TOKEN' }),
      withInterceptors([authInterceptor])
    ),
    provideAppInitializer(appInitializer),
    // provideAppInitializer(authInitializer),
    provideNzI18n(ro_RO),
    provideNzDateFnsAdapter(),
    { provide: DEFAULT_CURRENCY_CODE, useValue: 'RON' },
    { provide: LOCALE_ID, useValue: 'ro' },
    provideSignalR({
      url: '/hubs/app',
      eventMap: {
        [realtimeEventNames.categoryCreated]: realtimeEvents.categoryCreated,
        [realtimeEventNames.categoryUpdated]: realtimeEvents.categoryUpdated,
        [realtimeEventNames.categoryImportCreated]: realtimeEvents.categoryImportCreated,
        [realtimeEventNames.categoryImportProcessed]: realtimeEvents.categoryImportProcessed,
        [realtimeEventNames.categoryImportConfirmed]: realtimeEvents.categoryImportConfirmed,
        [realtimeEventNames.goodsReceiptImportCreated]: realtimeEvents.goodsReceiptImportCreated,
        [realtimeEventNames.goodsReceiptImportProcessed]: realtimeEvents.goodsReceiptImportProcessed,
        [realtimeEventNames.goodsReceiptImportConfirmed]: realtimeEvents.goodsReceiptImportConfirmed
      }
    })
  ]
};
