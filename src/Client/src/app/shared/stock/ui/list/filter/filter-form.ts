import { Component, input, linkedSignal, output } from '@angular/core';
import {
  CategoryDropdownValue,
  ColumnFilter,
  GetClassLocationStockRequest,
  LocationDropdownValue
} from '@ske/models';
import { form, FormField, submit } from '@angular/forms/signals';
import { isNil } from 'lodash-es';
import { FormsModule } from '@angular/forms';
import { NzFormDirective } from 'ng-zorro-antd/form';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzInputDirective, NzInputWrapperComponent } from 'ng-zorro-antd/input';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { CategoryDropdown } from '@ske/shared/categories';
import { LocationDropdown } from '@ske/shared/locations';
import {NzDividerComponent} from 'ng-zorro-antd/divider';

@Component({
  imports: [
    FormsModule,
    NzFormDirective,
    NzRowDirective,
    NzColDirective,
    NzInputWrapperComponent,
    NzIconDirective,
    NzInputDirective,
    FormField,
    NzSpaceComponent,
    NzSpaceItemDirective,
    NzButtonComponent,
    CategoryDropdown,
    LocationDropdown,
    NzDividerComponent
  ],
  selector: 'ske-stock-filter-form',
  styles: ``,
  templateUrl: './filter-form.html'
})
export class FilterForm {
  public readonly loading = input.required<boolean>();
  public readonly filter = input.required<GetClassLocationStockRequest>();

  public readonly onFilterChange = output<GetClassLocationStockRequest>();

  private readonly _formModel = linkedSignal({
    source: () => this.filter(),
    computation: (filter) => (<StockListFilterModel>{
      searchTerm: filter.searchTerm ?? '',
      location: getDropdownFilterValue(filter.filters, 'locationId'),
      category: getDropdownFilterValue(filter.filters, 'categoryId')
    })
  });

  public readonly stockListFilterForm = form(this._formModel);

  private buildFilterCriteria(): GetClassLocationStockRequest {
    const criteria = this.stockListFilterForm().value();

    return {
      ...this.filter(),
      searchTerm: criteria.searchTerm,
      filters: [
        ...this.filter().filters.filter(filter => !STOCK_FILTER_FIELDS.has(filter.field)),
        ...[
          buildEqualsFilter('locationId', criteria.location),
          buildEqualsFilter('categoryId', criteria.category)
        ].filter((filter): filter is ColumnFilter => filter !== null)
      ]
    };
  }

  public async onSubmit() {
    let data: StockListFilterModel | null = null;
    let isValid = submit(this.stockListFilterForm, async (_) => {
      data = this.stockListFilterForm().value();
    });

    if (!isValid)
      return;

    if (isNil(data))
      return;

    this.onFilterChange.emit(this.buildFilterCriteria());
  }

  public clear() {
    this.stockListFilterForm().reset({
      searchTerm: '',
      location: null,
      category: null
    });

    this.onSubmit();
  }
}

type StockListFilterModel = {
  searchTerm: string;
  location: LocationDropdownValue;
  category: CategoryDropdownValue;
}

const STOCK_FILTER_FIELDS = new Set(['locationId', 'categoryId']);

function getDropdownFilterValue(
  filters: ColumnFilter[],
  field: string
): { id: string; name: string } | null {
  const selectedFilter = filters.find(filter =>
    filter.field === field && filter.operator === 'equals');

  if (isNil(selectedFilter?.value))
    return null;

  return {
    id: String(selectedFilter.value),
    name: selectedFilter.displayValue ?? ''
  };
}

function buildEqualsFilter(
  field: string,
  value: { id?: string; name?: string } | null
): ColumnFilter | null {
  if (isNil(value?.id) || value.id.length === 0)
    return null;

  return {
    field,
    operator: 'equals',
    value: value.id,
    fieldType: 'select',
    displayValue: value.name
  };
}
