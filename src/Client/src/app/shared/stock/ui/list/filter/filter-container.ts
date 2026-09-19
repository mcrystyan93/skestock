import { Component, inject } from '@angular/core';
import { GetClassLocationStockRequest } from '@ske/models';
import { StockStore } from '../../../services/stock.store';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { FilterForm, type StockFilterAddPrefill } from './filter-form';
import { isNil } from 'lodash-es';
import { AddStockBatchModal } from '@ske/shared/stock-batches';
import { NzModalService } from 'ng-zorro-antd/modal';

@Component({
  imports: [
    NzCardComponent,
    FilterForm
  ],
  selector: 'ske-stock-filter-container',
  styles: ``,
  templateUrl: './filter-container.html',
  providers: [NzModalService]
})
export class FilterContainer {
  public readonly store = inject(StockStore);
  private readonly _nzModalService = inject(NzModalService);

  public onFilterChange(filter: GetClassLocationStockRequest) {
    this.store.load(filter);
  }

  public onAdd(prefill: StockFilterAddPrefill) {
    const classId = this.store.filter().classId;

    if (isNil(classId))
      return;

    const modalRef = this._nzModalService.create({
      nzContent: AddStockBatchModal,
      nzData: { schoolClassId: classId, ...prefill },
      nzCentered: true,
      nzMaskClosable: false
    });
  }
}
