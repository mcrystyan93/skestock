import { Component, input, output } from '@angular/core';
import { ErrorAlert } from '@ske/shared/errors';
import { FilterForm } from '../../list/filter/regular/filter-form';
import { Table } from '../../list/tables/regular/large/table';
import { CategoryDto, GetAllCategoriesRequest, ProblemDetails, ValidationProblemDetails } from '@ske/models';
import { LayoutBreakpoint } from '@ske/shared/directives';
import { CategoryListSmall } from '../../list/tables/regular/small/category-list-small';

@Component({
  imports: [
    ErrorAlert,
    FilterForm,
    Table,
    LayoutBreakpoint,
    CategoryListSmall
  ],
  selector: 'ske-category-list-tab',
  host: {
    class: 'flex grow flex-col gap-4 absolute inset-0'
  },
  template: `
    <ske-category-filter-form class="px-4"
                              [loading]="loading()"
                              [filter]="filter()"
                              (onFilterChange)="onFilterChange.emit($event)" />

    @if (problemDetail() || validationErrors()) {
      <ske-error-display [problemDetail]="problemDetail()"
                         [validationErrors]="validationErrors()" />
    }

    <div class="grow relative">
      <ng-container *skeLayoutBreakpoint="'lg';else:smallScreenListTemplate">
        <ske-category-table [items]="categories()"
                            [filter]="filter()"
                            [loading]="loading()"
                            [hasNextPage]="hasNextPage()"
                            [isLoadingMore]="isLoadingMore()"
                            (onFilterChange)="onFilterChange.emit($event)"
                            (onLoadMore)="onLoadMore.emit()"
                            (onEdit)="onEdit.emit($event)" />
      </ng-container>
      <ng-template #smallScreenListTemplate>
        <ske-category-list-small [items]="categories()"
                                 [filter]="filter()"
                                 [loading]="loading()"
                                 [hasNextPage]="hasNextPage()"
                                 [isLoadingMore]="isLoadingMore()"
                                 (onLoadMore)="onLoadMore.emit()"
                                 (onEdit)="onEdit.emit($event)" />
      </ng-template>

    </div>
  `
})
export class CategoryListTab {
  public readonly problemDetail = input<ProblemDetails | null>();
  public readonly validationErrors = input<ValidationProblemDetails | null>();
  public readonly categories = input.required<CategoryDto[]>();
  public readonly filter = input.required<GetAllCategoriesRequest>();
  public readonly loading = input.required<boolean>();
  public readonly hasNextPage = input.required<boolean>();
  public readonly isLoadingMore = input.required<boolean>();
  public readonly onFilterChange = output<GetAllCategoriesRequest>();
  public readonly onLoadMore = output<void>();
  public readonly onEdit = output<CategoryDto>();

}
