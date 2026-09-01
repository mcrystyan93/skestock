import { Component, effect, input, linkedSignal, output } from '@angular/core';
import { ColumnFilter, GetAllItemsRequest } from '@ske/models';
import { form, FormField, submit } from '@angular/forms/signals';
import { isNil } from 'lodash-es';
import { FormsModule } from '@angular/forms';
import { NzFormDirective } from 'ng-zorro-antd/form';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzInputDirective, NzInputWrapperComponent } from 'ng-zorro-antd/input';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { NzButtonComponent } from 'ng-zorro-antd/button';

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
    NzButtonComponent
  ],
  selector: 'ske-item-filter-form',
  styles: ``,
  templateUrl: './filter-form.html'
})
export class FilterForm {
  public readonly loading = input.required<boolean>();
  public readonly filter = input.required<GetAllItemsRequest>();

  public readonly onFilterChange = output<GetAllItemsRequest>();
  private initialFilterEmitted = false;

  private readonly _formModel = linkedSignal({
    source: () => this.filter(),
    computation: (filter) => (<ItemListFilterModel>{
      searchTerm: filter.searchTerm ?? '',
      filters: filter.filters ?? []
    })
  });

  public readonly itemListFilterForm = form(this._formModel);

  private readonly _initialFilterEffectRef = effect(() => {
    if (this.initialFilterEmitted)
      return;
    // Wait until required inputs are initialized, then trigger the first list load.
    this.filter();

    this.onFilterChange.emit(this.buildFilterCriteria());

    this.initialFilterEmitted = true;
  });

  private buildFilterCriteria(): GetAllItemsRequest {
    const criteria = this.itemListFilterForm().value();

    return {
      ...this.filter(),
      searchTerm: criteria.searchTerm,
      filters: (criteria.filters ?? [])
        .filter(
          (f) => !isNil(f.value) && !isNil(f.fieldType) && !isNil(f.operator) && !isNil(f.field)
        )
        .map((f) => ({
          value: f.value,
          fieldType: f.fieldType,
          operator: f.operator,
          field: f.field,
          displayValue: f.displayValue,
          booleanDisplaySelector: f.booleanDisplaySelector
        }))
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
      filters: []
    });

    this.onSubmit();
  }
}

type ItemListFilterModel = {
  searchTerm: string;
  filters: Array<ColumnFilter>;
}
