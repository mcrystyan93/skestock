import { Component, DestroyRef, inject, input, model } from '@angular/core';
import { SchoolClassOverviewStore } from '../../services/school-class-overview.store';
import { Header } from './header';
import { NzModalService } from 'ng-zorro-antd/modal';
import { AddGoodsReceiptModal } from '@ske/shared/goods-receipts';
import { ReviewModal, ReviewModalData } from '@ske/shared/goods-receipt-imports';
import { Router } from '@angular/router';
import { isNil } from 'lodash-es';
import { OrderListDetailModal } from '@ske/shared/order-lists';
import { NzMessageService } from 'ng-zorro-antd/message';
import { injectDispatch } from '@ngrx/signals/events';
import { StockEvents } from '@ske/shared/stock';

@Component({
  imports: [
    Header
  ],
  selector: 'ske-school-class-overview-header-container',
  styles: ``,
  templateUrl: './header-container.html',
  providers: [NzModalService]
})
export class HeaderContainer {
  public readonly selectedTabIndex = model<number>(0);
  public readonly classId = input.required<string | null>();
  public readonly store = inject(SchoolClassOverviewStore);
  private readonly _stockDispatcher = injectDispatch(StockEvents);

  private readonly _router = inject(Router);
  private readonly _nzModalService = inject(NzModalService);
  private readonly _nzMessageService = inject(NzMessageService);
  private readonly _destroyRef = inject(DestroyRef);

  public addGoodsReceipt() {
    this._nzModalService.create({
      nzContent: AddGoodsReceiptModal,
      nzData: {
        classId: this.classId()
      },
      nzCentered: true,
      nzClosable: false
    });
  }

  public onAddProduct() {
    this._stockDispatcher.addProduct();
  }

  private openGoodsReceiptImportReview(importId: string) {
    this._nzModalService.create({
      nzContent: ReviewModal,
      nzData: <ReviewModalData>{ importId },
      nzWidth: '90vw',
      nzCentered: true,
      nzMaskClosable: false
    });
  }

  public createOrderList() {
    const classId = this.classId();

    if (isNil(classId) || classId.trim().length === 0) {
      this._nzMessageService.error('Clasa nu este disponibilă pentru crearea comenzii.');
      return;
    }

    const modalRef = this._nzModalService.create({
      nzContent: OrderListDetailModal,
      nzData: { classId },
      nzWrapClassName: 'modal-90',
      nzCentered: true,
      nzMaskClosable: false
    });
  }

  public openAnalytics() {
    const classId = this.classId();
    if (classId) {
      void this._router.navigate(['/class-analytics'], { queryParams: { classId } });
    }
  }

  public close() {
    this._router.navigate(['school-classes']);
  }
}
