import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { switchMap } from 'rxjs';
import { AntiforgeryHttp } from './antiforgery.http';
import { Credentials, UserInfo } from '@ske/models';

/**
 * Cookie-based authentication against MapIdentityApi (src/Web/Endpoints/Users.cs), mapped under
 * /api/Users. Login/logout are handled entirely via the "Identity.Application" auth cookie, which
 * ASP.NET Core's cookie middleware renews transparently (sliding expiration) — there is no
 * client-side refresh call for the auth cookie itself, unlike the bearer-token flow.
 *
 * Every mutating call here must go through same-origin paths (dev proxy / prod reverse proxy) so
 * the cookie and its antiforgery pairing behave as same-site.
 */
@Service()
export class AuthHttp {
  private readonly _httpClient = inject(HttpClient);
  private readonly antiforgery = inject(AntiforgeryHttp);

  public login(credentials: Credentials) {
    return this._httpClient
      .post<void>('/api/Users/login?useCookies=true', credentials)
      // .pipe(
      //   // The antiforgery token embeds the current principal, so the anonymous token seeded at
      //   // bootstrap is no longer valid once authenticated — re-seed immediately.
      //   switchMap(() => this.antiforgery.refreshToken()),
      // );
  }

  public logout() {
    return this._httpClient.post<void>('/api/Users/logout', {})
    //   .pipe(
    //   // Re-seed for the now-anonymous principal.
    //   switchMap(() => this.antiforgery.refreshToken()),
    // );
  }

  public getUserInfo(){
    return this._httpClient.get<UserInfo>('/api/Users/manage/info');
  }
}
