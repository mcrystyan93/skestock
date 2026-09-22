import { Component, inject } from '@angular/core';
import { GetAllSchoolClassesRequest } from '@ske/models';
import { SchoolClassListState } from '../../services/school-class-list.store';
import { FilterForm } from './filter-form';

@Component({
  imports: [
    FilterForm
  ],
  selector: 'ske-school-class-filter-container',
  styles: ``,
  templateUrl: './filter-container.html',
  host: {
    class: 'px-4'
  }
})
export class FilterContainer {
  public readonly store = inject(SchoolClassListState);

  public onFilterChange(filter: GetAllSchoolClassesRequest) {
    this.store.load(filter);
  }
}
