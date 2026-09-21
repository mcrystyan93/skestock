import { Component, effect, input, linkedSignal, output, untracked } from '@angular/core';
import { GetAllCategoriesRequest } from '@ske/models';
import { debounce, form, FormField, submit } from '@angular/forms/signals';
import { isEqual, isNil } from 'lodash-es';
import { FormsModule } from '@angular/forms';
import { NzFormDirective } from 'ng-zorro-antd/form';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzInputDirective, NzInputPrefixDirective, NzInputWrapperComponent } from 'ng-zorro-antd/input';
import { NzIconDirective } from 'ng-zorro-antd/icon';
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
    NzButtonComponent,
    NzInputPrefixDirective
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
  private _initialFormChangeHandled = false;

  private readonly _formModel = linkedSignal({
    source: () => this.filter(),
    computation: (filter) => (<CategoryListFilterModel>{
      searchTerm: filter.searchTerm ?? ''
    })
  });

  public readonly categoryListFilterForm = form(this._formModel, (schemaPath) => {
    debounce(schemaPath.searchTerm, 300);
  });

  private readonly _initialFilterEffectRef = effect(() => {
    if (this.initialFilterEmitted)
      return;
    // Wait until required inputs are initialized, then trigger the first list load.
    this.filter();

    this.onFilterChange.emit(this.buildFilterCriteria());

    this.initialFilterEmitted = true;
  });

  private readonly _formEffectChange = effect(() => {
    this.categoryListFilterForm().value();

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

  private buildFilterCriteria(): GetAllCategoriesRequest {
    const criteria = this.categoryListFilterForm().value();

    return {
      ...this.filter(),
      searchTerm: criteria.searchTerm,
      filters: (this.filter().filters ?? [])
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
      searchTerm: ''
    });

    this.onSubmit();
  }
}

type CategoryListFilterModel = {
  searchTerm: string;
}
