import { Component, DestroyRef, inject } from '@angular/core';
import { StockCategoryCards } from './stock-category-cards';
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
    StockCategoryCards,
    NzEmptyComponent
  ],
  selector: 'ske-stock-category-cards-container',
  styles: ``,
  templateUrl: './stock-category-cards-container.html',
  providers: [NzModalService],
  host: {
    class: 'absolute block inset-0'
  }
})
export class StockCategoryCardsContainer {
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

  public onRemoveExpired(item: StockItemDto): void {
    const classId = this.store.filter().classId;

    this.store.removeExpiredStock({
      request: {
        classId,
        itemId: item.itemId,
        locationId: item.locationId
      },
      expiredQuantity: item.expiredQuantity
    });
  }

  public addStock(category: Partial<CategoryDto> | null = null) {
    const classId = this.store.filter().classId;

    const modalRef = this._nzModalService.create({
      nzContent: AddStockBatchModal,
      nzData: { schoolClassId: classId, category, location: null },
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

    this.store.setClassItemStockVisibility({
      classId,
      itemId: item.itemId,
      locationId: item.locationId,
      request: { hideWhenZeroStock }
    });
  }
}
