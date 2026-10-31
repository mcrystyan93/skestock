import { Component, input, output } from '@angular/core';
import { ErrorAlert } from '@ske/shared/errors';
import { FilterContainer } from '../filter/filter-container';
import { Table } from '../tables/large/table';
import { CongregationDto, GetAllCongregationsRequest, ProblemDetails, ValidationProblemDetails } from '@ske/models';
import { LayoutBreakpoint } from '@ske/shared/directives';
import { CongregationListSmall } from '../tables/small/congregation-list-small';

@Component({
  imports: [
    ErrorAlert,
    FilterContainer,
    Table,
    LayoutBreakpoint,
    CongregationListSmall
  ],
  selector: 'ske-congregation-list-tab',
  host: {
    class: 'flex grow flex-col gap-4 absolute inset-0'
  },
  template: `
    <ske-congregation-filter-container />

    @if (problemDetail() || validationErrors()) {
      <ske-error-display [problemDetail]="problemDetail()"
                         [validationErrors]="validationErrors()" />
    }

    <div class="grow relative">
      <ng-container *skeLayoutBreakpoint="'lg';else:smallScreenListTemplate">
        <ske-congregation-table [items]="congregations()"
                                [filter]="filter()"
                                [loading]="loading()"
                                [hasNextPage]="hasNextPage()"
                                [isLoadingMore]="isLoadingMore()"
                                [deletingCongregationId]="deletingCongregationId()"
                                (onFilterChange)="onFilterChange.emit($event)"
                                (onLoadMore)="onLoadMore.emit()"
                                (onEdit)="onEdit.emit($event)"
                                (onDelete)="onDelete.emit($event)" />
      </ng-container>
      <ng-template #smallScreenListTemplate>
        <ske-congregation-list-small [items]="congregations()"
                                     [filter]="filter()"
                                     [loading]="loading()"
                                     [hasNextPage]="hasNextPage()"
                                     [isLoadingMore]="isLoadingMore()"
                                     [deletingCongregationId]="deletingCongregationId()"
                                     (onLoadMore)="onLoadMore.emit()"
                                     (onEdit)="onEdit.emit($event)"
                                     (onDelete)="onDelete.emit($event)" />
      </ng-template>
    </div>
  `
})
export class CongregationListTab {
  public readonly problemDetail = input<ProblemDetails | null>();
  public readonly validationErrors = input<ValidationProblemDetails | null>();
  public readonly congregations = input.required<CongregationDto[]>();
  public readonly filter = input.required<GetAllCongregationsRequest>();
  public readonly loading = input.required<boolean>();
  public readonly hasNextPage = input.required<boolean>();
  public readonly isLoadingMore = input.required<boolean>();
  public readonly deletingCongregationId = input.required<string | null>();

  public readonly onFilterChange = output<GetAllCongregationsRequest>();
  public readonly onLoadMore = output<void>();
  public readonly onEdit = output<CongregationDto>();
  public readonly onDelete = output<CongregationDto>();
}
