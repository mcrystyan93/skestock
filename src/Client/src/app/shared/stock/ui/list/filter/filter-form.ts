import { Component, input, linkedSignal, output } from '@angular/core';
import { GetClassLocationStockRequest } from '@ske/models';
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
      searchTerm: filter.searchTerm ?? ''
    })
  });

  public readonly stockListFilterForm = form(this._formModel);

  private buildFilterCriteria(): GetClassLocationStockRequest {
    const criteria = this.stockListFilterForm().value();

    return {
      ...this.filter(),
      searchTerm: criteria.searchTerm
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
      searchTerm: ''
    });

    this.onSubmit();
  }
}

type StockListFilterModel = {
  searchTerm: string;
}
