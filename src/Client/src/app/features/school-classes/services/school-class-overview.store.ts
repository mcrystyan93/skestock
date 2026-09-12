import { patchState, signalStore, withMethods, withProps, withState } from '@ngrx/signals';
import { SchoolClassDto } from '@ske/models';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { inject } from '@angular/core';
import { SchoolClassesHttp } from '@ske/shared/school-classes';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap, tap } from 'rxjs';
import { mapResponse } from '@ngrx/operators';
import { withSchoolClassSummaryFeature } from './school-class-summary.feature';
import { withGoodReceiptsFeature } from '@ske/shared/goods-receipts';
import { withQueryParamsSync } from '@ske/routes';

type SchoolClassOverviewState = {
  schoolClass: Partial<SchoolClassDto>;
  selectedTabIndex: number;
  receiptIdQueryParam: string | null;
};
const initialState: SchoolClassOverviewState = {
  schoolClass: {},
  selectedTabIndex: 0,
  receiptIdQueryParam: null
};

export const SchoolClassOverviewStore = signalStore(
  withState(initialState),
  withLoadingFeature('schoolClass'),
  withProblemDetailsFeature('schoolClass'),
  withSchoolClassSummaryFeature(),
  withGoodReceiptsFeature(),
  withQueryParamsSync({
    key: 'tab',
    getValue: (store) => () => store.selectedTabIndex(),
    setValue: (store, value) => patchState(store, { selectedTabIndex: value }),
    parse: (raw) => {
      const parsed = parseInt(raw ?? '0', 10);
      return isNaN(parsed) ? 0 : parsed;
    },
    serialize: (value) => value.toString()
  }),
  withQueryParamsSync({
    key: 'receiptId',
    getValue: (store) => () => store.receiptIdQueryParam(),
    setValue: (store, value) => patchState(store, { receiptIdQueryParam: value }),
    parse: (raw) => raw ?? null,
    serialize: (value) => value ?? ''
  }),
  withProps(() => ({
    schoolClassHttp: inject(SchoolClassesHttp)
  })),
  withMethods((store) => {
    const load = rxMethod<string>(
      pipe(
        tap(() => {
          store.clearSchoolClassErrors();
          store.setSchoolClassLoading();
        }),
        switchMap((id) =>
          store.schoolClassHttp.getById(id).pipe(
            mapResponse({
              next: (result) => {
                patchState(store, {
                  schoolClass: result
                });

                store.setSchoolClassLoaded();

                store.loadSummary(result.id);
              },
              error: (error) => {
                store.handleSchoolClassError(error);
                store.setSchoolClassLoaded();
              }
            })
          )
        )
      )
    );

    const selectTab = (tabIndex: number) => patchState(store, { selectedTabIndex: tabIndex });

    return { load, selectTab };
  })
);
