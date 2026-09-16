import { Component, input, linkedSignal } from '@angular/core';
import { form, FormField, required, submit, validate } from '@angular/forms/signals';
import { LocationDropdown } from '@ske/shared/locations';
import { LocationDto, StockItemDto } from '@ske/models';
import { NzAlertComponent } from 'ng-zorro-antd/alert';
import {
  NzFormControlComponent,
  NzFormDirective,
  NzFormItemComponent,
  NzFormLabelComponent
} from 'ng-zorro-antd/form';
import { NzInputNumberComponent } from 'ng-zorro-antd/input-number';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';

@Component({
  imports: [
    LocationDropdown,
    FormField,
    NzAlertComponent,
    NzFormControlComponent,
    NzFormDirective,
    NzFormItemComponent,
    NzFormLabelComponent,
    NzInputNumberComponent,
    NzSpaceComponent,
    NzSpaceItemDirective
  ],
  selector: 'ske-stock-move-form',
  styles: ``,
  templateUrl: './stock-move-form.html'
})
export class StockMoveForm {
  public readonly loading = input.required<boolean>();
  public readonly item = input.required<StockItemDto>();

  private readonly _formModel = linkedSignal({
    source: () => this.item(),
    computation: (item) => (<StockMoveFormModel>{
      destination: null,
      quantity: item.quantity > 0 ? 1 : 0
    })
  });

  public readonly stockMoveForm = form(this._formModel, (schemaPath) => {
    required(schemaPath.destination, {
      message: 'Locația destinație este obligatorie.'
    });
    required(schemaPath.quantity, {
      message: 'Cantitatea este obligatorie.'
    });
    validate(schemaPath.quantity, (ctx) => {
      const quantity = ctx.valueOf(schemaPath.quantity) ?? 0;

      if (quantity <= 0) {
        return {
          kind: 'invalidQuantity',
          message: 'Cantitatea trebuie să fie mai mare decât 0.'
        };
      }

      if (!Number.isInteger(quantity)) {
        return {
          kind: 'invalidQuantity',
          message: 'Cantitatea trebuie să fie un număr întreg.'
        };
      }

      if (quantity > this.item().quantity) {
        return {
          kind: 'quantityExceedsAvailable',
          message: 'Cantitatea nu poate depăși stocul disponibil.'
        };
      }

      return undefined;
    });
  });

  public async submit(): Promise<StockMoveFormSubmit> {
    let formData: StockMoveFormModel | null = null;
    const isValid = await submit(this.stockMoveForm, async () => {
      formData = this.stockMoveForm().value();
    });

    return { isValid, formData };
  }
}

export type StockMoveFormModel = {
  destination: LocationDto | null;
  quantity: number;
};

export type StockMoveFormSubmit = {
  isValid: boolean;
  formData: StockMoveFormModel | null;
};
