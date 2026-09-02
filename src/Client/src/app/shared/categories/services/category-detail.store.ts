import { CategoryDto, CreateCategoryRequest, UpdateCategoryRequest } from '@ske/models';
import { patchState, signalStore, type, withMethods, withProps, withState } from '@ngrx/signals';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { inject } from '@angular/core';
import { CategoriesHttp } from '@ske/shared/categories';
import { isNil } from 'lodash-es';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, of, pipe, switchMap, tap } from 'rxjs';
import { mapResponse } from '@ngrx/operators';
import { eventGroup, injectDispatch } from '@ngrx/signals/events';

type CategoryDetailState = { category: Partial<CategoryDto> };
const initialState: CategoryDetailState = { category: {} };
export const NEW_CATEGORY_ROUTE_ID = 'new';
export const categoryApiEvents = eventGroup({
  source: 'Category API',
  events: {
    saveSuccess: type<void>()
  }
});
export const CategoryDetailState = signalStore(
  withState(initialState),
  withLoadingFeature('category'),
  withProblemDetailsFeature('category'),
  withProps(() => ({
    categoryHttp: inject(CategoriesHttp),
    dispatcher: injectDispatch(categoryApiEvents)
  })),
  withMethods((store) => {
    const getCurrentCategoryId = () => {
      const id = store.category().id;

      if (isNil(id)) {
        store.handleCategoryError({ title: 'Category ID missing', status: 400 });
        store.setCategoryLoaded();
        return null;
      }

      return id;
    };

    const loadCategory = rxMethod<string>(
      pipe(
        tap(() => {
          store.setCategoryLoading();
          store.clearCategoryErrors();
        }),
        switchMap((id) => {
          if (id === NEW_CATEGORY_ROUTE_ID) {
            patchState(store, {
              category: {}
            });
            store.setCategoryLoaded();
            return of(null);
          }

          if (isNil(id) || id === '') {
            store.handleCategoryError({ title: 'Category ID missing', status: 400 });
            store.setCategoryLoaded();
            return of(null);
          }

          return store.categoryHttp.getById(id).pipe(
            mapResponse({
              next: (category) => {
                patchState(store, { category });
                store.setCategoryLoaded();
              },
              error: (error) => {
                store.handleCategoryError(error);
                store.setCategoryLoaded();
              }
            })
          );
        })
      )
    );

    const createCategory = rxMethod<CreateCategoryRequest>(
      pipe(
        tap(() => {
          store.setCategoryLoading();
          store.clearCategoryErrors();
        }),
        switchMap((request) =>
          store.categoryHttp.create(request).pipe(
            mapResponse({
              next: (category) => {
                patchState(store, { category });
                store.dispatcher.saveSuccess();
                store.setCategoryLoaded();
              },
              error: (error) => {
                store.handleCategoryError(error);
                store.setCategoryLoaded();
              }
            })
          )
        )
      )
    );

    const updateCategory = rxMethod<UpdateCategoryRequest>(
      pipe(
        tap(() => {
          store.setCategoryLoading();
          store.clearCategoryErrors();
        }),
        switchMap((request) => {
          const id = getCurrentCategoryId();

          if (isNil(id)) {
            return EMPTY;
          }

          return store.categoryHttp.update(id, request).pipe(
            mapResponse({
              next: (category) => {
                patchState(store, { category });
                store.dispatcher.saveSuccess();
                store.setCategoryLoaded();
              },
              error: (error) => {
                store.handleCategoryError(error);
                store.setCategoryLoaded();
              }
            })
          );
        })
      )
    );

    const saveCategory = (request: CreateCategoryRequest | UpdateCategoryRequest) => {
      if (store.category().id) {
        updateCategory(request);
        return;
      }

      createCategory(request);
    };

    return { loadCategory, saveCategory };
  })
);
