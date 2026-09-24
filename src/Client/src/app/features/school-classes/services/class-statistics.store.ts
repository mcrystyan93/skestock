import { inject } from '@angular/core';
import { Events, withEventHandlers } from '@ngrx/signals/events';
import { mapResponse } from '@ngrx/operators';
import { patchState, signalStore, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { ClassItemStockEvolutionDto, ClassStockByCategoryChartDto } from '@ske/models';
import { ClassStatisticsHttp } from '@ske/shared/class-statistics';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { withLoadingFeature } from '@ske/shared/loader';
import { realtimeEvents } from '@ske/signalr';
import { EMPTY, pipe, switchMap, tap } from 'rxjs';

type ClassStatisticsState = {
  classId: string | null;
  locationId: string | null;
  active: boolean;
  allLocationsChart: ClassStockByCategoryChartDto | null;
  locationChart: ClassStockByCategoryChartDto | null;
  itemIds: string[];
  itemEvolution: ClassItemStockEvolutionDto | null;
};

const initialState: ClassStatisticsState = {
  classId: null,
  locationId: null,
  active: false,
  allLocationsChart: null,
  locationChart: null,
  itemIds: [],
  itemEvolution: null
};

export const ClassStatisticsStore = signalStore(
  withState(initialState),
  withLoadingFeature('allLocations'),
  withProblemDetailsFeature('allLocations'),
  withLoadingFeature('location'),
  withProblemDetailsFeature('location'),
  withLoadingFeature('itemIds'),
  withProblemDetailsFeature('itemIds'),
  withLoadingFeature('itemEvolution'),
  withProblemDetailsFeature('itemEvolution'),
  withProps(() => ({
    classStatisticsHttp: inject(ClassStatisticsHttp)
  })),
  withMethods((store) => {
    const loadItemIds = rxMethod<string>(
      pipe(
        tap(() => {
          store.clearItemIdsErrors();
          store.setItemIdsLoading();
        }),
        switchMap((classId) =>
          store.classStatisticsHttp.getClassStockItemIds(classId).pipe(
            mapResponse({
              next: (itemIds) => {
                patchState(store, { itemIds });
                store.setItemIdsLoaded();
              },
              error: (error) => {
                patchState(store, { itemIds: [] });
                store.handleItemIdsError(error);
                store.setItemIdsLoaded();
              }
            })
          )
        )
      )
    );

    const loadAllLocations = rxMethod<string>(
      pipe(
        tap((classId) => {
          store.clearAllLocationsErrors();
          store.setAllLocationsLoading();
          const classChanged = store.classId() !== classId;
          patchState(store, { classId, active: true, allLocationsChart: null });
          if (classChanged) {
            patchState(store, { itemIds: [], itemEvolution: null });
          }
          loadItemIds(classId);
        }),
        switchMap((classId) =>
          store.classStatisticsHttp.getStockByCategoryAllLocations(classId).pipe(
            mapResponse({
              next: (chart) => {
                patchState(store, { allLocationsChart: chart });
                store.setAllLocationsLoaded();
              },
              error: (error) => {
                store.handleAllLocationsError(error);
                store.setAllLocationsLoaded();
              }
            })
          )
        )
      )
    );

    const loadItemEvolution = rxMethod<{ classId: string; itemId: string } | null>(
      pipe(
        tap((request) => {
          store.clearItemEvolutionErrors();
          patchState(store, { itemEvolution: null });
          if (request) {
            store.setItemEvolutionLoading();
          } else {
            store.setItemEvolutionLoaded();
          }
        }),
        switchMap((request) => {
          if (!request) {
            return EMPTY;
          }

          return store.classStatisticsHttp
            .getItemStockEvolution(request.classId, request.itemId)
            .pipe(
              mapResponse({
                next: (itemEvolution) => {
                  patchState(store, { itemEvolution });
                  store.setItemEvolutionLoaded();
                },
                error: (error) => {
                  store.handleItemEvolutionError(error);
                  store.setItemEvolutionLoaded();
                }
              })
            );
        })
      )
    );

    const loadLocation = rxMethod<{ classId: string; locationId: string | null }>(
      pipe(
        tap(({ classId, locationId }) => {
          store.clearLocationErrors();
          store.setLocationLoading();
          patchState(store, { classId, locationId, active: true, locationChart: null });
        }),
        switchMap(({classId, locationId}) => {
          if (!locationId) {
            store.setLocationLoaded();
            return EMPTY;
          }

          return store.classStatisticsHttp
            .getStockByCategoryForLocation(classId, locationId)
            .pipe(
              mapResponse({
                next: (chart) => {
                  patchState(store, { locationChart: chart });
                  store.setLocationLoaded();
                },
                error: (error) => {
                  store.handleLocationError(error);
                  store.setLocationLoaded();
                }
              })
            );
        })
      )
    );

    const deactivate = () => patchState(store, { active: false });

    const reload = () => {
      const classId = store.classId();
      if (!store.active() || !classId) {
        return;
      }

      loadAllLocations(classId);
      loadLocation({ classId, locationId: store.locationId() });
    };

    return { loadAllLocations, loadLocation, loadItemEvolution, deactivate, reload };
  }),
  withEventHandlers((store, events = inject(Events)) => ({
    stockChanged: events
      .on(
        realtimeEvents.goodsReceiptImportConfirmed,
        realtimeEvents.stockAdjusted,
        realtimeEvents.stockMoved,
        realtimeEvents.stockBatchCreated
      )
      .pipe(tap(() => store.reload()))
  }))
);
