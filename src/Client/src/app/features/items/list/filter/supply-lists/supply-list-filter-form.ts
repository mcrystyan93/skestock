import { Component, effect, input, linkedSignal, output, untracked } from '@angular/core';
import {
  buildSupplyListActiveFilter,
  ColumnFilter,
  GetAllSupplyListsRequest,
  getSupplyListActiveFilter,
  SUPPLY_LIST_ACTIVE_FILTER_FIELD,
  SupplyListActiveFilter
} from '@ske/models';
import { debounce, form, FormField, submit } from '@angular/forms/signals';
import { FormsModule } from '@angular/forms';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzFormControlComponent, NzFormDirective, NzFormItemComponent } from 'ng-zorro-antd/form';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzInputDirective, NzInputPrefixDirective, NzInputWrapperComponent } from 'ng-zorro-antd/input';
import { NzSegmentedComponent, NzSegmentedItemComponent } from 'ng-zorro-antd/segmented';
import { isEqual } from 'lodash-es';

export type SupplyListFilterModel = {
  searchTerm: string;
  active: SupplyListActiveFilter;
};

const DEFAULT_FILTER: SupplyListFilterModel = { searchTerm: '', active: 'active' };

@Component({
  imports: [FormsModule, NzFormDirective, NzFormItemComponent, NzFormControlComponent, NzRowDirective,
    NzColDirective, NzInputWrapperComponent, NzIconDirective, NzInputDirective, FormField,
    NzButtonComponent, NzInputPrefixDirective, NzSegmentedComponent, NzSegmentedItemComponent],
  selector: 'ske-supply-list-filter-form',
  templateUrl: './supply-list-filter-form.html'
})
export class SupplyListFilterForm {
  public readonly filter = input.required<GetAllSupplyListsRequest>();
  public readonly onFilterChange = output<GetAllSupplyListsRequest>();

  public readonly activeSegmentOptions: { label: string; value: SupplyListActiveFilter }[] = [
    { label: 'Active', value: 'active' },
    { label: 'Inactive', value: 'inactive' },
    { label: 'Toate', value: 'all' }
  ];

  private _initialFilterEmitted = false;
  private _initialFormChangeHandled = false;

  private readonly _formModel = linkedSignal({
    source: () => this.filter(),
    computation: (filter): SupplyListFilterModel => ({
      searchTerm: filter.searchTerm ?? '',
      active: getSupplyListActiveFilter(filter.filters)
    })
  });

  public readonly filterForm = form(this._formModel, (path) => {
    debounce(path.searchTerm, 300);
  });

  private readonly _initialFilterEffectRef = effect(() => {
    if (this._initialFilterEmitted)
      return;

    this.filter();
    this._initialFilterEmitted = true;
    untracked(() => this.onFilterChange.emit(this.buildFilterCriteria()));
  });

  private readonly _formChangeEffectRef = effect(() => {
    this.filterForm().value();

    if (!this._initialFormChangeHandled) {
      this._initialFormChangeHandled = true;
      return;
    }

    untracked(() => {
      if (!isEqual(this.filter(), this.buildFilterCriteria()))
        void this.onSubmit();
    });
  });

  public async onSubmit() {
    const valid = await submit(this.filterForm, async () => undefined);

    if (valid)
      this.onFilterChange.emit(this.buildFilterCriteria());
  }

  public clear() {
    this.filterForm().reset({ ...DEFAULT_FILTER });
    void this.onSubmit();
  }

  public buildFilterCriteria(): GetAllSupplyListsRequest {
    const { searchTerm, active } = this.filterForm().value();

    return {
      ...this.filter(),
      searchTerm: searchTerm.trim() || null,
      filters: [
        ...this.filter().filters.filter((f) => f.field !== SUPPLY_LIST_ACTIVE_FILTER_FIELD),
        buildSupplyListActiveFilter(active)
      ].filter((f): f is ColumnFilter => f !== null)
    };
  }
}
