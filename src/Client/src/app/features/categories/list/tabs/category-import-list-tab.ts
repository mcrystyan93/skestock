import { Component, inject, output } from '@angular/core';
import { ErrorAlert } from '@ske/shared/errors';
import { CategoryImportBatchListItemDto } from '@ske/models';
import { CategoryImportState } from '@ske/shared/categories';
import { CategoryImportFilterContainer } from '../filter/import/category-import-filter-container';
import { TableContainer } from '../tables/import/table-container';

@Component({
  imports: [
    CategoryImportFilterContainer,
    ErrorAlert,
    TableContainer
  ],
  selector: 'ske-category-import-list-tab',
  templateUrl: './category-import-list-tab.html',
  host: {
    class: 'flex grow flex-col gap-2 absolute inset-0'
  }
})
export class CategoryImportListTab {
  public readonly store = inject(CategoryImportState);
  public readonly review = output<CategoryImportBatchListItemDto>();
}
