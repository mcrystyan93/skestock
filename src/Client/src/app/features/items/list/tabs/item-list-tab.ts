import { Component, input, output } from '@angular/core';
import { ErrorAlert } from '@ske/shared/errors';
import { FilterContainer } from '../filter/regular/filter-container';
import { Table } from '../tables/regular/large/table';
import { ItemListSmall } from '../tables/regular/small/item-list-small';
import { GetAllItemsRequest, ItemDto, ProblemDetails, ValidationProblemDetails } from '@ske/models';
import { LayoutBreakpoint } from '@ske/shared/directives';

@Component({
  imports: [ErrorAlert, FilterContainer, Table, LayoutBreakpoint, ItemListSmall],
  selector: 'ske-item-list-tab',
  template: `
    <ske-item-filter-container />

    <ske-error-display [problemDetail]="problemDetail()" [validationErrors]="validationErrors()" />

    <div class="grow relative">
      <ng-container *skeLayoutBreakpoint="'xl'; else smallScreenListTemplate">
        <ske-item-table
          [items]="items()"
          [filter]="filter()"
          [loading]="loading()"
          [hasNextPage]="hasNextPage()"
          [isLoadingMore]="isLoadingMore()"
          (onFilterChange)="onFilterChange.emit($event)"
          (onLoadMore)="onLoadMore.emit()"
          (onEdit)="onEdit.emit($event)"
          (onToggleActive)="onToggleActive.emit($event)"
          [togglingItemId]="togglingItemId()"
        />
      </ng-container>
      <ng-template #smallScreenListTemplate>
        <ske-item-list-small
          [items]="items()"
          [filter]="filter()"
          [loading]="loading()"
          [hasNextPage]="hasNextPage()"
          [isLoadingMore]="isLoadingMore()"
          (onFilterChange)="onFilterChange.emit($event)"
          (onLoadMore)="onLoadMore.emit()"
          (onEdit)="onEdit.emit($event)"
        />
      </ng-template>
    </div>
  `,
  host: {
    class: 'flex grow flex-col gap-2 absolute inset-0',
  },
})
export class ItemListTab {
  public readonly problemDetail = input<ProblemDetails | null>();
  public readonly validationErrors = input<ValidationProblemDetails | null>();
  public readonly items = input.required<ItemDto[]>();
  public readonly filter = input.required<GetAllItemsRequest>();
  public readonly loading = input.required<boolean>();
  public readonly hasNextPage = input.required<boolean>();
  public readonly isLoadingMore = input.required<boolean>();
  public readonly togglingItemId = input.required<string | null>();

  public readonly onFilterChange = output<GetAllItemsRequest>();
  public readonly onLoadMore = output<void>();
  public readonly onEdit = output<ItemDto>();
  public readonly onToggleActive = output<ItemDto>();
}
