import { Component, DestroyRef, inject } from '@angular/core';
import { Table } from './table';
import { StockStore } from '../../services/stock.store';
import { CategoryDto, StockItemDto } from '@ske/models';
import { NzModalService } from 'ng-zorro-antd/modal';
import { StockAdjustmentModal, StockAdjustmentModalData } from '../modals/adjust/stock-adjustment-modal';
import { StockMoveModal, StockMoveModalData } from '../modals/move/stock-move-modal';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AddStockBatchModal } from '@ske/shared/stock-batches';
import { NzEmptyComponent } from 'ng-zorro-antd/empty';

@Component({
  imports: [
    Table,
    NzEmptyComponent
  ],
  selector: 'ske-stock-table-container',
  styles: ``,
  templateUrl: './table-container.html',
  providers: [NzModalService],
  host: {
    class: 'absolute block inset-0'
  }
})
export class TableContainer {
  public readonly store = inject(StockStore);

  private readonly _nzModalService = inject(NzModalService);
  private readonly _destroyRef = inject(DestroyRef);

  public onAdjust(item: StockItemDto) {
    const classId = this.store.filter().classId;

    const modalRef = this._nzModalService.create<StockAdjustmentModal, StockAdjustmentModalData>({
      nzContent: StockAdjustmentModal,
      nzData: { classId, item },
      nzCentered: true,
      nzMaskClosable: false
    });

    modalRef.afterClose.pipe(
      takeUntilDestroyed(this._destroyRef)
    ).subscribe(() => {
      this.store.load(this.store.filter());
    });
  }

  public onMove(item: StockItemDto) {
    const classId = this.store.filter().classId;

    const modalRef = this._nzModalService.create<StockMoveModal, StockMoveModalData>({
      nzContent: StockMoveModal,
      nzData: { classId, item },
      nzCentered: true,
      nzMaskClosable: false
    });

    modalRef.afterClose.pipe(
      takeUntilDestroyed(this._destroyRef)
    ).subscribe(() => {
      this.store.load(this.store.filter());
    });
  }

  public addStock(category: Partial<CategoryDto> | null = null) {
    const classId = this.store.filter().classId;

    const modalRef = this._nzModalService.create({
      nzContent: AddStockBatchModal,
      nzData: { schoolClassId: classId, category },
      nzCentered: true,
      nzMaskClosable: false
    });

    modalRef.afterClose.pipe(
      takeUntilDestroyed(this._destroyRef)
    ).subscribe(() => {
      this.store.load(this.store.filter());
    });
  }
}
