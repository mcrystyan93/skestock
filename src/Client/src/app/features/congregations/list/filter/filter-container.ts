import { Component, inject } from '@angular/core';
import { GetAllCongregationsRequest } from '@ske/models';
import { CongregationListState } from '../../services/congregation-list.store';
import { FilterForm } from './filter-form';

@Component({
  imports: [
    FilterForm
  ],
  selector: 'ske-congregation-filter-container',
  template: `
    <ske-congregation-filter-form [loading]="store.congregationsLoading()"
                                  [filter]="store.filter()"
                                  (onFilterChange)="onFilterChange($event)" />
  `,
  host: {
    class: 'px-4'
  }
})
export class FilterContainer {
  public readonly store = inject(CongregationListState);

  public onFilterChange(filter: GetAllCongregationsRequest) {
    this.store.load(filter);
  }
}
