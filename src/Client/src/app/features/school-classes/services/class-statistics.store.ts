import { inject } from '@angular/core';
import { Events, withEventHandlers } from '@ngrx/signals/events';
import { mapResponse } from '@ngrx/operators';
import {
  patchState,
  signalStore,
  signalStoreFeature,
  withMethods,
  withProps,
  withState
} from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import {
  ClassDailyConsumptionDto,
  ClassDailyConsumptionFilter,
  ClassItemStockEvolutionDto,
  ClassStockByCategoryChartDto,
  TopPurchasesDto,
  TopPurchasesFilter
} from '@ske/models';
import { ClassStatisticsHttp } from '@ske/shared/class-statistics';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { withLoadingFeature } from '@ske/shared/loader';
import { realtimeEvents } from '@ske/signalr';
import { EMPTY, pipe, switchMap, tap } from 'rxjs';

/** A location the user drilled into from the all-locations chart. */
export type DrilledLocation = { id: string; name: string };

type ClassStatisticsState = {
  classId: string | null;
  /** When set, the stock card shows the per-item chart of this location instead of all locations. */
  drilledLocation: DrilledLocation | null;
  active: boolean;
  allLocationsChart: ClassStockByCategoryChartDto | null;
  locationChart: ClassStockByCategoryChartDto | null;
  itemIds: string[];
  itemEvolution: ClassItemStockEvolutionDto | null;
  dailyConsumptionFilter: ClassDailyConsumptionFilter | null;
  dailyConsumption: ClassDailyConsumptionDto | null;
  topPurchasesFilter: TopPurchasesFilter | null;
  topPurchases: TopPurchasesDto | null;
};

const initialState: ClassStatisticsState = {
  classId: null,
  drilledLocation: null,
  active: false,
  allLocationsChart: null,
  locationChart: null,
  itemIds: [],
  itemEvolution: null,
  dailyConsumptionFilter: null,
  dailyConsumption: null,
  topPurchasesFilter: null,
  topPurchases: null
};

// Grouped because signalStore() only types a limited number of features.
const withChartRequestStatus = () =>
  signalStoreFeature(
    withLoadingFeature('allLocations'),
    withProblemDetailsFeature('allLocations'),
    withLoadingFeature('location'),
    withProblemDetailsFeature('location'),
    withLoadingFeature('itemIds'),
    withProblemDetailsFeature('itemIds'),
    withLoadingFeature('itemEvolution'),
    withProblemDetailsFeature('itemEvolution')
  );

const withAnalyticsRequestStatus = () =>
  signalStoreFeature(
    withLoadingFeature('dailyConsumption'),
    withProblemDetailsFeature('dailyConsumption'),
    withLoadingFeature('topPurchases'),
    withProblemDetailsFeature('topPurchases')
  );

export const ClassStatisticsStore = signalStore(
  withState(initialState),
  withChartRequestStatus(),
  withAnalyticsRequestStatus(),
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
            // A drilled location belongs to the previous class, so return to the overview.
            patchState(store, { itemIds: [], itemEvolution: null, drilledLocation: null, locationChart: null });
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

    const loadLocationChart = rxMethod<{ classId: string; locationId: string }>(
      pipe(
        tap(() => {
          store.clearLocationErrors();
          store.setLocationLoading();
        }),
        switchMap(({ classId, locationId }) =>
          store.classStatisticsHttp.getLocationStockByItem(classId, locationId).pipe(
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
          )
        )
      )
    );

    /** Drills the stock card into one location of the current class. */
    const openLocation = (location: DrilledLocation) => {
      const classId = store.classId();
      if (!classId) {
        return;
      }

      patchState(store, { drilledLocation: location, locationChart: null });
      loadLocationChart({ classId, locationId: location.id });
    };

    /** Returns the stock card to the all-locations chart. */
    const closeLocation = () => {
      store.clearLocationErrors();
      patchState(store, { drilledLocation: null, locationChart: null });
    };

    const loadDailyConsumption = rxMethod<{ classId: string; filter: ClassDailyConsumptionFilter }>(
      pipe(
        tap(({ filter }) => {
          store.clearDailyConsumptionErrors();
          store.setDailyConsumptionLoading();
          patchState(store, { dailyConsumptionFilter: filter });
        }),
        switchMap(({ classId, filter }) =>
          store.classStatisticsHttp.getClassDailyConsumption(classId, filter).pipe(
            mapResponse({
              next: (dailyConsumption) => {
                patchState(store, { dailyConsumption });
                store.setDailyConsumptionLoaded();
              },
              error: (error) => {
                patchState(store, { dailyConsumption: null });
                store.handleDailyConsumptionError(error);
                store.setDailyConsumptionLoaded();
              }
            })
          )
        )
      )
    );

    const loadTopPurchases = rxMethod<TopPurchasesFilter>(
      pipe(
        tap((filter) => {
          store.clearTopPurchasesErrors();
          store.setTopPurchasesLoading();
          patchState(store, { topPurchasesFilter: filter });
        }),
        switchMap((filter) =>
          store.classStatisticsHttp.getTopPurchases(filter).pipe(
            mapResponse({
              next: (topPurchases) => {
                patchState(store, { topPurchases });
                store.setTopPurchasesLoaded();
              },
              error: (error) => {
                patchState(store, { topPurchases: null });
                store.handleTopPurchasesError(error);
                store.setTopPurchasesLoaded();
              }
            })
          )
        )
      )
    );

    const deactivate = () => patchState(store, { active: false });

    const reload = () => {
      const classId = store.classId();
      if (!store.active() || !classId) {
        return;
      }

      loadAllLocations(classId);
      const drilledLocation = store.drilledLocation();
      if (drilledLocation) {
        loadLocationChart({ classId, locationId: drilledLocation.id });
      }
      const dailyConsumptionFilter = store.dailyConsumptionFilter();
      if (dailyConsumptionFilter) {
        loadDailyConsumption({ classId, filter: dailyConsumptionFilter });
      }
      const topPurchasesFilter = store.topPurchasesFilter();
      if (topPurchasesFilter) {
        loadTopPurchases(topPurchasesFilter);
      }
    };

    return {
      loadAllLocations,
      openLocation,
      closeLocation,
      loadItemEvolution,
      loadDailyConsumption,
      loadTopPurchases,
      deactivate,
      reload
    };
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
