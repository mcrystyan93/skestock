import { Component, DestroyRef, inject, input, model } from '@angular/core';
import { SchoolClassOverviewStore } from '../../services/school-class-overview.store';
import { Header } from './header';
import { NzModalService } from 'ng-zorro-antd/modal';
import { AddGoodsReceiptModal } from '@ske/shared/goods-receipts';
import { Router } from '@angular/router';
import { isNil } from 'lodash-es';
import { OrderListDetailModal } from '@ske/shared/order-lists';
import { NzMessageService } from 'ng-zorro-antd/message';
import { injectDispatch } from '@ngrx/signals/events';
import { StockCountModal, StockCountModalData, StockEvents } from '@ske/shared/stock';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs';
import { CloseSchoolClassModal, CloseSchoolClassModalData } from '../close-class/close-school-class-modal';

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
      nzWrapClassName: 'modal-100 modal-lg-75',
      nzCentered: true,
      nzClosable: false
    });
  }

  public onAddProduct() {
    this._stockDispatcher.addProduct();
  }

  public openStockCount() {
    const classId = this.classId();

    if (isNil(classId) || classId.trim().length === 0) {
      this._nzMessageService.error('Clasa nu este disponibilă pentru inventar.');
      return;
    }

    const modalRef = this._nzModalService.create<StockCountModal, StockCountModalData, boolean>({
      nzContent: StockCountModal,
      nzData: { classId },
      nzWrapClassName: 'modal-100 modal-lg-75',
      nzCentered: true,
      nzMaskClosable: false,
      nzClosable: true,
      // Route the X through the modal's own close() so unsaved counts get the discard confirmation.
      nzOnCancel: (instance) => {
        instance.close();
        return false;
      }
    });

    modalRef.afterClose.pipe(
      filter((saved) => saved === true),
      takeUntilDestroyed(this._destroyRef)
    ).subscribe(() => {
      this._stockDispatcher.refresh();
      // The summary also refreshes from the SignalR stockAdjusted event, but not when that connection is down.
      this.store.loadSummary(classId);
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
      nzWrapClassName: 'modal-100 modal-lg-75',
      nzCentered: true,
      nzMaskClosable: false
    });
  }

  public closeClass() {
    const schoolClass = this.store.schoolClass();

    if (isNil(schoolClass.id)) {
      this._nzMessageService.error('Clasa nu este disponibilă pentru închidere.');
      return;
    }

    const modalRef = this._nzModalService.create<CloseSchoolClassModal, CloseSchoolClassModalData, boolean>({
      nzContent: CloseSchoolClassModal,
      nzData: { classId: schoolClass.id, className: schoolClass.name ?? '' },
      nzCentered: true,
      nzMaskClosable: false
    });

    modalRef.afterClose.pipe(
      filter((closed) => closed === true),
      takeUntilDestroyed(this._destroyRef)
    ).subscribe(() => {
      this.store.load(schoolClass.id!);
      this._stockDispatcher.refresh();
    });
  }

  public close() {
    this._router.navigate(['school-classes']);
  }
}
