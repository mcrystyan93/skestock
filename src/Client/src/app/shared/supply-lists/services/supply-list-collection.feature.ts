import { inject } from '@angular/core';
import { mapResponse } from '@ngrx/operators';
import { patchState, signalStoreFeature, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import {
  buildSupplyListActiveFilter,
  buildSupplyListFilter,
  GetAllSupplyListsRequest,
  PAGINATION_PAGE_SIZE,
  PaginatedResponseData,
  SupplyListListItemDto
} from '@ske/models';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { withLoadingFeature } from '@ske/shared/loader';
import { NzMessageService } from 'ng-zorro-antd/message';
import { EMPTY, filter, map, pipe, switchMap, tap } from 'rxjs';
import { SupplyListsHttp } from './supply-lists.http';

type SupplyListCollectionState = {
  supplyLists: SupplyListListItemDto[];
  paginationData: PaginatedResponseData | null;
  filter: GetAllSupplyListsRequest;
  isLoadingMore: boolean;
  togglingId: string | null;
};

const initialState: SupplyListCollectionState = {
  supplyLists: [],
  paginationData: null,
  filter: {
    sort: [],
    filters: [buildSupplyListActiveFilter('active')!],
    cursor: null,
    pageSize: PAGINATION_PAGE_SIZE,
    searchTerm: null
  },
  isLoadingMore: false,
  togglingId: null
};

export function withSupplyListCollection() {
  return signalStoreFeature(
    withState(initialState),
    withLoadingFeature('supplyLists'),
    withProblemDetailsFeature('supplyLists'),
    withComputed((store) => ({
      hasNextPage: () => store.paginationData()?.hasNextPage ?? false,
      nextCursor: () => store.paginationData()?.nextCursor ?? null
    })),
    withProps(() => ({
      supplyListsHttp: inject(SupplyListsHttp),
      nzMessageService: inject(NzMessageService)
    })),
    withMethods((store) => {
      const load = rxMethod<GetAllSupplyListsRequest>(
        pipe(
          map((data) => buildSupplyListFilter(store.filter(), data)),
          tap((filter) => {
            store.clearSupplyListsErrors();
            store.setSupplyListsLoading();
            patchState(store, { filter, isLoadingMore: false });
          }),
          switchMap((filter) =>
            store.supplyListsHttp.getAll(filter).pipe(
              mapResponse({
                next: (result) => {
                  patchState(store, {
                    supplyLists: result.data,
                    paginationData: result,
                    filter: { ...filter, cursor: null, sort: result.sort }
                  });
                  store.setSupplyListsLoaded();
                },
                error: (error) => {
                  store.handleSupplyListsError(error);
                  store.setSupplyListsLoaded();
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
            store.clearSupplyListsErrors();
            patchState(store, { isLoadingMore: true });
          }),
          switchMap(() => {
            const filter = store.filter();
            const nextCursor = store.nextCursor();

            if (!nextCursor) {
              patchState(store, { isLoadingMore: false });
              return EMPTY;
            }

            return store.supplyListsHttp.getAll({ ...filter, cursor: nextCursor }).pipe(
              mapResponse({
                next: (result) => {
                  patchState(store, {
                    supplyLists: [...store.supplyLists(), ...result.data],
                    paginationData: result,
                    filter: { ...filter, cursor: null, sort: result.sort },
                    isLoadingMore: false
                  });
                },
                error: (error) => {
                  store.handleSupplyListsError(error);
                  patchState(store, { isLoadingMore: false });
                }
              })
            );
          })
        )
      );

      const toggleActive = rxMethod<SupplyListListItemDto>(
        pipe(
          filter(() => store.togglingId() === null),
          tap((supplyList) => {
            store.clearSupplyListsErrors();
            patchState(store, { togglingId: supplyList.id });
          }),
          switchMap((supplyList) => {
            const request$ = supplyList.isActive
              ? store.supplyListsHttp.disable(supplyList.id)
              : store.supplyListsHttp.enable(supplyList.id);

            return request$.pipe(
              mapResponse({
                next: (updated) => {
                  patchState(store, { togglingId: null });
                  store.nzMessageService.success(updated.isActive
                    ? 'Lista a fost activată.'
                    : 'Lista a fost dezactivată.');
                  load(store.filter());
                },
                error: (error) => {
                  store.handleSupplyListsError(error);
                  patchState(store, { togglingId: null });
                }
              })
            );
          })
        )
      );

      return { load, loadMore, toggleActive };
    })
  );
}
