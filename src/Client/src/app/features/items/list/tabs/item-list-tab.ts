import { Component, input, output } from '@angular/core';
import { ErrorAlert } from '@ske/shared/errors';
import { FilterContainer } from '../filter/filter-container';
import { Table } from '../table/table';
import { GetAllItemsRequest, ItemDto, ProblemDetails, ValidationProblemDetails } from '@ske/models';

@Component({
  imports: [
    ErrorAlert,
    FilterContainer,
    Table
  ],
  selector: 'ske-item-list-tab',
  template: `
    <ske-item-filter-container />

    <ske-error-alert [problemDetail]="problemDetail()"
                     [validationErrors]="validationErrors()" />

    <div class="grow relative">
      <ske-item-table [items]="items()"
                      [filter]="filter()"
                      [loading]="loading()"
                      [hasNextPage]="hasNextPage()"
                      [isLoadingMore]="isLoadingMore()"
                      (onFilterChange)="onFilterChange.emit($event)"
                      (onLoadMore)="onLoadMore.emit()"
                      (onEdit)="onEdit.emit($event)"
                      (onToggleActive)="onToggleActive.emit($event)"
                      [togglingItemId]="togglingItemId()" />
    </div>
  `,
  host: {
    class: 'flex grow flex-col gap-2 absolute inset-0'
  }
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
