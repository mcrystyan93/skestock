import { SchoolClassSummary } from '@ske/models';
import { patchState, signalStoreFeature, withMethods, withState } from '@ngrx/signals';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { inject } from '@angular/core';
import { SchoolClassesHttp } from '@ske/shared/school-classes';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap, tap } from 'rxjs';
import { mapResponse } from '@ngrx/operators';

type SchoolClassSummaryState = {
  summary: Partial<SchoolClassSummary>;
};

const initialState: SchoolClassSummaryState = { summary: {} };

export function withSchoolClassSummaryFeature() {
  return signalStoreFeature(
    withState(initialState),
    withLoadingFeature('summary'),
    withProblemDetailsFeature('summary'),
    withMethods((store) => {
      const schoolClassHttp = inject(SchoolClassesHttp);

      const loadSummary = rxMethod<string>(
        pipe(
          tap(() => {
            store.setSummaryLoading();
            store.clearSummaryErrors();
          }),
          switchMap(id =>
            schoolClassHttp.getSummary(id)
              .pipe(
                mapResponse({
                  next: (result) => {
                    patchState(store, { summary: result });
                    store.setSummaryLoaded();
                  },
                  error: (error) => {
                    store.handleSummaryError(error);
                    store.setSummaryLoaded();
                  }
                })
              )
          )
        )
      );

      return {loadSummary};
    })
  );
}
