import { Component, effect, input, linkedSignal, output } from '@angular/core';
import { ColumnFilter, GetAllCategoriesRequest } from '@ske/models';
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
  selector: 'ske-category-filter-form',
  styles: ``,
  templateUrl: './filter-form.html'
})
export class FilterForm {
  public readonly loading = input.required<boolean>();
  public readonly filter = input.required<GetAllCategoriesRequest>();

  public readonly onFilterChange = output<GetAllCategoriesRequest>();
  private initialFilterEmitted = false;

  private readonly _formModel = linkedSignal({
    source: () => this.filter(),
    computation: (filter) => (<CategoryListFilterModel>{
      searchTerm: filter.searchTerm ?? '',
      filters: filter.filters ?? []
    })
  });

  public readonly categoryListFilterForm = form(this._formModel);

  private readonly _initialFilterEffectRef = effect(() => {
    if (this.initialFilterEmitted)
      return;
    // Wait until required inputs are initialized, then trigger the first list load.
    this.filter();

    this.onFilterChange.emit(this.buildFilterCriteria());

    this.initialFilterEmitted = true;
  });

  private buildFilterCriteria(): GetAllCategoriesRequest {
    const criteria = this.categoryListFilterForm().value();

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
    let data: CategoryListFilterModel | null = null;
    let isValid = submit(this.categoryListFilterForm, async (_) => {
      data = this.categoryListFilterForm().value();
    });

    if (!isValid)
      return;

    if (isNil(data))
      return;

    this.onFilterChange.emit(this.buildFilterCriteria());
  }

  public clear() {
    this.categoryListFilterForm().reset({
      searchTerm: '',
      filters: []
    });

    this.onSubmit();
  }
}

type CategoryListFilterModel = {
  searchTerm: string;
  filters: Array<ColumnFilter>;
}
