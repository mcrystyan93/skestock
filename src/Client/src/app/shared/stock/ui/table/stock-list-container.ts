import { Component, DestroyRef, inject } from '@angular/core';
import { StockStore } from '../../services/stock.store';
import { CategoryDto, getDropdownFilterValue, StockItemDto } from '@ske/models';
import { NzModalService } from 'ng-zorro-antd/modal';
import { StockAdjustmentModal, StockAdjustmentModalData } from '../modals/adjust/stock-adjustment-modal';
import { StockExtendExpiryModal, StockExtendExpiryModalData } from '../modals/extend-expiry/stock-extend-expiry-modal';
import { StockAddToOrderModal, StockAddToOrderModalData } from '../modals/add-to-order/stock-add-to-order-modal';
import { StockMoveModal, StockMoveModalData } from '../modals/move/stock-move-modal';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AddStockBatchModal } from '@ske/shared/stock-batches';
import { StockCategoryItemsTable } from './stock-category-items-table';
import { StockCategoryItemsSmall } from './stock-category-items-small';
import { LayoutBreakpoint } from '@ske/shared/directives';
import { Events } from '@ngrx/signals/events';
import { StockEvents } from '../../services/stock.events';

@Component({
  imports: [
    StockCategoryItemsTable,
    StockCategoryItemsSmall,
    LayoutBreakpoint
  ],
  selector: 'ske-stock-category-cards-container',
  styles: ``,
  templateUrl: './stock-list-container.html',
  providers: [NzModalService],
  host: {
    class: 'absolute block inset-0'
  }
})
export class StockListContainer {
  public readonly store = inject(StockStore);

  private readonly _nzModalService = inject(NzModalService);
  private readonly _destroyRef = inject(DestroyRef);
  private readonly _events = inject(Events);

  private readonly _stockEvents = this._events.on(StockEvents.addProduct)
    .pipe(takeUntilDestroyed(this._destroyRef))
    .subscribe(() => {
      const category = getDropdownFilterValue(this.store.filter().filters, 'categoryId');
      this.addStock({ id: category?.id });
    });

  public onAdjust(item: StockItemDto) {
    const classId = this.store.filter().classId;

    const modalRef = this._nzModalService.create<StockAdjustmentModal, StockAdjustmentModalData>({
      nzContent: StockAdjustmentModal,
      nzData: { classId, item },
      nzWrapClassName: 'modal-100 modal-lg-75',
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
      nzWrapClassName: 'modal-100 modal-lg-75',
      nzCentered: true,
      nzMaskClosable: false
    });

    modalRef.afterClose.pipe(
      takeUntilDestroyed(this._destroyRef)
    ).subscribe(() => {
      this.store.load(this.store.filter());
    });
  }

  public onAddToOrder(item: StockItemDto): void {
    this._nzModalService.create<StockAddToOrderModal, StockAddToOrderModalData>({
      nzContent: StockAddToOrderModal,
      nzData: { classId: this.store.filter().classId, item },
      nzWrapClassName: 'modal-w-90 modal-w-md-50 modal-w-xl-30',
      nzCentered: true,
      nzMaskClosable: false
    });
  }

  public onExtendExpiry(item: StockItemDto): void {
    const classId = this.store.filter().classId;

    const modalRef = this._nzModalService.create<StockExtendExpiryModal, StockExtendExpiryModalData, number>({
      nzContent: StockExtendExpiryModal,
      nzData: { item },
      nzWrapClassName: 'modal-w-90 modal-w-md-50 modal-w-xl-30',
      nzCentered: true,
      nzMaskClosable: false
    });

    modalRef.afterClose.pipe(
      takeUntilDestroyed(this._destroyRef)
    ).subscribe((extensionDays) => {
      if (extensionDays) {
        this.store.extendExpiredStockExpiry({
          classId,
          itemId: item.itemId,
          locationId: item.locationId,
          extensionDays
        });
      }
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
    const location = getDropdownFilterValue(this.store.filter().filters, 'locationId');

    const modalRef = this._nzModalService.create({
      nzContent: AddStockBatchModal,
      nzData: { schoolClassId: classId, category, location },
      nzWrapClassName: 'modal-100 modal-lg-75',
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
