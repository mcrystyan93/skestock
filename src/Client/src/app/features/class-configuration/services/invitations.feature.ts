import { inject } from '@angular/core';
import { mapResponse } from '@ngrx/operators';
import { patchState, signalStoreFeature, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap, tap } from 'rxjs';
import { InvitationCountDto, SaveInvitationCountRequest } from '@ske/models';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { ConfigurationHttp } from './configuration.http';

export function withInvitations() {
  return signalStoreFeature(
    withState({ invitationCount: 0 }),
    withLoadingFeature('invitations'),
    withProblemDetailsFeature('invitations'),
    withProps(() => ({
      invitationsHttp: inject(ConfigurationHttp),
    })),
    withMethods((store) => {
      const beginRequest = () => {
        store.clearInvitationsErrors();
        store.setInvitationsLoading();
      };
      const applyInvitations = (result: InvitationCountDto) => {
        patchState(store, { invitationCount: result.invitationCount });
        store.setInvitationsLoaded();
      };
      const handleError = (error: unknown) => {
        store.handleInvitationsError(error);
        store.setInvitationsLoaded();
      };
      const loadInvitations = rxMethod<void>(
        pipe(
          tap(beginRequest),
          switchMap(() =>
            store.invitationsHttp
              .getInvitationCount()
              .pipe(mapResponse({ next: applyInvitations, error: handleError })),
          ),
        ),
      );
      const saveInvitations = rxMethod<SaveInvitationCountRequest>(
        pipe(
          tap(beginRequest),
          switchMap((request) =>
            store.invitationsHttp.saveInvitationCount(request).pipe(
              switchMap(() => store.invitationsHttp.getInvitationCount()),
              mapResponse({ next: applyInvitations, error: handleError }),
            ),
          ),
        ),
      );
      return { loadInvitations, saveInvitations };
    }),
  );
}
