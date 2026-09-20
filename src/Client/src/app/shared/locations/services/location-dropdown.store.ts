import { type LocationDropdownValue, type LocationDto } from '@ske/models';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { withLocationCollection } from './location-collection.feature';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { filter, pipe, switchMap, tap } from 'rxjs';
import { mapResponse } from '@ngrx/operators';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { isNil } from 'lodash-es';

type LocationDropdownState = {
  selectedLocation: LocationDto | null;
  selectedLocationId: string | null;
  selectedLocationUnavailable: boolean;
};

const initialState: LocationDropdownState = {
  selectedLocation: null,
  selectedLocationId: null,
  selectedLocationUnavailable: false
};

export const LocationDropdownStore = signalStore(
  withState(initialState),
  withLocationCollection(),
  withLoadingFeature('selectedLocation'),
  withProblemDetailsFeature('selectedLocation'),
  withComputed((store) => ({
    loading: () => store.selectedLocationLoading() || store.locationsLoading()
  })),
  withMethods((store) => {
    const loadSelectedLocation = rxMethod<string | null>(
      pipe(
        tap((id) => {
          store.clearSelectedLocationErrors();

          patchState(store, {
            selectedLocation: null,
            selectedLocationId: id,
            selectedLocationUnavailable: false
          });

          if (!isNil(id)) {
            store.setSelectedLocationLoading();
          } else {
            store.setSelectedLocationLoaded();
          }
        }),
        filter((id): id is string => !!id),
        switchMap((id) =>
          store.locationHttp.getById(id).pipe(
            mapResponse({
              next: (location) => {
                if (store.selectedLocationId() !== id)
                  return;

                patchState(store, {
                  selectedLocation: location,
                  selectedLocationUnavailable: false
                });

                store.setSelectedLocationLoaded();
              },
              error: (error) => {
                if (store.selectedLocationId() !== id)
                  return;

                patchState(store, {
                  selectedLocationUnavailable: true
                });
                store.setSelectedLocationLoaded();
                store.handleSelectedLocationError(error);
              }
            })
          )
        )
      )
    );

    const resolveSelectedLocation = (value: LocationDropdownValue) => {
      const id = value?.id;

      if (!id) {
        loadSelectedLocation(null);
        return;
      }

      if (value.name) {
        loadSelectedLocation(null);
        return;
      }

      loadSelectedLocation(id);
    };

    return { loadSelectedLocation, resolveSelectedLocation };
  })
);
