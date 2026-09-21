import { Component, inject } from '@angular/core';
import { GetAllCategoriesRequest } from '@ske/models';
import { CategoryListState } from '../../../services/category-list.store';
import { FilterForm } from './filter-form';

@Component({
  imports: [
    FilterForm
  ],
  selector: 'ske-category-filter-container',
  styles: ``,
  templateUrl: './filter-container.html',
  host: {
    class: 'px-4'
  }
})
export class FilterContainer {
  public readonly store = inject(CategoryListState);

  public onFilterChange(filter: GetAllCategoriesRequest) {
    this.store.load(filter);
  }
}
