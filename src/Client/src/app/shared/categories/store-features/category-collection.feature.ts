import {
  CategoryDto,
  GetAllCategoriesRequest,
  PaginatedResponse,
  PAGINATION_PAGE_SIZE,
  PaginatedResponseData, buildCategoryListFilter
} from '@ske/models';
import { patchState, signalStoreFeature, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { inject } from '@angular/core';
import { CategoriesHttp } from '@ske/shared/categories';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, filter, map, pipe, switchMap, tap } from 'rxjs';
import { mapResponse } from '@ngrx/operators';


type CategoryCollectionState = {
  categories: CategoryDto[];
  paginationData: PaginatedResponseData | null;
  filter: GetAllCategoriesRequest;
  isLoadingMore: boolean;
};

const initialState: CategoryCollectionState = {
  categories: [],
  paginationData: null,
  filter: {
    sort: [],
    filters: [],
    cursor: null,
    pageSize: PAGINATION_PAGE_SIZE,
    searchTerm: null
  },
  isLoadingMore: false
};

export function withCategoryCollection() {
  return signalStoreFeature(
    withState(initialState),
    withLoadingFeature('categories'),
    withProblemDetailsFeature('categories'),
    withComputed((store) => ({
      hasNextPage: () => store.paginationData()?.hasNextPage ?? false,
      nextCursor: () => store.paginationData()?.nextCursor ?? null
    })),
    withProps(() => ({
      categoryHttp: inject(CategoriesHttp)
    })),
    withMethods((store) => {
      const load = rxMethod<GetAllCategoriesRequest>(
        pipe(
          map(data => buildCategoryListFilter(store.filter(), data)),
          tap((filter) => {
            store.clearCategoriesErrors();
            store.categoriesLoading();

            patchState(store, { filter, isLoadingMore: false });
          }),
          switchMap(filter =>
            store.categoryHttp.getAll(filter)
              .pipe(
                mapResponse({
                  next: (result) => {
                    patchState(store, {
                      categories: result.data,
                      paginationData: result,
                      filter: { ...filter, cursor: null, sort: result.sort }
                    });
                    store.setCategoriesLoaded();
                  },
                  error: (error) => {
                    store.handleCategoriesError(error);
                    store.setCategoriesLoaded();
                  }
                })
              )
          )
        )
      );

      const loadMore = rxMethod<void>(
        pipe(
          filter(() => store.hasNextPage() && !store.isLoadingMore()),
          tap(() => {
            store.clearCategoriesErrors();
            patchState(store, { isLoadingMore: true });
          }),
          switchMap(() => {
            const filter = store.filter();
            const nextCursor = store.nextCursor();

            if (!nextCursor) {
              patchState(store, { isLoadingMore: false });
              return EMPTY;
            }

            return store.categoryHttp.getAll({ ...filter, cursor: nextCursor })
              .pipe(
                mapResponse({
                  next: (result) => {
                    patchState(store, {
                      categories: [...store.categories(), ...result.data],
                      paginationData: result,
                      filter: { ...filter, cursor: null, sort: result.sort },
                      isLoadingMore: false
                    });
                    store.setCategoriesLoaded();
                  },
                  error: (error) => {
                    store.handleCategoriesError(error);
                    patchState(store, { isLoadingMore: false });
                  }
                })
              );

          })
        )
      );

      return { load, loadMore };
    })
  );
}
