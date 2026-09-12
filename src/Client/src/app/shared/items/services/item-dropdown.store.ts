// noinspection ES6PreferShortImport

import {type ItemDropdownOption, type ItemDropdownValue} from '@ske/models';
import {patchState, signalStore, withComputed, withMethods, withState} from '@ngrx/signals';
import {withItemCollection} from './item-collection.feature';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {EMPTY, filter, pipe, switchMap, tap} from 'rxjs';
import {mapResponse} from '@ngrx/operators';
import {withLoadingFeature} from '@ske/shared/loader';
import {withProblemDetailsFeature} from '@ske/shared/errors';
import {isNil} from 'lodash-es';

type ItemDropdownState = {
  selectedItem: ItemDropdownOption | null;
  selectedItemId: string | null;
  selectedItemUnavailable: boolean;
};

const initialState: ItemDropdownState = {
  selectedItem: null,
  selectedItemId: null,
  selectedItemUnavailable: false
};

export const ItemDropdownStore = signalStore(
  withState(initialState),
  withItemCollection(),
  withLoadingFeature('selectedItem'),
  withProblemDetailsFeature('selectedItem'),
  withComputed((store) => ({
    loading: () => store.selectedItemLoading() || store.itemsLoading(),
  })),
  withMethods((store) => {
    const loadSelectedItem = rxMethod<string | null>(
      pipe(
        tap((id) => {
          store.clearSelectedItemErrors();

          patchState(store, {
            selectedItem: null,
            selectedItemId: id,
            selectedItemUnavailable: false
          });

          if (!isNil(id))
            store.setSelectedItemLoading();
        }),
        filter((id): id is string => !!id),
        switchMap((id) => {

          return store.itemHttp.getByIdCached(id).pipe(
            mapResponse({
              next: (item) => {
                if (store.selectedItemId() !== id)
                  return;

                patchState(store, {
                  selectedItem: item,
                  selectedItemUnavailable: false
                });

                store.setSelectedItemLoaded();
              },
              error: (error) => {
                if (store.selectedItemId() !== id)
                  return;

                patchState(store, {
                  selectedItemUnavailable: true
                });
                store.setSelectedItemLoaded();
                store.handleItemsError(error);
              }
            })
          );
        })
      )
    );

    const resolveSelectedItem = (value: ItemDropdownValue) => {
      const id = value?.id;

      if (!id) {
        loadSelectedItem(null);
        return;
      }

      if (value.name) {
        loadSelectedItem(null);
        return;
      }

      loadSelectedItem(id);
    };

    return {loadSelectedItem, resolveSelectedItem};
  })
);
