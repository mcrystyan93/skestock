import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { EMPTY } from 'rxjs';

/**
 * Seeds the JS-readable "XSRF-TOKEN" cookie issued by GET /api/Antiforgery/token.
 * Angular's HttpClient (configured with withXsrfConfiguration in app.config.ts) automatically
 * echoes that cookie's value back as the "X-XSRF-TOKEN" header on state-changing requests, so
 * nothing else needs to read/attach it manually.
 *
 * The token is bound to the current principal, so it must be re-seeded whenever the
 * authenticated identity changes (after login and after logout) in addition to being seeded
 * once at app bootstrap, before any anonymous mutation (e.g. register) is possible.
 */
@Service()
export class AntiforgeryHttp {
  private readonly http = inject(HttpClient);

  refreshToken() {
    //return this.http.get<void>('/api/Antiforgery/token');
    return EMPTY;
  }
}
