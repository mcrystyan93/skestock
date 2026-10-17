import { computed, inject } from '@angular/core';
import { patchState, signalStore, type, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { eventGroup, injectDispatch } from '@ngrx/signals/events';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { mapResponse } from '@ngrx/operators';
import { exhaustMap, pipe, tap } from 'rxjs';
import { ClassStatus, PAGINATION_PAGE_SIZE } from '@ske/models';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { withLoadingFeature } from '@ske/shared/loader';
import { SchoolClassesHttp, withSchoolClassCollection } from '@ske/shared/school-classes';

export const closeSchoolClassEvents = eventGroup({
  source: 'Close School Class',
  events: {
    closed: type<void>()
  }
});

/** Drives the "Închide clasa" modal: lists the open target classes and performs the close. */
export const CloseSchoolClassStore = signalStore(
  withState({ sourceClassId: null as string | null }),
  withSchoolClassCollection(),
  withLoadingFeature('close'),
  withProblemDetailsFeature('close'),
  withProps(() => ({
    _http: inject(SchoolClassesHttp),
    _dispatcher: injectDispatch(closeSchoolClassEvents)
  })),
  withComputed((store) => ({
    // The server already filters; this keeps a closed or source class out even if a filter is ignored.
    targets: computed(() =>
      store.schoolClasses().filter(c => c.status !== ClassStatus.Closed && c.id !== store.sourceClassId()))
  })),
  withMethods((store) => {
    const loadTargets = (sourceClassId: string) => {
      patchState(store, { sourceClassId });
      store.load({
        searchTerm: null,
        cursor: null,
        pageSize: PAGINATION_PAGE_SIZE,
        sort: [{ key: 'name', value: 'ascend' }],
        filters: [
          { field: 'status', operator: 'notEquals', value: 'closed', displayValue: 'closed', fieldType: 'string' },
          { field: 'id', operator: 'notEquals', value: sourceClassId, displayValue: sourceClassId, fieldType: 'string' }
        ]
      });
    };

    const close = rxMethod<{ classId: string; targetClassId: string }>(
      pipe(
        tap(() => {
          store.clearCloseErrors();
          store.setCloseLoading();
        }),
        exhaustMap(({ classId, targetClassId }) =>
          store._http.close(classId, targetClassId).pipe(
            mapResponse({
              next: () => {
                store.setCloseLoaded();
                store._dispatcher.closed();
              },
              error: (error) => {
                store.handleCloseError(error);
                store.setCloseLoaded();
              }
            })
          )
        )
      )
    );

    return { loadTargets, close };
  })
);
