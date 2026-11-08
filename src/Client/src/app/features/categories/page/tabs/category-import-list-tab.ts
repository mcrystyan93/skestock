import { Component, input, output } from '@angular/core';
import { ErrorAlert } from '@ske/shared/errors';
import { LayoutBreakpoint } from '@ske/shared/directives';
import {
  CategoryImportBatchFileDto,
  CategoryImportBatchListItemDto,
  GetAllCategoryImportBatchesRequest,
  ProblemDetails,
  ValidationProblemDetails
} from '@ske/models';
import { FilterForm } from '../../import/filter/category-import-filter-form';
import { Table } from '../../import/tables/large/table';
import { CategoryImportListSmall } from '../../import/tables/small/category-import-list-small';

@Component({
  imports: [
    ErrorAlert,
    FilterForm,
    Table,
    CategoryImportListSmall,
    LayoutBreakpoint
  ],
  selector: 'ske-category-import-list-tab',
  templateUrl: './category-import-list-tab.html',
  host: {
    class: 'flex grow flex-col gap-2 absolute inset-0'
  }
})
export class CategoryImportListTab {
  public readonly items = input.required<CategoryImportBatchListItemDto[]>();
  public readonly filter = input.required<GetAllCategoryImportBatchesRequest>();
  public readonly loading = input.required<boolean>();
  public readonly hasNextPage = input.required<boolean>();
  public readonly isLoadingMore = input.required<boolean>();
  public readonly problemDetail = input<ProblemDetails | null>();
  public readonly validationErrors = input<ValidationProblemDetails | null>();

  public readonly onFilterChange = output<GetAllCategoryImportBatchesRequest>();
  public readonly onLoadMore = output<void>();
  public readonly downloadFile = output<CategoryImportBatchFileDto>();
  public readonly review = output<CategoryImportBatchListItemDto>();
}
