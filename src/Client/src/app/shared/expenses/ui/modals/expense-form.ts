import { Component, input, linkedSignal } from '@angular/core';
import { disabled, form, FormField, max, maxLength, min, minLength, required, submit } from '@angular/forms/signals';
import { NzFormControlComponent, NzFormDirective, NzFormItemComponent, NzFormLabelComponent } from 'ng-zorro-antd/form';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzInputDirective } from 'ng-zorro-antd/input';
import { NzInputNumberComponent } from 'ng-zorro-antd/input-number';
import { NzDatePickerComponent } from 'ng-zorro-antd/date-picker';
import { ExpenseDto, ExpenseFieldsRequest } from '@ske/models';

type ExpenseFormModel = {
  supplierName: string;
  documentNumber: string;
  expenseDate: Date | null;
  currency: string;
  totalAmount: number | null;
  vatAmount: number | null;
};

export type ExpenseFormSubmit = {
  isValid: boolean;
  fields: ExpenseFieldsRequest | null;
};

@Component({
  imports: [
    NzFormDirective,
    NzFormItemComponent,
    NzFormLabelComponent,
    NzFormControlComponent,
    NzRowDirective,
    NzColDirective,
    NzInputDirective,
    NzInputNumberComponent,
    NzDatePickerComponent,
    FormField
  ],
  selector: 'ske-expense-form',
  templateUrl: './expense-form.html'
})
export class ExpenseForm {
  public readonly expense = input<ExpenseDto | null>(null);
  /** Read-only class, or the AI still reading (editing would be overwritten by its result). */
  public readonly locked = input(false);

  private readonly _model = linkedSignal<ExpenseDto | null, ExpenseFormModel>({
    source: this.expense,
    computation: (expense) => toFormModel(expense)
  });

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
    disabled(schema, () => this.locked());
  });

  public async submit(): Promise<ExpenseFormSubmit> {
    let fields: ExpenseFieldsRequest | null = null;

    const isValid = await submit(this.expenseForm, async () => {
      fields = toRequest(this.expenseForm().value(), this.expense()?.expenseCategoryId ?? null);
    });

    return { isValid, fields };
  }
}

function toFormModel(expense: ExpenseDto | null): ExpenseFormModel {
  return {
    supplierName: expense?.supplierName ?? '',
    documentNumber: expense?.documentNumber ?? '',
    expenseDate: expense?.expenseDate ? new Date(`${expense.expenseDate}T00:00:00`) : null,
    currency: expense?.currency ?? 'RON',
    totalAmount: expense?.totalAmount ?? null,
    vatAmount: expense?.vatAmount ?? null
  };
}

// The category has no editor yet, so an existing value is carried over instead of being cleared on save.
function toRequest(model: ExpenseFormModel, expenseCategoryId: string | null): ExpenseFieldsRequest {
  return {
    supplierName: model.supplierName.trim() || null,
    documentNumber: model.documentNumber.trim() || null,
    expenseDate: model.expenseDate ? toIsoDate(model.expenseDate) : null,
    currency: model.currency.trim().toUpperCase(),
    totalAmount: model.totalAmount,
    vatAmount: model.vatAmount,
    expenseCategoryId
  };
}

// Local date parts: toISOString() would shift the day for timezones ahead of UTC.
function toIsoDate(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}
