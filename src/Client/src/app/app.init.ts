import { inject } from '@angular/core';
import { ThemeService } from '@ske/theme';
import { AntiforgeryHttp } from './core/auth/services/antiforgery.http';
import { firstValueFrom } from 'rxjs';

export const appInitializer = async () => {
  const themeService = inject(ThemeService);

  await themeService.loadTheme(true);
};

// Seeds the "XSRF-TOKEN" cookie (see AntiforgeryService) before the app renders, so it exists
// ahead of the first mutating request (e.g. register/login).
// export const authInitializer = async () => {
//   const antiforgery = inject(AntiforgeryHttp);
//
//   await firstValueFrom(antiforgery.refreshToken());
// };
