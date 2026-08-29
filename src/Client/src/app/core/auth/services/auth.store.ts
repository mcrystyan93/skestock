import { patchState, signalStore, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { ActivatedRoute, Router } from '@angular/router';
import { inject } from '@angular/core';
import { Credentials } from '@ske/models';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap, tap } from 'rxjs';
import { AuthHttp } from './auth.http';
import { mapResponse } from '@ngrx/operators';

type AuthState = { isAuthenticated: boolean };

const initialState: AuthState = { isAuthenticated: false };

export const AuthStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withLoadingFeature('auth'),
  withLoadingFeature('login'),
  withProblemDetailsFeature('auth'),
  withProps(() => ({
    activatedRoute: inject(ActivatedRoute),
    router: inject(Router),
    authHttp: inject(AuthHttp)
  })),
  withMethods((store) => {
    const login = rxMethod<Credentials>(
      pipe(
        tap(() => {
          store.clearAuthErrors();
          store.setLoginLoading();
          patchState(store, { isAuthenticated: true });
        }),
        switchMap(payload =>
          store.authHttp.login(payload)
            .pipe(
              mapResponse({
                next: () => {
                  store.setLoginLoaded();
                  store.router.navigate(['./home']);
                },
                error: (error) => {
                  store.handleAuthError(error);
                  store.setLoginLoaded();

                  store.router.navigate(['./login']);
                }
              })
            )
        )
      )
    );

    const logout = rxMethod(
      pipe(
        tap(() => {
          store.clearAuthErrors();
          store.setLoginLoading();
        }),
        switchMap(() =>
          store.authHttp.logout()
            .pipe(
              mapResponse({
                next: () => {
                  patchState(store, { isAuthenticated: false });
                  store.setLoginLoaded();
                  store.router.navigate(['./login']);
                },
                error: (error) => {
                  patchState(store, { isAuthenticated: true });
                  store.handleAuthError(error);
                  store.setLoginLoaded();
                  // we still route to login
                  store.router.navigate(['./login']);
                }
              })
            )
        )
      )
    );

    const load = rxMethod<void>(
      pipe(
        tap(() => {
          store.setAuthLoading();
        }),
        // fetch user info
        switchMap(() => store.authHttp.getUserInfo()
          .pipe(
            mapResponse({
              next: () => {
                // cookie still valid
                store.setAuthLoaded();
                patchState(store, { isAuthenticated: true });
              },
              error: (error) => {
                // cookie invalid, redirect to login
                store.handleAuthError(error);
                store.setAuthLoaded();
                store.setLoginLoaded();
                patchState(store, { isAuthenticated: false });

                store.router.navigate(['./login']);
              }
            })
          ))
      )
    );

    return { login, logout, load };
  }),
  withHooks({
    onInit(store) {
      patchState(store, { isAuthenticated: false });

      store.load();
    }
  })
);
