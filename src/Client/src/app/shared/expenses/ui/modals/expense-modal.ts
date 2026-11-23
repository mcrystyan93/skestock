import { Component, computed, effect, inject, viewChild } from '@angular/core';
import { NZ_MODAL_DATA, NzModalFooterDirective, NzModalRef, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { NzBadgeComponent } from 'ng-zorro-antd/badge';
import { NzAlertComponent } from 'ng-zorro-antd/alert';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { ErrorAlert } from '@ske/shared/errors';
import { FileStorageState } from '@ske/shared/storage';
import { EXPENSE_SOURCE_LABELS, EXPENSE_STATUS_COLORS, EXPENSE_STATUS_LABELS } from '@ske/models';
import { ExpenseDetailStore } from '../../services/expense-detail.store';
import { ExpenseForm } from './expense-form';

export type ExpenseModalData = {
  classId: string;
  /** Null creates a manual expense. */
  expenseId: string | null;
  readOnly: boolean;
};

@Component({
  imports: [
    NzModalTitleDirective,
    NzModalFooterDirective,
    NzButtonComponent,
    NzSpaceComponent,
    NzSpaceItemDirective,
    NzBadgeComponent,
    NzAlertComponent,
    NzTypographyComponent,
    ExpenseForm,
    ErrorAlert
  ],
  selector: 'ske-expense-modal',
  templateUrl: './expense-modal.html',
  providers: [ExpenseDetailStore, FileStorageState]
})
export class ExpenseModal {
  private readonly _modalData = inject<ExpenseModalData>(NZ_MODAL_DATA);
  private readonly _nzModalRef = inject(NzModalRef);

  public readonly store = inject(ExpenseDetailStore);
  public readonly fileStorage = inject(FileStorageState);

  public readonly readOnly = this._modalData.readOnly;
  public readonly isNew = this._modalData.expenseId === null;

  private readonly _form = viewChild(ExpenseForm);

  public readonly expense = this.store.expense;
  public readonly title = computed(() => (this.isNew ? 'Cheltuială nouă' : 'Cheltuială'));
  public readonly statusLabel = computed(() => EXPENSE_STATUS_LABELS[this.expense()?.status ?? 'processed']);
  public readonly statusColor = computed(() => EXPENSE_STATUS_COLORS[this.expense()?.status ?? 'processed']);
  public readonly sourceLabel = computed(() => EXPENSE_SOURCE_LABELS[this.expense()?.source ?? 'manual']);
  public readonly locked = computed(() => this.readOnly || this.expense()?.status === 'processing');
  public readonly canSave = computed(() => !this.readOnly && this.expense()?.status !== 'processing');
  public readonly canRetry = computed(() =>
    !this.readOnly && this.expense()?.status === 'failed' && !!this.expense()?.fileMetadataId);

  private readonly _closeAfterRetryEffect = effect(() => {
    if (this.store.retried())
      this._nzModalRef.close('retried');
  });

  private readonly _closeAfterSaveEffect = effect(() => {
    if (this.store.saved())
      this._nzModalRef.close(this.store.saved());
  });

  constructor() {
    if (this._modalData.expenseId)
      this.store.load(this._modalData.expenseId);
  }

  public close() {
    this._nzModalRef.close();
  }

  public downloadFile() {
    const fileId = this.expense()?.fileMetadataId;

    if (fileId)
      this.fileStorage.downloadFile(fileId);
  }

  public retry() {
    const id = this._modalData.expenseId;

    if (id)
      this.store.retry(id);
  }

  public async save() {
    const form = this._form();

    if (!form)
      return;

    const { isValid, fields } = await form.submit();

    if (!isValid || fields === null)
      return;

    this.store.save({ expenseId: this._modalData.expenseId, classId: this._modalData.classId, fields });
  }
}
