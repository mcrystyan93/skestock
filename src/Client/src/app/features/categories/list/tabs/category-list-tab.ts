import { Component, input, output } from '@angular/core';
import { ErrorAlert } from '@ske/shared/errors';
import { FilterContainer } from '../filter/filter-container';
import { Table } from '../tables/regular/table';
import { CategoryDto, GetAllCategoriesRequest, ProblemDetails, ValidationProblemDetails } from '@ske/models';

@Component({
  imports: [
    ErrorAlert,
    FilterContainer,
    Table
  ],
  selector: 'ske-category-list-tab',
  host: {
    class: 'flex grow flex-col gap-2 absolute inset-0'
  },
  template: `
    <ske-category-filter-container />

    <ske-error-alert [problemDetail]="problemDetail()"
                     [validationErrors]="validationErrors()" />

    <div class="grow relative">
      <ske-category-table [items]="categories()"
                          [filter]="filter()"
                          [loading]="loading()"
                          [hasNextPage]="hasNextPage()"
                          [isLoadingMore]="isLoadingMore()"
                          (onFilterChange)="onFilterChange.emit($event)"
                          (onLoadMore)="onLoadMore.emit()"
                          (onEdit)="onEdit.emit($event)"
                          (onDelete)="onDelete.emit($event)"
                          [deletingCategoryId]="deletingCategoryId()" />
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
  public readonly deletingCategoryId = input.required<string | null>();

  public readonly onFilterChange = output<GetAllCategoriesRequest>();
  public readonly onLoadMore = output<void>();
  public readonly onEdit = output<CategoryDto>();
  public readonly onDelete = output<CategoryDto>();

}
