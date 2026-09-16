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
import { StockHttp } from '../../services/stock.http';
import { NzMessageService } from 'ng-zorro-antd/message';
import { EMPTY, catchError, tap } from 'rxjs';

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
  private readonly _stockHttp = inject(StockHttp);
  private readonly _nzMessageService = inject(NzMessageService);

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

  public onRemoveExpired(item: StockItemDto): void {
    const classId = this.store.filter().classId;

    this._stockHttp.removeExpiredStock({
      classId,
      itemId: item.itemId,
      locationId: item.locationId
    }).pipe(
      takeUntilDestroyed(this._destroyRef),
      tap(() => {
        this._nzMessageService.success(`Au fost eliminate ${item.expiredQuantity} articole expirate.`);
        this.store.load(this.store.filter());
      }),
      catchError(() => {
        this._nzMessageService.error('Articolele expirate nu au putut fi eliminate.');
        return EMPTY;
      })
    ).subscribe();
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

  public onVisibilityChange(item: StockItemDto): void {
    const classId = this.store.filter().classId;
    const hideWhenZeroStock = !item.hideWhenZeroStock;

    this._stockHttp.setClassItemStockVisibility(classId, item.itemId, { hideWhenZeroStock })
      .pipe(
        takeUntilDestroyed(this._destroyRef),
        tap(() => {
          this._nzMessageService.success(
            hideWhenZeroStock
              ? 'Produsul va fi ascuns când stocul ajunge la 0.'
              : 'Produsul nu va mai fi ascuns la stoc 0.'
          );
          this.store.load(this.store.filter());
        }),
        catchError(() => {
          this._nzMessageService.error('Setarea vizibilității produsului nu a putut fi salvată.');
          return EMPTY;
        })
      )
      .subscribe();
  }
}
