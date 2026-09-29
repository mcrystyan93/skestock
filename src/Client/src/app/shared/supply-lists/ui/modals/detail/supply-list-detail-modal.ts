import { Component, computed, DestroyRef, effect, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Events, provideDispatcher } from '@ngrx/signals/events';
import { SaveSupplyListRequest } from '@ske/models';
import { ErrorAlert } from '@ske/shared/errors';
import { isNil } from 'lodash-es';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzMessageService } from 'ng-zorro-antd/message';
import {
  NZ_MODAL_DATA,
  NzModalFooterDirective,
  NzModalRef,
  NzModalService,
  NzModalTitleDirective
} from 'ng-zorro-antd/modal';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { supplyListApiEvents, SupplyListDetailState } from '../../../services/supply-list-detail.store';
import { SupplyListDetailForm, SupplyListDetailFormModel } from './form/supply-list-detail-form';

export type SupplyListDetailModalData = {
  id: string | null;
};

@Component({
  imports: [
    NzButtonComponent,
    NzModalFooterDirective,
    NzModalTitleDirective,
    NzSpaceComponent,
    NzSpaceItemDirective,
    NzTagComponent,
    ErrorAlert,
    SupplyListDetailForm
  ],
  selector: 'ske-supply-list-detail-modal',
  templateUrl: './supply-list-detail-modal.html',
  providers: [provideDispatcher(), SupplyListDetailState]
})
export class SupplyListDetailModal {
  public readonly modalData = signal<SupplyListDetailModalData>(inject(NZ_MODAL_DATA));
  public readonly store = inject(SupplyListDetailState);

  private readonly _nzModalRef = inject(NzModalRef);
  private readonly _nzModalService = inject(NzModalService);
  private readonly _nzMessageService = inject(NzMessageService);
  private readonly _formComponent = viewChild(SupplyListDetailForm);
  private readonly _storeEvents = inject(Events);
  private readonly _destroyRef = inject(DestroyRef);
  private readonly _pendingOperationId = signal<string | null>(null);
  private readonly _saved = signal(false);
  private _saveSequence = 0;
  private _initialLoad = false;

  public readonly busy = computed(() => this.store.supplyListLoading() || this._pendingOperationId() !== null);
  public readonly editable = computed(() => this.store.supplyList().isActive !== false);

  private readonly _initialLoadEffectRef = effect(() => {
    if (this._initialLoad)
      return;

    this._initialLoad = true;
    const { id } = this.modalData();

    if (!isNil(id))
      this.store.loadSupplyList(id);
  });

  private readonly _saveSuccessRef = this._storeEvents.on(supplyListApiEvents.saveSuccess)
    .pipe(takeUntilDestroyed(this._destroyRef))
    .subscribe(({ payload }) => {
      if (this._pendingOperationId() !== payload.operationId)
        return;

      this._pendingOperationId.set(null);
      this._saved.set(true);
      this._nzMessageService.success('Lista a fost salvată cu succes!');
      this.close(true);
    });

  private readonly _saveFailureRef = this._storeEvents.on(supplyListApiEvents.saveFailure)
    .pipe(takeUntilDestroyed(this._destroyRef))
    .subscribe(({ payload }) => {
      if (this._pendingOperationId() === payload.operationId)
        this._pendingOperationId.set(null);
    });

  public close(force = false) {
    if (!force && this.busy())
      return;

    if (!force && this._formComponent()?.supplyListForm().dirty()) {
      this._nzModalService.confirm({
        nzTitle: 'Renunțați la modificări?',
        nzContent: 'Modificările nesalvate vor fi pierdute.',
        nzOkText: 'Renunță',
        nzCancelText: 'Continuă editarea',
        nzOnOk: () => this._nzModalRef.close(this._saved())
      });
      return;
    }

    this._nzModalRef.close(this._saved());
  }

  public async save() {
    if (this.busy() || !this.editable())
      return;

    const formComponent = this._formComponent();

    if (isNil(formComponent))
      return;

    const { isValid, formData } = await formComponent.submit();

    if (!isValid || isNil(formData))
      return;

    const operationId = `save-${++this._saveSequence}`;
    this._pendingOperationId.set(operationId);
    this.store.saveSupplyList({ request: mapSupplyListSaveRequest(formData), operationId });
  }
}

export function mapSupplyListSaveRequest(formData: SupplyListDetailFormModel): SaveSupplyListRequest {
  return {
    name: formData.name.trim(),
    note: formData.note.trim() || null,
    frequency: formData.frequency,
    intervalWeeks: formData.frequency === 'EveryXWeeks' ? formData.intervalWeeks : null,
    lines: formData.lines.map((line) => ({
      itemId: line.itemId,
      quantity: line.quantity,
      unit: line.unit.trim() || null,
      notes: line.notes.trim() || null
    }))
  };
}
