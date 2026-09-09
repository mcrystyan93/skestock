import { Component, effect, input, linkedSignal, output } from '@angular/core';
import { GetAllCategoryImportsRequest } from '@ske/models';
import { form, FormField, submit } from '@angular/forms/signals';
import { isNil } from 'lodash-es';
import { FormsModule } from '@angular/forms';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzFormDirective } from 'ng-zorro-antd/form';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzInputDirective, NzInputWrapperComponent } from 'ng-zorro-antd/input';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';

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
  selector: 'ske-category-import-filter-form',
  templateUrl: './category-import-filter-form.html'
})
export class FilterForm {
  public readonly loading = input.required<boolean>();
  public readonly filter = input.required<GetAllCategoryImportsRequest>();
  public readonly onFilterChange = output<GetAllCategoryImportsRequest>();

  private _initialFilterEmitted = false;
  private readonly _formModel = linkedSignal({
    source: () => this.filter(),
    computation: (filter) => ({
      searchTerm: filter.searchTerm ?? ''
    })
  });

  public readonly categoryImportFilterForm = form(this._formModel);

  private readonly _initialFilterEffectRef = effect(() => {
    if (this._initialFilterEmitted)
      return;

    this.filter();
    this.onFilterChange.emit(this.buildFilterCriteria());
    this._initialFilterEmitted = true;
  });

  public async onSubmit() {
    let data: CategoryImportFilterModel | null = null;
    const isValid = submit(this.categoryImportFilterForm, async (_) => {
      data = this.categoryImportFilterForm().value();
    });

    if (!isValid || isNil(data))
      return;

    this.onFilterChange.emit(this.buildFilterCriteria());
  }

  public clear() {
    this.categoryImportFilterForm().reset({ searchTerm: '' });
    this.onSubmit();
  }

  private buildFilterCriteria(): GetAllCategoryImportsRequest {
    const criteria = this.categoryImportFilterForm().value();

    return {
      ...this.filter(),
      searchTerm: criteria.searchTerm
    };
  }
}

type CategoryImportFilterModel = {
  searchTerm: string;
};
