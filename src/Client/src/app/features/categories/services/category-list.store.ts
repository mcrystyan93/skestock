import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { withCategoryCollection } from '@ske/shared/categories';
import { CategoryDto } from '@ske/models';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, tap } from 'rxjs';

type CategoryListState = {
  deletingCategoryId: number | null;
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
  })
);
