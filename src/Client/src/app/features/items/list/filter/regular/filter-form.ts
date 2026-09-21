import { Component, computed, effect, input, linkedSignal, output, untracked } from '@angular/core';
import {
  buildEqualsFilterForDropdown,
  CategoryDropdownValue,
  ColumnFilter,
  GetAllItemsRequest,
  getDropdownFilterValue
} from '@ske/models';
import { debounce, form, FormField, submit } from '@angular/forms/signals';
import { isEqual, isNil } from 'lodash-es';
import { FormsModule } from '@angular/forms';
import { NzFormDirective } from 'ng-zorro-antd/form';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzInputDirective, NzInputPrefixDirective, NzInputWrapperComponent } from 'ng-zorro-antd/input';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { CategoryDropdown } from '@ske/shared/categories';

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
    NzInputPrefixDirective
  ],
  selector: 'ske-item-filter-form',
  styles: ``,
  templateUrl: './filter-form.html'
})
export class FilterForm {
  public readonly loading = input.required<boolean>();
  public readonly filter = input.required<GetAllItemsRequest>();

  public readonly onFilterChange = output<GetAllItemsRequest>();
  private _initialFilterEmitted = false;
  private _initialFormChangeHandled = false;

  private readonly _formModel = linkedSignal({
    source: () => this.filter(),
    computation: (filter) => (<ItemListFilterModel>{
      searchTerm: filter.searchTerm ?? '',
      category: getDropdownFilterValue(filter.filters, 'categoryId') as CategoryDropdownValue
    })
  });

  public readonly itemListFilterForm = form(this._formModel, (schemaPath) => {
    debounce(schemaPath.searchTerm, 300);
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
          buildEqualsFilterForDropdown('categoryId', criteria.category)
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
      category: null
    });

    this.onSubmit();
  }
}

type ItemListFilterModel = {
  searchTerm: string;
  category: CategoryDropdownValue;
}
const ITEM_FILTER_FIELDS = new Set(['categoryId']);
