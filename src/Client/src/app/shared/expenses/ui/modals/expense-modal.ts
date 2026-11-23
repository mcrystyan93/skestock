import { Component, computed, effect, inject, signal } from '@angular/core';
import { NZ_MODAL_DATA, NzModalFooterDirective, NzModalRef, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { NzFormControlComponent, NzFormDirective, NzFormItemComponent, NzFormLabelComponent } from 'ng-zorro-antd/form';
import { NzInputDirective } from 'ng-zorro-antd/input';
import { NzInputNumberComponent } from 'ng-zorro-antd/input-number';
import { NzBadgeComponent } from 'ng-zorro-antd/badge';
import { NzAlertComponent } from 'ng-zorro-antd/alert';
import { disabled, form, FormField, max, maxLength, min, minLength, required, submit } from '@angular/forms/signals';
import { FormsModule } from '@angular/forms';
import { ErrorAlert } from '@ske/shared/errors';
import { FileStorageState } from '@ske/shared/storage';
import { EXPENSE_SOURCE_LABELS, EXPENSE_STATUS_COLORS, EXPENSE_STATUS_LABELS, ExpenseDto, ExpenseFieldsRequest } from '@ske/models';
import { ExpenseDetailStore } from '../../services/expense-detail.store';
import { ExpensesHttp } from '../../services/expenses.http';

export type ExpenseModalData = {
  classId: string;
  /** Null creates a manual expense. */
  expenseId: string | null;
  readOnly: boolean;
};

type ExpenseFormModel = {
  supplierName: string;
  documentNumber: string;
  expenseDate: string;
  currency: string;
  totalAmount: number | null;
  vatAmount: number | null;
};

const emptyModel = (): ExpenseFormModel => ({
  supplierName: '',
  documentNumber: '',
  expenseDate: '',
  currency: 'RON',
  totalAmount: null,
  vatAmount: null
});

@Component({
  imports: [
    NzModalTitleDirective,
    NzModalFooterDirective,
    NzButtonComponent,
    NzSpaceComponent,
    NzSpaceItemDirective,
    NzFormDirective,
    NzFormItemComponent,
    NzFormLabelComponent,
    NzFormControlComponent,
    NzInputDirective,
    NzInputNumberComponent,
    NzBadgeComponent,
    NzAlertComponent,
    FormsModule,
    FormField,
    ErrorAlert
  ],
  selector: 'ske-expense-modal',
  templateUrl: './expense-modal.html',
  providers: [ExpenseDetailStore, FileStorageState]
})
export class ExpenseModal {
  private readonly _modalData = inject<ExpenseModalData>(NZ_MODAL_DATA);
  private readonly _nzModalRef = inject(NzModalRef);
  private readonly _expensesHttp = inject(ExpensesHttp);

  public readonly store = inject(ExpenseDetailStore);
  public readonly fileStorage = inject(FileStorageState);

  public readonly readOnly = this._modalData.readOnly;
  public readonly isNew = this._modalData.expenseId === null;
  public readonly retrying = signal(false);

  private readonly _model = signal<ExpenseFormModel>(emptyModel());

  public readonly expenseForm = form(this._model, (schema) => {
    maxLength(schema.supplierName, 300, { message: 'Maxim 300 de caractere' });
    maxLength(schema.documentNumber, 100, { message: 'Maxim 100 de caractere' });
    required(schema.currency, { message: 'Moneda este obligatorie' });
    minLength(schema.currency, 3, { message: 'Cod de 3 litere (ex. RON)' });
    maxLength(schema.currency, 3, { message: 'Cod de 3 litere (ex. RON)' });
    min(schema.totalAmount, 0, { message: 'Totalul nu poate fi negativ' });
    min(schema.vatAmount, 0, { message: 'TVA nu poate fi negativ' });
    max(schema.vatAmount, ({ valueOf }) => valueOf(schema.totalAmount) ?? Number.MAX_SAFE_INTEGER, {
      message: 'TVA nu poate depăși totalul'
    });
    // Editing while the AI is still reading would be overwritten by its result.
    disabled(schema, () => this._modalData.readOnly || this.expense()?.status === 'processing');
  });

  public readonly expense = this.store.expense;
  public readonly title = computed(() => (this.isNew ? 'Cheltuială nouă' : 'Cheltuială'));
  public readonly statusLabel = computed(() => EXPENSE_STATUS_LABELS[this.expense()?.status ?? 'processed']);
  public readonly statusColor = computed(() => EXPENSE_STATUS_COLORS[this.expense()?.status ?? 'processed']);
  public readonly sourceLabel = computed(() => EXPENSE_SOURCE_LABELS[this.expense()?.source ?? 'manual']);
  public readonly canSave = computed(() => !this.readOnly && this.expense()?.status !== 'processing');
  public readonly canRetry = computed(() =>
    !this.readOnly && this.expense()?.status === 'failed' && !!this.expense()?.fileMetadataId);

  private readonly _fillFormEffect = effect(() => {
    const expense = this.expense();

    if (expense)
      this._model.set(toFormModel(expense));
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

    if (!id)
      return;

    this.retrying.set(true);
    this._expensesHttp.retry(id).subscribe({
      next: () => this._nzModalRef.close('retried'),
      error: () => this.retrying.set(false)
    });
  }

  public async save() {
    let fields: ExpenseFieldsRequest | null = null;

    const isValid = await submit(this.expenseForm, async () => {
      fields = toRequest(this.expenseForm().value(), this.expense()?.expenseCategoryId ?? null);
    });

    if (!isValid || fields === null)
      return;

    this.store.save({ expenseId: this._modalData.expenseId, classId: this._modalData.classId, fields });
  }
}

function toFormModel(expense: ExpenseDto): ExpenseFormModel {
  return {
    supplierName: expense.supplierName ?? '',
    documentNumber: expense.documentNumber ?? '',
    expenseDate: expense.expenseDate ?? '',
    currency: expense.currency,
    totalAmount: expense.totalAmount ?? null,
    vatAmount: expense.vatAmount ?? null
  };
}

// The category has no editor yet, so an existing value is carried over instead of being cleared on save.
function toRequest(model: ExpenseFormModel, expenseCategoryId: string | null): ExpenseFieldsRequest {
  return {
    supplierName: model.supplierName.trim() || null,
    documentNumber: model.documentNumber.trim() || null,
    expenseDate: model.expenseDate || null,
    currency: model.currency.trim().toUpperCase(),
    totalAmount: model.totalAmount,
    vatAmount: model.vatAmount,
    expenseCategoryId
  };
}
