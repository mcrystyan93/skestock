import { Component, input, output } from '@angular/core';
import { ErrorAlert } from '@ske/shared/errors';
import { FilterForm } from '../../list/filter/regular/filter-form';
import { Table } from '../../list/tables/regular/large/table';
import { ItemListSmall } from '../../list/tables/regular/small/item-list-small';
import { GetAllItemsRequest, ItemDto, ProblemDetails, ValidationProblemDetails } from '@ske/models';
import { LayoutBreakpoint } from '@ske/shared/directives';

@Component({
  imports: [ErrorAlert, FilterForm, Table, LayoutBreakpoint, ItemListSmall],
  selector: 'ske-item-list-tab',
  template: `
    <ske-item-filter-form class="px-4"
                          [loading]="loading()"
                          [filter]="filter()"
                          [selectedCount]="selectedCount()"
                          (onFilterChange)="onFilterChange.emit($event)"
                          (onMerge)="onMerge.emit()" />

    <ske-error-display [problemDetail]="problemDetail()" [validationErrors]="validationErrors()" />

    <div class="grow relative">
      <ng-container *skeLayoutBreakpoint="'lg'; else smallScreenListTemplate">
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
          (onImageUploaded)="onImageUploaded.emit($event)"
          [togglingItemId]="togglingItemId()"
          [selectedIds]="selectedIds()"
          (onToggleSelected)="onToggleSelected.emit($event)"
          (onToggleAllSelected)="onToggleAllSelected.emit($event)"
        />
      </ng-container>
      <ng-template #smallScreenListTemplate>
        <ske-item-list-small
          [items]="items()"
          [filter]="filter()"
          [loading]="loading()"
          [hasNextPage]="hasNextPage()"
          [isLoadingMore]="isLoadingMore()"
          [selectedIds]="selectedIds()"
          (onToggleSelected)="onToggleSelected.emit($event)"
          (onFilterChange)="onFilterChange.emit($event)"
          (onLoadMore)="onLoadMore.emit()"
          (onEdit)="onEdit.emit($event)"
          (onImageUploaded)="onImageUploaded.emit($event)"
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
  public readonly selectedIds = input.required<ReadonlySet<string>>();
  public readonly selectedCount = input.required<number>();

  public readonly onFilterChange = output<GetAllItemsRequest>();
  public readonly onLoadMore = output<void>();
  public readonly onMerge = output<void>();
  public readonly onToggleSelected = output<{ item: ItemDto; checked: boolean }>();
  public readonly onToggleAllSelected = output<boolean>();
  public readonly onEdit = output<ItemDto>();
  public readonly onToggleActive = output<ItemDto>();
  public readonly onImageUploaded = output<{ item: ItemDto; fileId: string }>();
}
