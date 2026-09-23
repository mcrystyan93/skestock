import { Component, computed, DestroyRef, effect, inject, signal, viewChild } from '@angular/core';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzDropdownDirective, NzDropdownMenuComponent } from 'ng-zorro-antd/dropdown';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzMenuDirective, NzMenuItemComponent } from 'ng-zorro-antd/menu';
import { NzSpaceCompactComponent, NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { orderListApiEvents, OrderListDetailState } from '../../../services/order-list-detail.store';
import { OrderListExportService } from '../../../services/order-list-export.service';
import {
  CreateOrderListRequest,
  ORDER_LIST_STATUS_ICONS,
  ORDER_LIST_STATUS_LABELS,
  OrderListLineRequest,
  UpdateOrderListRequest
} from '@ske/models';
import {
  NZ_MODAL_DATA,
  NzModalFooterDirective,
  NzModalRef,
  NzModalService,
  NzModalTitleDirective
} from 'ng-zorro-antd/modal';
import { isNil } from 'lodash-es';
import { ErrorAlert } from '@ske/shared/errors';
import { OrderListDetailForm, OrderListDetailFormModel } from './form/order-list-detail-form';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Events, provideDispatcher } from '@ngrx/signals/events';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzTagComponent } from 'ng-zorro-antd/tag';

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
    OrderListDetailForm,
    NzModalTitleDirective,
    NzTagComponent
  ],
  selector: 'ske-order-list-detail-modal',
  styles: ``,
  templateUrl: './order-list-detail-modal.html',
  providers: [provideDispatcher(), OrderListDetailState]
})
export class OrderListDetailModal {
  public readonly modalData = signal<OrderListDetailModalData>(inject(NZ_MODAL_DATA));

  public readonly store = inject(OrderListDetailState);
  public readonly exportService = inject(OrderListExportService);

  private readonly _nzModalRef = inject(NzModalRef);
  private readonly _formComponent = viewChild(OrderListDetailForm);
  private readonly _storeEvents = inject(Events);
  private readonly _nzMessageService = inject(NzMessageService);
  private readonly _nzModalService = inject(NzModalService);
  private readonly _destroyRef = inject(DestroyRef);
  private readonly _pendingSave = signal<PendingSave | null>(null);
  private _saveSequence = 0;

  public readonly busy = computed(() =>
    this.store.orderListLoading() || this._pendingSave() !== null
  );

  public readonly editable = computed(() => {
    const status = this.store.orderList().status;
    return isNil(status) || status === 'Draft';
  });

  public readonly statusLabel = computed(() =>
    ORDER_LIST_STATUS_LABELS[this.store.orderList().status ?? 'Draft']
  );

  public readonly statusIcon = computed(() =>
    ORDER_LIST_STATUS_ICONS[this.store.orderList().status ?? 'Draft']
  );

  public readonly canExport = computed(() =>
    this.store.orderList().status === 'Submitted' && !isNil(this.store.orderList().id)
  );

  public readonly downloading = computed(() => {
    const id = this.store.orderList().id;
    return !isNil(id) && this.exportService.isDownloading(id);
  });

  private initialLoad = false;

  private readonly _initialLoadEffectRef = effect(() => {
    if (this.initialLoad)
      return;

    this.initialLoad = true;
    const { id, classId } = this.modalData();

    if (typeof classId === 'string' && classId.trim().length > 0)
      this.store.loadLowStockItems(classId);

    if (isNil(id))
      return;

    this.store.loadOrderList({ id });
  });

  private readonly _saveSuccessRef = this._storeEvents.on(orderListApiEvents.saveSuccess)
    .pipe(
      takeUntilDestroyed(this._destroyRef)
    )
    .subscribe(({ payload }) => this.handleSaveSuccess(payload.operationId));

  private readonly _saveFailureRef = this._storeEvents.on(orderListApiEvents.saveFailure)
    .pipe(
      takeUntilDestroyed(this._destroyRef)
    )
    .subscribe(({ payload }) => this.handleSaveFailure(payload.operationId));

  public close(force = false) {
    if (!force && this.busy())
      return;

    if (!force && this._formComponent()?.orderListForm().dirty()) {
      this._nzModalService.confirm({
        nzTitle: 'Renunțați la modificări?',
        nzContent: 'Modificările nesalvate vor fi pierdute.',
        nzOkText: 'Renunță',
        nzCancelText: 'Continuă editarea',
        nzOnOk: () => this._nzModalRef.close()
      });
      return;
    }

    this._nzModalRef.close();
  }

  public download() {
    const id = this.store.orderList().id;

    if (!isNil(id))
      this.exportService.download(id);
  }

  public async save(shouldClose: boolean = true) {
    if (this.busy() || !this.editable())
      return;

    const formComponent = this._formComponent();

    if (isNil(formComponent))
      return;

    const { isValid, formData } = await formComponent.submit();

    if (!isValid || isNil(formData))
      return;

    const operationId = `save-${++this._saveSequence}`;
    this._pendingSave.set({ operationId, shouldClose });

    if (!this.store.saveOrderList(this.mapSaveRequest(formData), operationId))
      this._pendingSave.set(null);
  }

  private mapSaveRequest(formData: OrderListDetailFormModel): CreateOrderListRequest | UpdateOrderListRequest {
    const lines: OrderListLineRequest[] = formData.lines.map((line) => ({
      itemId: line.itemId ?? null,
      productName: line.productName.trim() || null,
      quantity: line.quantity,
      unit: line.unit.trim() || null,
      notes: line.notes.trim() || null
    }));

    if (isNil(formData.id)) {
      return {
        classId: this.modalData().classId ?? '',
        name: formData.name,
        note: formData.note,
        lines
      };
    }

    return {
      name: formData.name,
      note: formData.note,
      lines
    };
  }

  private handleSaveSuccess(operationId: string) {
    const pendingSave = this._pendingSave();

    if (pendingSave?.operationId !== operationId)
      return;

    this._pendingSave.set(null);
    this._nzMessageService.success('Comanda a fost salvată cu succes!');

    if (pendingSave.shouldClose) {
      this.close(true);
      return;
    }

    this.reloadModalData();
  }

  private handleSaveFailure(operationId: string) {
    if (this._pendingSave()?.operationId === operationId)
      this._pendingSave.set(null);
  }

  private reloadModalData() {
    const orderListId = this.store.orderList().id;

    if (!isNil(orderListId))
      this.store.loadOrderList({ id: orderListId });

    const classId = this.modalData().classId;

    if (typeof classId === 'string' && classId.trim().length > 0)
      this.store.loadLowStockItems(classId);
  }
}

type OrderListDetailModalData = {
  id?: string | null;
  classId: string | null;
};

type PendingSave = {
  operationId: string;
  shouldClose: boolean;
};
