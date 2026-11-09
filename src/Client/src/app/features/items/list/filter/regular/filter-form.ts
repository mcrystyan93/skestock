import { Component, computed, effect, input, linkedSignal, output, signal, untracked } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import {
  buildEqualsFilterForDropdown,
  buildItemActiveFilter,
  CategoryDropdownValue,
  ColumnFilter,
  GetAllItemsRequest,
  getDropdownFilterValue,
  getItemActiveFilter,
  ITEM_ACTIVE_FILTER_FIELD,
  ItemActiveFilter
} from '@ske/models';
import { debounce, form, FormField, submit } from '@angular/forms/signals';
import { isEqual, isNil } from 'lodash-es';
import { FormsModule } from '@angular/forms';
import { NzFormDirective } from 'ng-zorro-antd/form';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzInputDirective, NzInputPrefixDirective, NzInputWrapperComponent } from 'ng-zorro-antd/input';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { CategoryDropdown } from '@ske/shared/categories';
import { LayoutBreakpoint } from '@ske/shared/directives';
import { NzBadgeComponent } from 'ng-zorro-antd/badge';
import { NzDrawerComponent, NzDrawerContentDirective } from 'ng-zorro-antd/drawer';
import { NzSegmentedComponent, NzSegmentedItemComponent } from 'ng-zorro-antd/segmented';

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
    NzButtonComponent,
    CategoryDropdown,
    NzInputPrefixDirective,
    NgTemplateOutlet,
    LayoutBreakpoint,
    NzBadgeComponent,
    NzDrawerComponent,
    NzDrawerContentDirective,
    NzSegmentedComponent,
    NzSegmentedItemComponent
  ],
  selector: 'ske-item-filter-form',
  styles: ``,
  templateUrl: './filter-form.html'
})
export class FilterForm {
  public readonly loading = input.required<boolean>();
  public readonly filter = input.required<GetAllItemsRequest>();

  public readonly selectedCount = input(0);

  public readonly onFilterChange = output<GetAllItemsRequest>();
  public readonly onMerge = output<void>();
  public readonly activeSegmentOptions: { label: string; value: ItemActiveFilter }[] = [
    { label: 'Active', value: 'active' },
    { label: 'Inactive', value: 'inactive' },
    { label: 'Toate', value: 'all' }
  ];
  private _initialFilterEmitted = false;
  private _initialFormChangeHandled = false;

  private readonly _formModel = linkedSignal({
    source: () => this.filter(),
    computation: (filter) => (<ItemListFilterModel>{
      searchTerm: filter.searchTerm ?? '',
      category: getDropdownFilterValue(filter.filters, 'categoryId') as CategoryDropdownValue,
      active: getItemActiveFilter(filter.filters)
    })
  });

  public readonly itemListFilterForm = form(this._formModel, (schemaPath) => {
    debounce(schemaPath.searchTerm, 300);
  });

  public readonly filtersOpen = signal(false);
  public readonly canMerge = computed(() => this.selectedCount() >= 2);

  public readonly activeFilterCount = computed(() =>
    (this.itemListFilterForm().value().category ? 1 : 0) +
    (this.itemListFilterForm().value().active === DEFAULT_ACTIVE ? 0 : 1)
  );

  public readonly filtersButtonLabel = computed(() => {
    const count = this.activeFilterCount();

    return count === 0 ? 'Filtre' : `Filtre, ${count} ${count === 1 ? 'filtru activ' : 'filtre active'}`;
  });

  private readonly _initialFilterEffectRef = effect(() => {
    if (this._initialFilterEmitted)
      return;
    // Wait until required inputs are initialized, then trigger the first list load.
    this.filter();

    this.onFilterChange.emit(this.buildFilterCriteria());

    this._initialFilterEmitted = true;
  });

  private readonly _formEffectChange = effect(() => {
    this.itemListFilterForm().value();

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

  private readonly _categoryEffectChange = effect(() => {
    const categoryValue = this._getCategoryValue();
    const categoryFormValue = this.itemListFilterForm().value().category;

    if (categoryValue === (categoryFormValue?.id ?? null))
      return;

    untracked(() => this.onSubmit());
  });

  private readonly _getCategoryValue = computed(() => {
    const currentFilter = this.filter();

    return currentFilter.filters.find(filter => filter.field === 'categoryId')?.value ?? null;
  });

  private buildFilterCriteria(): GetAllItemsRequest {
    const criteria = this.itemListFilterForm().value();

    return {
      ...this.filter(),
      searchTerm: criteria.searchTerm,
      filters: [
        ...this.filter().filters.filter(filter => !ITEM_FILTER_FIELDS.has(filter.field)),
        ...[
          buildEqualsFilterForDropdown('categoryId', criteria.category),
          buildItemActiveFilter(criteria.active)
        ].filter((filter): filter is ColumnFilter => filter !== null)
      ]
    };
  }

  public async onSubmit() {
    let data: ItemListFilterModel | null = null;
    let isValid = submit(this.itemListFilterForm, async (_) => {
      data = this.itemListFilterForm().value();
    });

    if (!isValid)
      return;

    if (isNil(data))
      return;

    this.onFilterChange.emit(this.buildFilterCriteria());
  }

  public clear() {
    this.itemListFilterForm().reset({
      searchTerm: '',
      category: null,
      active: DEFAULT_ACTIVE
    });

    this.onSubmit();
  }
}

type ItemListFilterModel = {
  searchTerm: string;
  category: CategoryDropdownValue;
  active: ItemActiveFilter;
}
const ITEM_FILTER_FIELDS = new Set(['categoryId', ITEM_ACTIVE_FILTER_FIELD])
const DEFAULT_ACTIVE: ItemActiveFilter = 'active';
