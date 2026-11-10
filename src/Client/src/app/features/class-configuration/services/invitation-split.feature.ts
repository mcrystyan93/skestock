import { inject } from '@angular/core';
import { mapResponse } from '@ngrx/operators';
import { patchState, signalStoreFeature, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap, tap } from 'rxjs';
import { InvitationSplitDto, SaveInvitationSplitRequest } from '@ske/models';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { ConfigurationHttp } from './configuration.http';

const EMPTY_INVITATION_SPLIT: InvitationSplitDto = {
  studentInvitationCount: 0,
  instructorInvitationCount: 0,
  openingPrayerInvitationCount: 0,
  experienceInvitationCount: 0,
  changeInvitationCount: 0,
};

export function withInvitationSplit() {
  return signalStoreFeature(
    withState({ invitationSplit: EMPTY_INVITATION_SPLIT }),
    withLoadingFeature('invitationSplit'),
    withProblemDetailsFeature('invitationSplit'),
    withProps(() => ({ invitationSplitHttp: inject(ConfigurationHttp) })),
    withMethods((store) => {
      const beginRequest = () => {
        store.clearInvitationSplitErrors();
        store.setInvitationSplitLoading();
      };
      const applySplit = (invitationSplit: InvitationSplitDto) => {
        patchState(store, { invitationSplit });
        store.setInvitationSplitLoaded();
      };
      const handleError = (error: unknown) => {
        store.handleInvitationSplitError(error);
        store.setInvitationSplitLoaded();
      };
      const loadInvitationSplit = rxMethod<void>(
        pipe(
          tap(beginRequest),
          switchMap(() =>
            store.invitationSplitHttp
              .getInvitationSplit()
              .pipe(mapResponse({ next: applySplit, error: handleError })),
          ),
        ),
      );
      const saveInvitationSplit = rxMethod<SaveInvitationSplitRequest>(
        pipe(
          tap(beginRequest),
          switchMap((request) =>
            store.invitationSplitHttp.saveInvitationSplit(request).pipe(
              switchMap(() => store.invitationSplitHttp.getInvitationSplit()),
              mapResponse({ next: applySplit, error: handleError }),
            ),
          ),
        ),
      );

      return { loadInvitationSplit, saveInvitationSplit };
    }),
  );
}
