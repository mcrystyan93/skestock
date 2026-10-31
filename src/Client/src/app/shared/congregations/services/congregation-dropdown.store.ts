import { type CongregationDropdownValue, type CongregationDto } from '@ske/models';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { withCongregationCollection } from './congregation-collection.feature';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { filter, pipe, switchMap, tap } from 'rxjs';
import { mapResponse } from '@ngrx/operators';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { isNil } from 'lodash-es';

type CongregationDropdownState = {
  selectedCongregation: CongregationDto | null;
  selectedCongregationId: string | null;
  selectedCongregationUnavailable: boolean;
};

const initialState: CongregationDropdownState = {
  selectedCongregation: null,
  selectedCongregationId: null,
  selectedCongregationUnavailable: false,
};

export const CongregationDropdownStore = signalStore(
  withState(initialState),
  withCongregationCollection(),
  withLoadingFeature('selectedCongregation'),
  withProblemDetailsFeature('selectedCongregation'),
  withComputed((store) => ({
    loading: () => store.selectedCongregationLoading() || store.congregationsLoading(),
  })),
  withMethods((store) => {
    const loadSelectedCongregation = rxMethod<string | null>(
      pipe(
        tap((id) => {
          store.clearSelectedCongregationErrors();

          patchState(store, {
            selectedCongregation: null,
            selectedCongregationId: id,
            selectedCongregationUnavailable: false,
          });

          if (!isNil(id)) {
            store.setSelectedCongregationLoading();
          } else {
            store.setSelectedCongregationLoaded();
          }
        }),
        filter((id): id is string => !!id),
        switchMap((id) =>
          store.congregationHttp.getById(id).pipe(
            mapResponse({
              next: (congregation) => {
                if (store.selectedCongregationId() !== id) return;

                patchState(store, {
                  selectedCongregation: congregation,
                  selectedCongregationUnavailable: false,
                });

                store.setSelectedCongregationLoaded();
              },
              error: (error) => {
                if (store.selectedCongregationId() !== id) return;

                patchState(store, {
                  selectedCongregationUnavailable: true,
                });
                store.setSelectedCongregationLoaded();
                store.handleSelectedCongregationError(error);
              },
            }),
          ),
        ),
      ),
    );

    const resolveSelectedCongregation = (value: CongregationDropdownValue) => {
      const id = value?.id;

      if (!id) {
        loadSelectedCongregation(null);
        return;
      }

      if (value.name) {
        loadSelectedCongregation(null);
        return;
      }

      loadSelectedCongregation(id);
    };

    return { loadSelectedCongregation, resolveSelectedCongregation };
  }),
);
