import { Component, input, linkedSignal, output } from '@angular/core';
import { GetAllGoodsReceiptImportsRequest } from '@ske/models';
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
  selector: 'ske-goods-receipt-imports-filter-form',
  styles: ``,
  templateUrl: './filter-form.html'
})
export class FilterForm {
  public readonly loading = input.required<boolean>();
  public readonly filter = input.required<GetAllGoodsReceiptImportsRequest>();

  public readonly onFilterChange = output<GetAllGoodsReceiptImportsRequest>();

  private readonly _formModel = linkedSignal({
    source: () => this.filter(),
    computation: (filter) => (<GoodsReceiptImportListFilterModel>{
      searchTerm: filter.searchTerm ?? ''
    })
  });

  public readonly goodsReceiptImportsFilterForm = form(this._formModel);

  private buildFilterCriteria(): GetAllGoodsReceiptImportsRequest {
    const criteria = this.goodsReceiptImportsFilterForm().value();

    return {
      ...this.filter(),
      searchTerm: criteria.searchTerm
    };
  }

  public async onSubmit() {
    let data: GoodsReceiptImportListFilterModel | null = null;
    let isValid = submit(this.goodsReceiptImportsFilterForm, async (_) => {
      data = this.goodsReceiptImportsFilterForm().value();
    });

    if (!isValid)
      return;

    if (isNil(data))
      return;

    this.onFilterChange.emit(this.buildFilterCriteria());
  }

  public clear() {
    this.goodsReceiptImportsFilterForm().reset({
      searchTerm: ''
    });

    this.onSubmit();
  }
}

type GoodsReceiptImportListFilterModel = {
  searchTerm: string;
}
