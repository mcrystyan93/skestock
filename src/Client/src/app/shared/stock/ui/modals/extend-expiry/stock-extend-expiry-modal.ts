import { Component, computed, inject, signal } from '@angular/core';
import { form, FormField, max, min, required, submit, validate } from '@angular/forms/signals';
import { DatePipe } from '@angular/common';
import { addDays } from 'date-fns';
import { NZ_MODAL_DATA, NzModalFooterDirective, NzModalRef, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzFormControlComponent, NzFormDirective, NzFormItemComponent, NzFormLabelComponent } from 'ng-zorro-antd/form';
import { NzInputNumberComponent } from 'ng-zorro-antd/input-number';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { MAX_EXPIRY_EXTENSION_DAYS, StockItemDto } from '@ske/models';

@Component({
  imports: [
    DatePipe,
    FormField,
    NzModalTitleDirective,
    NzModalFooterDirective,
    NzButtonComponent,
    NzFormControlComponent,
    NzFormDirective,
    NzFormItemComponent,
    NzFormLabelComponent,
    NzInputNumberComponent,
    NzTypographyComponent,
    NzIconDirective
  ],
  selector: 'ske-stock-extend-expiry-modal',
  templateUrl: './stock-extend-expiry-modal.html'
})
export class StockExtendExpiryModal {
  public readonly modalData = inject<StockExtendExpiryModalData>(NZ_MODAL_DATA);
  protected readonly presets = [7, 14, 30];
  protected readonly maxDays = MAX_EXPIRY_EXTENSION_DAYS;

  private readonly _nzModalRef = inject(NzModalRef<StockExtendExpiryModal, number>);
  private readonly _model = signal({ days: 7 });

  protected readonly extendForm = form(this._model, (path) => {
    required(path.days, { message: 'Numărul de zile este obligatoriu.' });
    validate(path.days, (ctx) =>
      Number.isInteger(ctx.value()) ? undefined : { kind: 'integer', message: 'Numărul de zile trebuie să fie întreg.' }
    );
    min(path.days, 1, { message: 'Numărul de zile trebuie să fie cel puțin 1.' });
    max(path.days, MAX_EXPIRY_EXTENSION_DAYS, {
      message: `Numărul de zile nu poate depăși ${MAX_EXPIRY_EXTENSION_DAYS}.`
    });
  });

  protected readonly newExpiry = computed(() => {
    const days = this._model().days;

    if (!Number.isInteger(days) || days < 1) {
      return null;
    }

    return addDays(new Date(), days);
  });

  protected setDays(days: number): void {
    this._model.set({ days });
  }

  protected close(): void {
    this._nzModalRef.close();
  }

  protected async save(): Promise<void> {
    await submit(this.extendForm, async () => {
      this._nzModalRef.close(this._model().days);
    });
  }
}

export type StockExtendExpiryModalData = {
  item: StockItemDto;
};
