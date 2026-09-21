import { Component, DestroyRef, effect, inject, signal, viewChild } from '@angular/core';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzDropdownDirective, NzDropdownMenuComponent } from 'ng-zorro-antd/dropdown';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzMenuDirective, NzMenuItemComponent } from 'ng-zorro-antd/menu';
import { NzSpaceCompactComponent, NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { orderListApiEvents, OrderListDetailState } from '../../../services/order-list-detail.store';
import { CreateOrderListRequest, UpdateOrderListRequest } from '@ske/models';
import { NZ_MODAL_DATA, NzModalFooterDirective, NzModalRef } from 'ng-zorro-antd/modal';
import { isNil } from 'lodash-es';
import { ErrorAlert } from '@ske/shared/errors';
import { OrderListDetailForm, OrderListDetailFormModel } from './form/order-list-detail-form';
import { BehaviorSubject, filter, switchMap, tap } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Events } from '@ngrx/signals/events';
import { NzMessageService } from 'ng-zorro-antd/message';

@Component({
  imports: [
    NzButtonComponent,
    NzDropdownDirective,
    NzDropdownMenuComponent,
    NzIconDirective,
    NzMenuDirective,
    NzMenuItemComponent,
    NzSpaceCompactComponent,
    NzSpaceComponent,
    NzModalFooterDirective,
    NzSpaceItemDirective,
    ErrorAlert,
    OrderListDetailForm
  ],
  selector: 'ske-order-list-detail-modal',
  styles: ``,
  templateUrl: './order-list-detail-modal.html',
  providers: [OrderListDetailState]
})
export class OrderListDetailModal {
  public readonly modalData = signal<OrderListDetailModalData>(inject(NZ_MODAL_DATA));

  public readonly store = inject(OrderListDetailState);

  private readonly _nzModalRef = inject(NzModalRef);
  private readonly _formComponent = viewChild(OrderListDetailForm);
  private readonly _close$ = new BehaviorSubject(false);
  private readonly _storeEvents = inject(Events);
  private readonly _nzMessageService = inject(NzMessageService);
  private readonly _destroyRef = inject(DestroyRef);
  private initialLoad = false;

  private readonly _initialLoadEffectRef = effect(() => {
    if (this.initialLoad)
      return;

    const { id, classId } = this.modalData();

    if (!isNil(classId))
      this.store.loadLowStockItems(classId);

    if (isNil(id))
      return;

    this.store.loadOrderList({ id });
    this.initialLoad = true;
  });

  private readonly _saveSuccessRef = this._storeEvents.on(orderListApiEvents.saveSuccess)
    .pipe(
      takeUntilDestroyed(this._destroyRef),
      tap(() => this._nzMessageService.success('Comanda a fost salvat cu succes!')),
      switchMap(() => this._close$),
      filter((shouldClose) => shouldClose),
      tap(() => this.close())
    )
    .subscribe();

  public close() {
    this._nzModalRef.close();
  }

  public async save(shouldClose: boolean = true) {
    const formComponent = this._formComponent();

    if (isNil(formComponent))
      return;

    const { isValid, formData } = await formComponent.submit();

    if (!isValid || isNil(formData))
      return;

    this.store.saveOrderList(this.mapSaveRequest(formData));

    if (shouldClose)
      this._close$.next(true);
  }

  private mapSaveRequest(formData: OrderListDetailFormModel): CreateOrderListRequest | UpdateOrderListRequest {
    if (isNil(formData.id)) {
      return {
        classId: this.modalData().classId,
        name: formData.name,
        note: formData.note,
        lines: formData.lines
      };
    }

    return {
      name: formData.name,
      note: formData.note,
      lines: formData.lines
    };
  }
}

type OrderListDetailModalData = {
  id: string | null;
  classId: string;
}
