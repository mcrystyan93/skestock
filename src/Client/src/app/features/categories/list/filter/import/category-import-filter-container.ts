import { Component, inject } from '@angular/core';
import { GetAllCategoryImportBatchesRequest } from '@ske/models';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { CategoryImportState } from '@ske/shared/categories';
import { FilterForm } from './category-import-filter-form';

@Component({
  imports: [
    NzCardComponent,
    FilterForm
  ],
  selector: 'ske-category-import-filter-container',
  templateUrl: './category-import-filter-container.html'
})
export class CategoryImportFilterContainer {
  public readonly store = inject(CategoryImportState);

  public onFilterChange(filter: GetAllCategoryImportBatchesRequest) {
    this.store.load(filter);
  }
}
