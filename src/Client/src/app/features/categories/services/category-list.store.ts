import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { Events, withEventHandlers } from '@ngrx/signals/events';
import { withCategoryCollection } from '@ske/shared/categories';
import { CategoryDto } from '@ske/models';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { realtimeEvents } from '@ske/signalr';
import { pipe, tap } from 'rxjs';

type CategoryListState = {
  deletingCategoryId: string | null;
};
const initialState: CategoryListState = {
  deletingCategoryId: null
};

export const CategoryListState = signalStore(
  withState(initialState),
  withCategoryCollection(),
  withMethods((store) => {
    const reload = () => {
      store.load(store.filter());
    };

    const deleteCategory = rxMethod<CategoryDto>(
      pipe(
        tap(category => {
          store.clearCategoriesErrors();
          patchState(store, { deletingCategoryId: category.id });
        })
      )
    );

    return { reload, deleteCategory };
  }),
  withEventHandlers((store, events = inject(Events)) => ({
    categoryImportConfirmed: events.on(realtimeEvents.categoryImportConfirmed).pipe(
      tap(() => store.reload())
    )
  }))
);
