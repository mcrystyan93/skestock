import { Component, inject } from '@angular/core';
import { GetAllSchoolClassesRequest } from '@ske/models';
import { SchoolClassListState } from '../../services/school-class-list.store';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { FilterForm } from './filter-form';

@Component({
  imports: [
    NzCardComponent,
    FilterForm
  ],
  selector: 'ske-school-class-filter-container',
  styles: ``,
  templateUrl: './filter-container.html'
})
export class FilterContainer {
  public readonly store = inject(SchoolClassListState);

  public onFilterChange(filter: GetAllSchoolClassesRequest) {
    this.store.load(filter);
  }
}
