import { CongregationDto, CreateCongregationRequest, UpdateCongregationRequest } from '@ske/models';
import { patchState, signalStore, type, withMethods, withProps, withState } from '@ngrx/signals';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { inject } from '@angular/core';
// noinspection ES6PreferShortImport
import { CongregationsHttp } from './congregations.http';
import { isNil } from 'lodash-es';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, of, pipe, switchMap, tap } from 'rxjs';
import { mapResponse } from '@ngrx/operators';
import { eventGroup, injectDispatch } from '@ngrx/signals/events';

type CongregationDetailState = { congregation: Partial<CongregationDto> };
const initialState: CongregationDetailState = { congregation: {} };
export const NEW_CONGREGATION_ROUTE_ID = 'new';
export const congregationApiEvents = eventGroup({
  source: 'Congregation API',
  events: {
    saveSuccess: type<void>()
  }
});
export const CongregationDetailState = signalStore(
  withState(initialState),
  withLoadingFeature('congregation'),
  withProblemDetailsFeature('congregation'),
  withProps(() => ({
    congregationHttp: inject(CongregationsHttp),
    dispatcher: injectDispatch(congregationApiEvents)
  })),
  withMethods((store) => {
    const getCurrentCongregationId = () => {
      const id = store.congregation().id;

      if (isNil(id)) {
        store.handleCongregationError({ title: 'Congregation ID missing', status: 400 });
        store.setCongregationLoaded();
        return null;
      }

      return id;
    };

    const loadCongregation = rxMethod<LoadCongregationRequest>(
      pipe(
        tap(() => {
          store.setCongregationLoading();
          store.clearCongregationErrors();
        }),
        switchMap(({ id, prefill }) => {
          if (id === NEW_CONGREGATION_ROUTE_ID) {
            patchState(store, {
              congregation: prefill ?? {}
            });
            store.setCongregationLoaded();
            return of(null);
          }

          if (isNil(id) || id === '') {
            store.handleCongregationError({ title: 'Congregation ID missing', status: 400 });
            store.setCongregationLoaded();
            return of(null);
          }

          return store.congregationHttp.getById(id).pipe(
            mapResponse({
              next: (congregation) => {
                patchState(store, { congregation });
                store.setCongregationLoaded();
              },
              error: (error) => {
                store.handleCongregationError(error);
                store.setCongregationLoaded();
              }
            })
          );
        })
      )
    );

    const createCongregation = rxMethod<CreateCongregationRequest>(
      pipe(
        tap(() => {
          store.setCongregationLoading();
          store.clearCongregationErrors();
        }),
        switchMap((request) =>
          store.congregationHttp.create(request).pipe(
            mapResponse({
              next: (congregation) => {
                patchState(store, { congregation });
                store.dispatcher.saveSuccess();
                store.setCongregationLoaded();
              },
              error: (error) => {
                store.handleCongregationError(error);
                store.setCongregationLoaded();
              }
            })
          )
        )
      )
    );

    const updateCongregation = rxMethod<UpdateCongregationRequest>(
      pipe(
        tap(() => {
          store.setCongregationLoading();
          store.clearCongregationErrors();
        }),
        switchMap((request) => {
          const id = getCurrentCongregationId();

          if (isNil(id)) {
            return EMPTY;
          }

          return store.congregationHttp.update(id, request).pipe(
            mapResponse({
              next: (congregation) => {
                patchState(store, { congregation });
                store.dispatcher.saveSuccess();
                store.setCongregationLoaded();
              },
              error: (error) => {
                store.handleCongregationError(error);
                store.setCongregationLoaded();
              }
            })
          );
        })
      )
    );

    const saveCongregation = (request: CreateCongregationRequest | UpdateCongregationRequest) => {
      if (store.congregation().id) {
        updateCongregation(request);
        return;
      }

      createCongregation(request);
    };

    return { loadCongregation, saveCongregation };
  })
);

type LoadCongregationRequest = {
  id: string;
  prefill?: Partial<CongregationDto> | null;
};
