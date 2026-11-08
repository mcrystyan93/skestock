import { Component, input, output } from '@angular/core';
import { GetAllSupplyListsRequest, ProblemDetails, SupplyListListItemDto, ValidationProblemDetails } from '@ske/models';
import { ErrorAlert } from '@ske/shared/errors';
import { LayoutBreakpoint } from '@ske/shared/directives';
import { SupplyListFilterForm } from '../../supply-lists/filter/supply-list-filter-form';
import { SupplyListTable } from '../../supply-lists/tables/supply-list-table';
import { SupplyListSmall } from '../../supply-lists/tables/small/supply-list-small';

@Component({
  imports: [ErrorAlert, SupplyListFilterForm, SupplyListTable, SupplyListSmall, LayoutBreakpoint],
  selector: 'ske-supply-list-tab',
  host: { class: 'flex grow flex-col gap-2 absolute inset-0' },
  template: `
    <div class="px-4">
      <ske-supply-list-filter-form [filter]="filter()"
                                   (onFilterChange)="onFilterChange.emit($event)" />
    </div>
    <ske-error-display [problemDetail]="problemDetail()" [validationErrors]="validationErrors()" />
    <div class="grow relative">
      <ng-container *skeLayoutBreakpoint="'lg'; else smallScreenListTemplate">
        <ske-supply-list-table [items]="items()"
                               [filter]="filter()"
                               [loading]="loading()"
                               [hasNextPage]="hasNextPage()"
                               [isLoadingMore]="isLoadingMore()"
                               [togglingId]="togglingId()"
                               (onFilterChange)="onFilterChange.emit($event)"
                               (onLoadMore)="onLoadMore.emit()"
                               (open)="open.emit($event)"
                               (toggleActive)="toggleActive.emit($event)" />
      </ng-container>
      <ng-template #smallScreenListTemplate>
        <ske-supply-list-small [items]="items()"
                               [filter]="filter()"
                               [loading]="loading()"
                               [hasNextPage]="hasNextPage()"
                               [isLoadingMore]="isLoadingMore()"
                               [togglingId]="togglingId()"
                               (onFilterChange)="onFilterChange.emit($event)"
                               (onLoadMore)="onLoadMore.emit()"
                               (open)="open.emit($event)"
                               (toggleActive)="toggleActive.emit($event)" />
      </ng-template>
    </div>
  `
})
export class SupplyListTab {
  public readonly problemDetail = input<ProblemDetails | null>();
  public readonly validationErrors = input<ValidationProblemDetails | null>();
  public readonly items = input.required<SupplyListListItemDto[]>();
  public readonly filter = input.required<GetAllSupplyListsRequest>();
  public readonly loading = input.required<boolean>();
  public readonly hasNextPage = input.required<boolean>();
  public readonly isLoadingMore = input.required<boolean>();
  public readonly togglingId = input.required<string | null>();

  public readonly onFilterChange = output<GetAllSupplyListsRequest>();
  public readonly onLoadMore = output<void>();
  public readonly open = output<SupplyListListItemDto>();
  public readonly toggleActive = output<SupplyListListItemDto>();
}
