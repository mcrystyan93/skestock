import { Component, inject, output } from '@angular/core';
import { GetAllSupplyListsRequest, SupplyListListItemDto } from '@ske/models';
import { ErrorAlert } from '@ske/shared/errors';
import { SupplyListListState } from '../../services/supply-list-list.store';
import { SupplyListFilterForm } from '../filter/supply-lists/supply-list-filter-form';
import { SupplyListTable } from '../tables/supply-lists/supply-list-table';
import { SupplyListSmall } from '../tables/supply-lists/small/supply-list-small';
import { LayoutBreakpoint } from '@ske/shared/directives';

@Component({
  imports: [ErrorAlert, SupplyListFilterForm, SupplyListTable, SupplyListSmall, LayoutBreakpoint],
  selector: 'ske-supply-list-tab',
  host: { class: 'flex grow flex-col gap-2 absolute inset-0' },
  template: `
    <div class="px-4">
      <ske-supply-list-filter-form [filter]="store.filter()"
                                   (onFilterChange)="onFilterChange($event)" />
    </div>
    <ske-error-display [problemDetail]="store.supplyListsProblemDetail()"
                       [validationErrors]="store.supplyListsValidationErrors()" />
    <div class="grow relative">
      <ng-container *skeLayoutBreakpoint="'lg'; else smallScreenListTemplate">
        <ske-supply-list-table [items]="store.supplyLists()"
                               [filter]="store.filter()"
                               [loading]="store.supplyListsLoading()"
                               [hasNextPage]="store.hasNextPage()"
                               [isLoadingMore]="store.isLoadingMore()"
                               [togglingId]="store.togglingId()"
                               (onFilterChange)="onFilterChange($event)"
                               (onLoadMore)="store.loadMore()"
                               (open)="open.emit($event)"
                               (toggleActive)="toggleActive.emit($event)" />
      </ng-container>
      <ng-template #smallScreenListTemplate>
        <ske-supply-list-small [items]="store.supplyLists()"
                               [filter]="store.filter()"
                               [loading]="store.supplyListsLoading()"
                               [hasNextPage]="store.hasNextPage()"
                               [isLoadingMore]="store.isLoadingMore()"
                               [togglingId]="store.togglingId()"
                               (onFilterChange)="onFilterChange($event)"
                               (onLoadMore)="store.loadMore()"
                               (open)="open.emit($event)"
                               (toggleActive)="toggleActive.emit($event)" />
      </ng-template>
    </div>
  `
})
export class SupplyListTab {
  public readonly store = inject(SupplyListListState);
  public readonly open = output<SupplyListListItemDto>();
  public readonly toggleActive = output<SupplyListListItemDto>();

  public onFilterChange(filter: GetAllSupplyListsRequest) {
    this.store.load(filter);
  }
}
