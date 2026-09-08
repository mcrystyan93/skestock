import { inject } from '@angular/core';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { mapResponse } from '@ngrx/operators';
import { patchState, signalStore, withMethods, withProps, withState } from '@ngrx/signals';
import { concatMap, pipe, tap } from 'rxjs';
import { CategoryImportDto, CreateCategoryImportRequest } from '@ske/models';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { CategoryImportsHttp } from './category-imports.http';

type CategoryImportStateModel = {
  categoryImport: CategoryImportDto | null;
};

const initialState: CategoryImportStateModel = {
  categoryImport: null
};

export const CategoryImportState = signalStore(
  withState(initialState),
  withLoadingFeature('categoryImport'),
  withProblemDetailsFeature('categoryImport'),
  withProps(() => ({
    categoryImportsHttp: inject(CategoryImportsHttp)
  })),
  withMethods((store) => {
    const createImport = rxMethod<CreateCategoryImportRequest>(
      pipe(
        tap(() => {
          store.clearCategoryImportErrors();
          store.setCategoryImportLoading();
          patchState(store, { categoryImport: null });
        }),
        concatMap((request) => store.categoryImportsHttp.create(request)),
        mapResponse({
          next: (categoryImport) => {
            patchState(store, { categoryImport });
            store.setCategoryImportLoaded();
          },
          error: (error) => {
            store.handleCategoryImportError(error);
            store.setCategoryImportLoaded();
          }
        })
      )
    );

    return { createImport };
  })
);
