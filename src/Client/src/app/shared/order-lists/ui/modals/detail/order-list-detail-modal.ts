import { Component, computed, DestroyRef, effect, inject, signal, viewChild } from '@angular/core';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzDropdownDirective, NzDropdownMenuComponent } from 'ng-zorro-antd/dropdown';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzMenuDirective, NzMenuItemComponent } from 'ng-zorro-antd/menu';
import { NzSpaceCompactComponent, NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import {
  CreateOrderListRequest,
  ORDER_LIST_STATUS_COLORS,
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
import {
  NEW_ORDER_LIST_ROUTE_ID,
  orderListApiEvents,
  OrderListDetailState,
  OrderListExportService,
  OrderListWizardStore
} from '@ske/shared/order-lists/services';
import { OrderListWizardContainer } from '../wizard/order-list-wizard-container';

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
    NzTagComponent,
    OrderListWizardContainer
  ],
  selector: 'ske-order-list-detail-modal',
  styles: ``,
  templateUrl: './order-list-detail-modal.html',
  providers: [provideDispatcher(), OrderListDetailState, OrderListWizardStore]
})
export class OrderListDetailModal {
  public readonly modalData = signal<OrderListDetailModalData>(inject(NZ_MODAL_DATA));

  public readonly store = inject(OrderListDetailState);
  public readonly wizard = inject(OrderListWizardStore);
  public readonly exportService = inject(OrderListExportService);

  private readonly _nzModalRef = inject(NzModalRef);
  private readonly _formComponent = viewChild(OrderListDetailForm);
  private readonly _storeEvents = inject(Events);
  private readonly _nzMessageService = inject(NzMessageService);
  private readonly _nzModalService = inject(NzModalService);
  private readonly _destroyRef = inject(DestroyRef);
  private readonly _pendingSave = signal<PendingSave | null>(null);
  private readonly _pendingSubmitOperationId = signal<string | null>(null);
  private readonly _approvalInProgress = signal(false);
  private _saveSequence = 0;
  private _submitSequence = 0;

  public readonly busy = computed(() =>
    this.store.orderListLoading() || this._pendingSave() !== null
  );

  // A new order goes through the wizard until it is saved; after that it behaves like any opened order.
  public readonly isWizard = computed(() =>
    isNil(this.modalData().id) && isNil(this.store.orderList().id)
  );

  public readonly onEditorStep = computed(() =>
    !this.isWizard() || this.wizard.step() === 'edit'
  );

  public readonly editable = computed(() => {
    const status = this.store.orderList().status;
    return isNil(status) || status === 'Draft';
  });

  public readonly canShowApproval = computed(() =>
    !this.isWizard()
    && this.onEditorStep()
    && this.store.orderList().status === 'Draft'
  );

  public readonly hasLines = computed(() => {
    const formLines = this._formComponent()?.orderListForm.lines().value();
    return (formLines ?? this.store.orderList().lines ?? []).length > 0;
  });

  public readonly statusLabel = computed(() =>
    ORDER_LIST_STATUS_LABELS[this.store.orderList().status ?? 'Draft']
  );

