import { Component, effect, input, linkedSignal, output, untracked } from '@angular/core';
import {
  buildEqualsFilterForValue,
  ColumnFilter,
  GetAllGoodsReceiptImportsRequest,
  getFilterValue,
  GoodsReceiptImportStatus
} from '@ske/models';
import { debounce, form, FormField, submit } from '@angular/forms/signals';
import { isEqual, isNil } from 'lodash-es';
import { FormsModule } from '@angular/forms';
import { NzFormDirective } from 'ng-zorro-antd/form';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzInputDirective, NzInputWrapperComponent } from 'ng-zorro-antd/input';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzButtonComponent } from 'ng-zorro-antd/button';
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
    NzSegmentedComponent,
    NzSegmentedItemComponent
  ],
  selector: 'ske-goods-receipt-imports-filter-form',
  styles: ``,
  templateUrl: './filter-form.html'
})
export class FilterForm {
  public readonly loading = input.required<boolean>();
  public readonly filter = input.required<GetAllGoodsReceiptImportsRequest>();

  public readonly onFilterChange = output<GetAllGoodsReceiptImportsRequest>();

  private _initialFormChangeHandled = false;

  private readonly _formModel = linkedSignal({
    source: () => this.filter(),
    computation: (filter) => (<GoodsReceiptImportListFilterModel>{
      searchTerm: filter.searchTerm ?? '',
      status: getFilterValue<string>(filter.filters, 'status') ?? 'all'
    })
  });

  public readonly statusSegmentOptions: { label: string, value: GoodsReceiptImportStatus }[] = [
    { label: 'Toate', value: 'all' },
    { label: 'Procesare', value: 'processing' },
    { label: 'Asteptare', value: 'pendingReview' },
    { label: 'Confirmate', value: 'confirmed' },
    { label: 'Esuate', value: 'failed' }
  ];

  public readonly goodsReceiptImportsFilterForm = form(this._formModel, (schemaPath) => {
    debounce(schemaPath.searchTerm, 300);
  });

  private readonly _formEffectChange = effect(() => {
    this.goodsReceiptImportsFilterForm().value();

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
      searchTerm: '',
      status: 'all'
    });

    this.onSubmit();
  }

  private buildFilterCriteria(): GetAllGoodsReceiptImportsRequest {
    const criteria = this.goodsReceiptImportsFilterForm().value();
    const statusFilter = criteria.status && criteria.status !== 'all' ? criteria.status : null;
    return {
      ...this.filter(),
      searchTerm: criteria.searchTerm,
      filters: [
        ...this.filter().filters.filter(f => f.field !== 'status'),
        ...[
          buildEqualsFilterForValue('status', statusFilter)
        ].filter((filter): filter is ColumnFilter => filter !== null)
      ]
    };
  }
}

type GoodsReceiptImportListFilterModel = {
  searchTerm: string;
  status: GoodsReceiptImportStatus;
}
