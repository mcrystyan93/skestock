import { computed, inject } from '@angular/core';
import { mapResponse } from '@ngrx/operators';
import {
  patchState,
  signalStore,
  withComputed,
  withMethods,
  withProps,
  withState
} from '@ngrx/signals';
import { setAllEntities, withEntities } from '@ngrx/signals/entities';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { ClassAnalyticsHttp } from './class-analytics.http';
import {
  CategoryStockSummaryDto,
  ClassStatus,
  GetAllSchoolClassesRequest,
  LocationDto,
  PAGINATION_PAGE_SIZE,
  SchoolClassDto,
  ClassGoodsReceiptCostsDto, ClassAnalysisFilter, toDateOnlyString
} from '@ske/models';
import { Events, withEventHandlers as withSignalEventHandlers } from '@ngrx/signals/events';
import { realtimeEvents } from '@ske/signalr';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { catchError, map, of, pipe, switchMap, tap } from 'rxjs';
import { withQueryParamsSync } from '@ske/routes';

type AnalyticsState = {
  filter: ClassAnalysisFilter;
  categories: CategoryStockSummaryDto[];
  totalQuantity: number;
  totalItemCount: number;
  totalCategoryCount: number;
  receiptPoints: ClassGoodsReceiptCostsDto['points'];
  receiptCount: number;
  totalAmount: number;
  averageAmount: number;
};

const initialState: AnalyticsState = {
  filter: {
    schoolClass: null,
    location: null,
    startDate: null,
    endDate: null
  },
  categories: [],
  totalQuantity: 0,
  totalItemCount: 0,
  totalCategoryCount: 0,
  receiptPoints: [],
  receiptCount: 0,
  totalAmount: 0,
  averageAmount: 0
};

export const ClassAnalyticsStore = signalStore(
  withState(initialState),
  withState({
    classesLoading: false,
    locationsLoading: false,
    stockLoading: false,
    costsLoading: false
  }),
  withProblemDetailsFeature('stock'),
  withProblemDetailsFeature('costs'),
  withQueryParamsSync({
    key: 'classId',
    getValue: (store) => () => store.filter().schoolClass?.id ?? null,
    setValue: (store, value) => {
      const currentFilter = store.filter();
      patchState(store, {
        filter: {
          ...currentFilter,
          schoolClass: value ? { id: value } : null
        }
      });
    },
    parse: (raw) => raw ?? null,
    serialize: (value) => value ?? ''
  }),
  withQueryParamsSync({
    key: 'locationId',
    getValue: (store) => () => store.filter().location?.id ?? null,
    setValue: (store, value) => {
      const currentFilter = store.filter();
      patchState(store, {
        filter: {
          ...currentFilter,
          location: value ? { id: value } : null
        }
      });
    },
    parse: (raw) => raw ?? null,
    serialize: (value) => value ?? ''
  }),
  withComputed(({ receiptPoints, categories }) => ({
    hasCategories: computed(() => categories().length > 0),
    hasReceiptPoints: computed(() => receiptPoints().length > 0)
  })),
  withProps(() => ({
    analyticsHttp: inject(ClassAnalyticsHttp)
  })),
  withMethods((store) => {
    const loadStock = rxMethod<{ classId: string; locationId: string | null }>(
      pipe(
        tap(() => {
          store.clearStockErrors();
          patchState(store, { stockLoading: true });
        }),
        switchMap(({ classId, locationId }) =>
          store.analyticsHttp.getStockByCategory(classId, locationId).pipe(
            mapResponse({
              next: (result) => {
                patchState(store, {
                  categories: result.categories,
                  totalQuantity: result.totalQuantity,
                  totalItemCount: result.totalItemCount,
                  totalCategoryCount: result.totalCategoryCount,
                  stockLoading: false
                });
              },
              error: (error) => {
                store.handleStockError(error);
                patchState(store, { stockLoading: false });
              }
            })
          )
        )
      )
    );

    const loadCosts = rxMethod<{ classId: string; startDate: string | null; endDate: string | null }>(
      pipe(
        tap(() => {
          store.clearCostsErrors();
          patchState(store, { costsLoading: true });
        }),
        switchMap(({ classId, startDate, endDate }) =>
          store.analyticsHttp.getGoodsReceiptCosts(classId, startDate, endDate).pipe(
            mapResponse({
              next: (result) => patchState(store, {
                receiptPoints: result.points,
                receiptCount: result.receiptCount,
                totalAmount: result.totalAmount,
                averageAmount: result.averageAmount,
                costsLoading: false
              }),
              error: (error) => {
                store.handleCostsError(error);
                patchState(store, { costsLoading: false });
              }
            })
          )
        )
      )
    );

    const setFilter = (filter: Partial<ClassAnalysisFilter>) => {
      const currentFilter = store.filter();
      patchState(store, {
        filter: {
          ...currentFilter,
          ...filter
        }
      });

      refreshSelectedData();
    }

    const refreshSelectedData = () => {
      const classId = store.filter().schoolClass?.id;
      if (!classId) {
        return;
      }
      const locationId = store.filter().location?.id ?? null;

      loadStock({ classId, locationId: locationId });
      loadCosts({
        classId,
        startDate: toDateOnlyString(store.filter()?.startDate ?? null),
        endDate: toDateOnlyString(store.filter()?.endDate ?? null)
      });
    };

    return {
      loadStock,
      loadCosts,
      refreshSelectedData,
      setFilter
    };
  }),
  withSignalEventHandlers((store, events = inject(Events)) => ({
    goodsReceiptAnalyticsChanged: events.on(
      realtimeEvents.goodsReceiptImportCreated,
      realtimeEvents.goodsReceiptImportProcessed,
      realtimeEvents.goodsReceiptImportConfirmed
    ).pipe(tap(() => store.refreshSelectedData()))
  }))
);
