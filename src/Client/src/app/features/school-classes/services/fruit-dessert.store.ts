import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { mapResponse } from '@ngrx/operators';
import { concatMap, exhaustMap, pipe, switchMap, tap } from 'rxjs';
import { FruitDessertMessageType, FruitDessertPlanEntryDto } from '@ske/models';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { withLoadingFeature } from '@ske/shared/loader';
import { FruitDessertPlansHttp } from './fruit-dessert-plans.http';

/** The tab opened synchronously on click (plan §1); only what the store needs from a `Window`. */
export type BlankTab = { location: { replace(url: string): void }; close(): void };

type FruitDessertState = {
  classId: string | null;
  entries: FruitDessertPlanEntryDto[];
  /** Set only when the popup was blocked, so the row can offer a plain link instead. */
  lastDispatchUrl: { entryId: string; url: string } | null;
  sending: boolean;
};

export const FruitDessertStore = signalStore(
  withState<FruitDessertState>({ classId: null, entries: [], lastDispatchUrl: null, sending: false }),
  withLoadingFeature('entries'),
  withProblemDetailsFeature('entries'),
  withProblemDetailsFeature('action'),
  withProps(() => ({ _http: inject(FruitDessertPlansHttp) })),
  withMethods((store) => {
    const replaceEntry = (updated: FruitDessertPlanEntryDto) =>
      patchState(store, { entries: store.entries().map(e => (e.id === updated.id ? updated : e)) });

    // A failed save leaves the row showing the unsaved value; a new object makes the row's linkedSignal reset.
    const resetEntry = (entryId: string) =>
      patchState(store, { entries: store.entries().map(e => (e.id === entryId ? { ...e } : e)) });

    const load = rxMethod<string>(
      pipe(
        tap((classId) => {
          patchState(store, { classId });
          store.clearEntriesErrors();
          store.setEntriesLoading();
        }),
        switchMap((classId) =>
          store._http.get(classId).pipe(
            mapResponse({
              next: (entries) => {
                patchState(store, { entries });
                store.setEntriesLoaded();
              },
              error: (error) => {
                store.handleEntriesError(error);
                store.setEntriesLoaded();
              }
            })
          )
        )
      )
    );

    const setCongregation = rxMethod<{ entryId: string; congregationId: string | null }>(
      pipe(
        tap(() => store.clearActionErrors()),
        concatMap(({ entryId, congregationId }) =>
          store._http.setCongregation(store.classId()!, entryId, congregationId).pipe(
            mapResponse({
              next: replaceEntry,
              error: (error) => {
                resetEntry(entryId);
                store.handleActionError(error);
              }
            })
          )
        )
      )
    );

    const setEmailSent = rxMethod<{ entryId: string; emailSent: boolean }>(
      pipe(
        tap(() => store.clearActionErrors()),
        concatMap(({ entryId, emailSent }) =>
          store._http.setEmailSent(store.classId()!, entryId, emailSent).pipe(
            mapResponse({
              next: replaceEntry,
              error: (error) => {
                resetEntry(entryId);
                store.handleActionError(error);
              }
            })
          )
        )
      )
    );

    const sendMessage = rxMethod<{ entryId: string; type: FruitDessertMessageType; win: BlankTab | null }>(
      pipe(
        tap(() => {
          store.clearActionErrors();
          patchState(store, { lastDispatchUrl: null, sending: true });
        }),
        exhaustMap(({ entryId, type, win }) =>
          store._http.sendMessage(store.classId()!, entryId, type).pipe(
            mapResponse({
              next: (dispatch) => {
                const flag = type === FruitDessertMessageType.Reminder ? 'reminderSent' : 'thanksSent';
                patchState(store, {
                  entries: store.entries().map(e => (e.id === entryId ? { ...e, [flag]: true } : e)),
                  lastDispatchUrl: win ? null : { entryId, url: dispatch.url },
                  sending: false
                });
                win?.location.replace(dispatch.url);
              },
              error: (error) => {
                win?.close();
                patchState(store, { sending: false });
                store.handleActionError(error);
              }
            })
          )
        )
      )
    );

    return { load, setCongregation, setEmailSent, sendMessage };
  })
);
