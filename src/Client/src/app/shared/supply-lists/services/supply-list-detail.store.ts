import { inject } from '@angular/core';
import { mapResponse } from '@ngrx/operators';
import { patchState, signalStore, type, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { eventGroup, injectDispatch } from '@ngrx/signals/events';
import { SaveSupplyListRequest, SupplyListDto } from '@ske/models';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { withLoadingFeature } from '@ske/shared/loader';
import { isNil } from 'lodash-es';
import { pipe, switchMap, tap } from 'rxjs';
import { SupplyListsHttp } from './supply-lists.http';

type SupplyListDetailState = {
  supplyList: Partial<SupplyListDto>;
};

const initialState: SupplyListDetailState = {
  supplyList: {}
};

export const supplyListApiEvents = eventGroup({
  source: 'Supply List API',
  events: {
    saveSuccess: type<{ operationId: string }>(),
    saveFailure: type<{ operationId: string }>()
  }
});

export const SupplyListDetailState = signalStore(
  withState(initialState),
  withLoadingFeature('supplyList'),
  withProblemDetailsFeature('supplyList'),
  withProps(() => ({
    supplyListsHttp: inject(SupplyListsHttp),
    dispatcher: injectDispatch(supplyListApiEvents)
  })),
  withMethods((store) => {
    const loadSupplyList = rxMethod<string>(
      pipe(
        tap(() => {
          store.setSupplyListLoading();
          store.clearSupplyListErrors();
        }),
        switchMap((id) =>
          store.supplyListsHttp.getById(id).pipe(
            mapResponse({
              next: (supplyList) => {
                patchState(store, { supplyList });
                store.setSupplyListLoaded();
              },
              error: (error) => {
                store.handleSupplyListError(error);
                store.setSupplyListLoaded();
              }
            })
          )
        )
      )
    );

    const saveSupplyList = rxMethod<SaveSupplyListOperation>(
      pipe(
        tap(() => {
          store.setSupplyListLoading();
          store.clearSupplyListErrors();
        }),
        switchMap(({ request, operationId }) => {
          const id = store.supplyList().id;
          const request$ = isNil(id)
            ? store.supplyListsHttp.create(request)
            : store.supplyListsHttp.update(id, request);

          return request$.pipe(
            mapResponse({
              next: (supplyList) => {
                patchState(store, { supplyList });
                store.setSupplyListLoaded();
                store.dispatcher.saveSuccess({ operationId });
              },
              error: (error) => {
                store.handleSupplyListError(error);
                store.setSupplyListLoaded();
                store.dispatcher.saveFailure({ operationId });
              }
            })
          );
        })
      )
    );

    return { loadSupplyList, saveSupplyList };
  })
);

export type SaveSupplyListOperation = {
  request: SaveSupplyListRequest;
  operationId: string;
};
