import { Component, inject } from '@angular/core';
import { GetAllCategoryImportBatchesRequest } from '@ske/models';
import { CategoryImportState } from '@ske/shared/categories';
import { FilterForm } from './category-import-filter-form';

@Component({
  imports: [
    FilterForm
  ],
  selector: 'ske-category-import-filter-container',
  templateUrl: './category-import-filter-container.html',
  host: {
    class: 'px-4'
  }
})
export class CategoryImportFilterContainer {
  public readonly store = inject(CategoryImportState);

  public onFilterChange(filter: GetAllCategoryImportBatchesRequest) {
    this.store.load(filter);
  }
}
