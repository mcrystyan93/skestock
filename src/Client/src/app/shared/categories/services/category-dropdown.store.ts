import { type CategoryDropdownValue, type CategoryDto } from '@ske/models';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { withCategoryCollection } from './category-collection.feature';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { filter, pipe, switchMap, tap } from 'rxjs';
import { mapResponse } from '@ngrx/operators';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { isNil } from 'lodash-es';

type CategoryDropdownState = {
  selectedCategory: CategoryDto | null;
  selectedCategoryId: string | null;
  selectedCategoryUnavailable: boolean;
};

const initialState: CategoryDropdownState = {
  selectedCategory: null,
  selectedCategoryId: null,
  selectedCategoryUnavailable: false,
};

export const CategoryDropdownStore = signalStore(
  withState(initialState),
  withCategoryCollection(),
  withLoadingFeature('selectedCategory'),
  withProblemDetailsFeature('selectedCategory'),
  withComputed((store) => ({
    loading: () => store.selectedCategoryLoading() || store.categoriesLoading(),
  })),
  withMethods((store) => {
    const loadSelectedCategory = rxMethod<string | null>(
      pipe(
        tap((id) => {
          store.clearSelectedCategoryErrors();

          patchState(store, {
            selectedCategory: null,
            selectedCategoryId: id,
            selectedCategoryUnavailable: false,
          });

          if (!isNil(id)) {
            store.setSelectedCategoryLoading();
          } else {
            store.setSelectedCategoryLoaded();
          }
        }),
        filter((id): id is string => !!id),
        switchMap((id) =>
          store.categoryHttp.getById(id).pipe(
            mapResponse({
              next: (category) => {
                if (store.selectedCategoryId() !== id) return;

                patchState(store, {
                  selectedCategory: category,
                  selectedCategoryUnavailable: false,
                });

                store.setSelectedCategoryLoaded();
              },
              error: (error) => {
                if (store.selectedCategoryId() !== id) return;

                patchState(store, {
                  selectedCategoryUnavailable: true,
                });
                store.setSelectedCategoryLoaded();
                store.handleSelectedCategoryError(error);
              },
            }),
          ),
        ),
      ),
    );

    const resolveSelectedCategory = (value: CategoryDropdownValue) => {
      const id = value?.id;

      if (!id) {
        loadSelectedCategory(null);
        return;
      }

      if (value.name) {
        loadSelectedCategory(null);
        return;
      }

      loadSelectedCategory(id);
    };

    return { loadSelectedCategory, resolveSelectedCategory };
  }),
);
