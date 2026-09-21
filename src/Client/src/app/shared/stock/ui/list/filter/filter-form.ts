import { Component, effect, input, linkedSignal, output, untracked } from '@angular/core';
import {
  buildEqualsFilterForDropdown,
  CategoryDropdownValue,
  ColumnFilter,
  GetClassLocationStockRequest,
  getDropdownFilterValue,
  LocationDropdownValue
} from '@ske/models';
import { debounce, form, FormField, submit } from '@angular/forms/signals';
import { isEqual, isNil } from 'lodash-es';
import { FormsModule } from '@angular/forms';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzSpaceCompactComponent } from 'ng-zorro-antd/space';
import { LocationDropdown } from '@ske/shared/locations';
import { CategoryDropdown } from '@ske/shared/categories';
import { NzSegmentedComponent, NzSegmentedItemComponent } from 'ng-zorro-antd/segmented';
import { NzInputDirective, NzInputPrefixDirective, NzInputWrapperComponent } from 'ng-zorro-antd/input';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzDividerComponent } from 'ng-zorro-antd/divider';

@Component({
  imports: [
    FormsModule,
    NzRowDirective,
    NzColDirective,
    FormField,
    LocationDropdown,
    NzSpaceCompactComponent,
    CategoryDropdown,
    NzSegmentedComponent,
    NzSegmentedItemComponent,
    NzInputWrapperComponent,
    NzIconDirective,
    NzInputPrefixDirective,
    NzInputDirective,
    NzButtonComponent,
    NzDividerComponent
  ],
  selector: 'ske-stock-filter-form',
  styles: `
  `,
  templateUrl: './filter-form.html'
})
export class FilterForm {
  public readonly loading = input.required<boolean>();
  public readonly filter = input.required<GetClassLocationStockRequest>();

  public readonly onFilterChange = output<GetClassLocationStockRequest>();
  public readonly onAdd = output<StockFilterAddPrefill>();
  private _initialFormChangeHandled = false;

  public readonly booleanSegmentOptions = [
    { label: 'Toate', value: StockBooleanField.All },
    { label: 'Ascunse', value: StockBooleanField.IncludeHidden },
    { label: 'Stoc redus', value: StockBooleanField.LowStockOnly },
    { label: 'Expirate', value: StockBooleanField.ExpiredOnly }
  ];

  private readonly _formModel = linkedSignal({
    source: () => this.filter(),
    computation: (filter) => (<StockListFilterModel>{
      searchTerm: filter.searchTerm ?? '',
      location: getDropdownFilterValue(filter.filters, 'locationId'),
      category: getDropdownFilterValue(filter.filters, 'categoryId'),
      booleanSegmentValue: filter.includeHidden ? StockBooleanField.IncludeHidden :
        filter.lowStockOnly ? StockBooleanField.LowStockOnly :
          filter.expiredOnly ? StockBooleanField.ExpiredOnly : StockBooleanField.All
    })
  });

  public readonly stockListFilterForm = form(this._formModel, (schemaPath) => {
    debounce(schemaPath.searchTerm, 300);
  });

  private readonly _formEffectChange = effect(() => {
    this.stockListFilterForm().value();

    if (!this._initialFormChangeHandled) {
      this._initialFormChangeHandled = true;
      return;
    }

    const currentFilter = untracked(() => this.filter());
    const nextFilter = untracked(() => this.buildFilterCriteria());

    if (isEqual(currentFilter, nextFilter))
      return;

    untracked(() => this.onSubmit());
  });

  private buildFilterCriteria(): GetClassLocationStockRequest {
    const criteria = this.stockListFilterForm().value();

    return {
      ...this.filter(),
      searchTerm: criteria.searchTerm,
      filters: [
        ...this.filter().filters.filter(filter => !STOCK_FILTER_FIELDS.has(filter.field)),
        ...[
          buildEqualsFilterForDropdown('locationId', criteria.location),
          buildEqualsFilterForDropdown('categoryId', criteria.category)
        ].filter((filter): filter is ColumnFilter => filter !== null)
      ],
      includeHidden: criteria.booleanSegmentValue === StockBooleanField.IncludeHidden,
      lowStockOnly: criteria.booleanSegmentValue === StockBooleanField.LowStockOnly,
      expiredOnly: criteria.booleanSegmentValue === StockBooleanField.ExpiredOnly
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
      category: null,
      booleanSegmentValue: StockBooleanField.All
    });

    this.onSubmit();
  }

  public addStockBatch() {
    const { category, location } = this.stockListFilterForm().value();

    this.onAdd.emit({ category, location });
  }
}

type StockListFilterModel = {
  searchTerm: string;
  location: LocationDropdownValue;
  category: CategoryDropdownValue;
  booleanSegmentValue: StockBooleanField;
}

export type StockFilterAddPrefill = {
  category: CategoryDropdownValue;
  location: LocationDropdownValue;
};

const STOCK_FILTER_FIELDS = new Set(['locationId', 'categoryId']);

enum StockBooleanField {
  All = 'all',
  IncludeHidden = 'includeHidden',
  LowStockOnly = 'lowStockOnly',
  ExpiredOnly = 'expiredOnly'
}
