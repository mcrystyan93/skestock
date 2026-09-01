import { Component, input, linkedSignal, signal } from '@angular/core';
import { CategoryDto, ItemDto, LocationDto } from '@ske/models';
import { form, FormField, required, submit, validate } from '@angular/forms/signals';
import { NzFormControlComponent, NzFormDirective, NzFormItemComponent, NzFormLabelComponent } from 'ng-zorro-antd/form';
import { ItemDropdown } from '@ske/shared/items';
import { NzInputNumberComponent } from 'ng-zorro-antd/input-number';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzInputAddonAfterDirective } from 'ng-zorro-antd/input';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzDatePickerComponent } from 'ng-zorro-antd/date-picker';
import { CategoryDropdown } from '@ske/shared/categories';
import { NzSpaceCompactComponent } from 'ng-zorro-antd/space';
import { LocationDropdown } from '@ske/shared/locations';

@Component({
  imports: [
    NzFormDirective,
    NzFormControlComponent,
    NzFormLabelComponent,
    NzFormItemComponent,
    ItemDropdown,
    FormField,
    NzInputNumberComponent,
    NzButtonComponent,
    NzInputAddonAfterDirective,
    NzIconDirective,
    NzDatePickerComponent,
    CategoryDropdown,
    NzSpaceCompactComponent,
    LocationDropdown
  ],
  selector: 'ske-add-stock-batch-form',
  styles: ``,
  templateUrl: './form.html'
})
export class Form {
  public readonly category = input<Partial<CategoryDto> | null>();

  private readonly _formModel = linkedSignal({
    source: () => ({
      category: this.category(),
      initialState: this._initialState()
    }),
    computation: (state) => (
      <StockBatchFormModel>{ ...state.initialState, category: state.category }
    )
  });

  private readonly _initialState = signal<StockBatchFormModel>({
    category: null,
    item: null,
    location: null,
    quantity: 0,
    unitPrice: 0,
    receivedDate: new Date(),
    expiryDate: null
  });

  public readonly stockBatchForm = form(this._formModel, (schemaPath) => {
    required(schemaPath.category, {
      message: 'Categoria este obligatorie.'
    });
    required(schemaPath.item, {
      message: 'Produsul este obligatoriu.'
    });
    required(schemaPath.quantity, {
      message: 'Cantitatea este obligatorie.'
    });
    required(schemaPath.location,{
      message: 'Locația este obligatorie.'
    });
    validate(schemaPath.quantity, (ctx) => {
      const isValid = ctx.valueOf(schemaPath.quantity) > 0;

      if (isValid)
        return undefined;

      return {
        kind: 'invalidQuantity',
        message: 'Cantitatea trebuie să fie mai mare decât 0.'
      };
    });
  });

  public incrementQuantity() {
    this.stockBatchForm.quantity().value.update(prev => (prev ?? 0) + 1);
  }

  public decrementQuantity() {
    this.stockBatchForm.quantity().value.update(prev => (prev ?? 0) - 1);
  }

  public async submit(): Promise<StockBatchFormSubmit> {
    let formData: StockBatchFormModel | null = null;
    const isValid = await submit(this.stockBatchForm, async () => {
      formData = this.stockBatchForm().value();
    });

    return {
      isValid,
      formData: formData!
    };
  }
}

export type StockBatchFormModel = {
  category: CategoryDto | null;
  item: ItemDto | null;
  location: LocationDto | null;
  quantity: number;
  unitPrice: number;
  receivedDate: Date;
  expiryDate: Date | null;
};
export type StockBatchFormSubmit = {
  isValid: boolean;
  formData: StockBatchFormModel;
}
