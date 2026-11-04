import { patchState, signalStore, withHooks, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { mapResponse } from '@ngrx/operators';
import { exhaustMap, pipe, tap } from 'rxjs';
import { withCongregationCollection } from '@ske/shared/congregations';
import { CongregationDto } from '@ske/models';

type CongregationListState = {
  deletingCongregationId: string | null;
};
const initialState: CongregationListState = {
  deletingCongregationId: null
};

export const CongregationListState = signalStore(
  withState(initialState),
  withCongregationCollection(),
  withMethods((store) => {
    const reload = () => {
      store.load(store.filter());
    };

    const deleteCongregation = rxMethod<CongregationDto>(
      pipe(
        tap((congregation) => {
          store.clearCongregationsErrors();
          patchState(store, { deletingCongregationId: congregation.id });
        }),
        exhaustMap((congregation) =>
          store.congregationHttp.delete(congregation.id).pipe(
            mapResponse({
              next: () => {
                patchState(store, { deletingCongregationId: null });
                reload();
              },
              error: (error) => {
                store.handleCongregationsError(error);
                patchState(store, { deletingCongregationId: null });
              }
            })
          )
        )
      )
    );

    return { reload, deleteCongregation };
  }),
  withHooks({
    // The filter form only emits real changes, so the first page is loaded here.
    onInit: (store) => store.load(store.filter())
  })
);
