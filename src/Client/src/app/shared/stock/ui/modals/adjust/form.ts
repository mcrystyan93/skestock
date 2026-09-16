import { Component, input, linkedSignal } from '@angular/core';
import { AdjustmentReason, ADJUSTMENT_REASON_OPTIONS, StockItemDto } from '@ske/models';
import { form, FormField, min, required, submit } from '@angular/forms/signals';
import { NzFormControlComponent, NzFormDirective, NzFormItemComponent, NzFormLabelComponent } from 'ng-zorro-antd/form';
import { SkeletonInputLoaderDirective } from '@ske/shared/loader';
import { NzInputNumberComponent } from 'ng-zorro-antd/input-number';
import { NzOptionComponent, NzSelectComponent } from 'ng-zorro-antd/select';
import { NzAlertComponent } from 'ng-zorro-antd/alert';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';

@Component({
  imports: [
    NzFormDirective,
    SkeletonInputLoaderDirective,
    NzFormItemComponent,
    NzFormLabelComponent,
    NzFormControlComponent,
    FormField,
    NzInputNumberComponent,
    NzSelectComponent,
    NzOptionComponent,
    NzAlertComponent,
    NzSpaceComponent,
    NzSpaceItemDirective
  ],
  selector: 'ske-stock-adjustment-form',
  styles: ``,
  templateUrl: './form.html'
})
export class Form {
  public readonly loading = input.required<boolean>();
  public readonly item = input.required<StockItemDto>();

  public readonly reasonOptions = ADJUSTMENT_REASON_OPTIONS;

  private readonly _formModel = linkedSignal({
    source: () => this.item(),
    computation: (item) => (<StockAdjustmentFormModel>{
      actualQuantity: item.quantity,
      reason: AdjustmentReason.Adjustment
    })
  });

  public readonly stockAdjustmentForm = form(this._formModel, (schemaPath) => {
    required(schemaPath.actualQuantity, {
      message: 'Cantitatea numărată este obligatorie.'
    });
    min(schemaPath.actualQuantity, 0, {
      message: 'Cantitatea numărată nu poate fi negativă.'
    });
    required(schemaPath.reason, {
      message: 'Motivul ajustării este obligatoriu.'
    });
  });

  public async submit(): Promise<StockAdjustmentFormSubmit> {
    let formData: StockAdjustmentFormModel | null = null;
    const isValid = await submit(this.stockAdjustmentForm, async (_) => {
      formData = this.stockAdjustmentForm().value();
    });

    return { isValid, formData };
  }
}

export type StockAdjustmentFormModel = {
  actualQuantity: number;
  reason: AdjustmentReason;
};
export type StockAdjustmentFormSubmit = {
  isValid: boolean;
  formData: StockAdjustmentFormModel | null;
}