  public readonly statusColor = computed(() =>
    ORDER_LIST_STATUS_COLORS[this.store.orderList().status ?? 'Draft']
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
    const { id } = this.modalData();

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

  private readonly _submitSuccessRef = this._storeEvents.on(orderListApiEvents.submitSuccess)
    .pipe(
      takeUntilDestroyed(this._destroyRef)
    )
    .subscribe(({ payload }) => this.handleSubmitSuccess(payload.operationId));

  private readonly _submitFailureRef = this._storeEvents.on(orderListApiEvents.submitFailure)
    .pipe(
      takeUntilDestroyed(this._destroyRef)
    )
    .subscribe(({ payload }) => this.handleSubmitFailure(payload.operationId));

  public next() {
    const wizard = this.wizard;

    if (wizard.step() !== 'selection' || wizard.prefillKey() === wizard.loadedPrefillKey()) {
      wizard.advance(this.modalData().classId);
      return;
    }

    if (!wizard.canGoNext())
      return;

    const hasEdits = wizard.loadedPrefillKey() !== null && this._formComponent()?.orderListForm().dirty();

    if (!hasEdits) {
      this.openEditorWithPrefill();
      return;
    }

    this._nzModalService.confirm({
      nzTitle: 'Actualizați lista din selecția nouă?',
      nzContent: 'Selecția s-a schimbat, iar modificările făcute în listă vor fi pierdute.',
      nzOkText: 'Actualizează',
      nzCancelText: 'Rămâi',
      nzOnOk: () => this.openEditorWithPrefill()
    });
  }

  public back() {
    this.wizard.back();
  }

  private openEditorWithPrefill() {
    if (!this.wizard.advance(this.modalData().classId))
      return;

    this.store.loadOrderList({ id: NEW_ORDER_LIST_ROUTE_ID, prefill: this.wizard.buildPrefill() });
    this.wizard.markPrefillLoaded();
  }

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

  public downloadImage() {
    const id = this.store.orderList().id;

    if (!isNil(id))
      this.exportService.download(id, 'png');
  }

  public approve() {
    if (!this.canShowApproval() || this.busy() || this._approvalInProgress() || !this.hasLines())
      return;

    this._nzModalService.confirm({
      nzTitle: 'Aprobați comanda?',
      nzContent: 'Trimiterea finalizează planul comenzii pentru următorul pas de aprovizionare. '
        + 'Comanda nu va mai putea fi modificată. Doriți să continuați?',
      nzOkText: 'Confirmă trimiterea',
      nzCancelText: 'Renunță',
      nzOnOk: () => this.submitAfterConfirmation()
    });
  }

  public async save(shouldClose: boolean = true) {
    await this.saveDraft(shouldClose, false);
  }

  private async saveDraft(shouldClose: boolean, shouldSubmit: boolean) {
    if (this.busy() || !this.editable())
      return;

    const formComponent = this._formComponent();

    if (isNil(formComponent))
      return;

    const { isValid, formData } = await formComponent.submit();

    if (!isValid || isNil(formData))
      return;

    const operationId = `save-${++this._saveSequence}`;
    this._pendingSave.set({ operationId, shouldClose, shouldSubmit });

    if (!this.store.saveOrderList(this.mapSaveRequest(formData), operationId))
      this._pendingSave.set(null);
  }

  private async submitAfterConfirmation() {
    if (!this.canShowApproval() || this.busy() || this._approvalInProgress() || !this.hasLines())
      return;

    this._approvalInProgress.set(true);

    try {
      const formComponent = this._formComponent();

      if (isNil(formComponent))
        return;

      if (formComponent.orderListForm().dirty()) {
        await this.saveDraft(false, true);
        return;
      }

      this.submit();
    } finally {
      this._approvalInProgress.set(false);
    }
  }

  private submit() {
    const orderListId = this.store.orderList().id;

    if (isNil(orderListId) || this.busy())
      return;

    const operationId = `submit-${++this._submitSequence}`;
    this._pendingSubmitOperationId.set(operationId);
    this.store.submitOrderList({ operationId });
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

    if (pendingSave.shouldSubmit) {
      this._formComponent()?.orderListForm().reset();
      this.submit();
      return;
    }

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

  private handleSubmitSuccess(operationId: string) {
    if (this._pendingSubmitOperationId() !== operationId)
      return;

    this._pendingSubmitOperationId.set(null);
    this._nzMessageService.success('Comanda a fost aprobată.');
  }

  private handleSubmitFailure(operationId: string) {
    if (this._pendingSubmitOperationId() === operationId)
      this._pendingSubmitOperationId.set(null);
  }

  private reloadModalData() {
    const orderListId = this.store.orderList().id;

    if (!isNil(orderListId))
      this.store.loadOrderList({ id: orderListId });
  }
}

type OrderListDetailModalData = {
  id?: string | null;
  classId: string | null;
};

type PendingSave = {
  operationId: string;
  shouldClose: boolean;
  shouldSubmit: boolean;
};
