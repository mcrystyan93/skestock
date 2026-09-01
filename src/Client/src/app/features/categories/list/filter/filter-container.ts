import { Component, inject, input } from '@angular/core';
import { GetAllCategoriesRequest } from '@ske/models';
import { CategoryListState } from '../../services/category-list.store';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { FilterForm } from './filter-form';

@Component({
  imports: [
    NzCardComponent,
    FilterForm
  ],
  selector: 'ske-category-filter-container',
  styles: ``,
  templateUrl: './filter-container.html'
})
export class FilterContainer {
  public readonly store = inject(CategoryListState);

  public onFilterChange(filter: GetAllCategoriesRequest) {
    this.store.load(filter);
  }
}
