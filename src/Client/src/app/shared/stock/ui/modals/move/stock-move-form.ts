import { Component, computed, inject, input, linkedSignal } from '@angular/core';
import { form, FormField, required, submit, validate } from '@angular/forms/signals';
import { LocationDropdownStore } from '@ske/shared/locations';
import { LocationDto, PAGINATION_PAGE_SIZE, StockItemDto } from '@ske/models';
import { NzAlertComponent } from 'ng-zorro-antd/alert';
import {
  NzFormControlComponent,
  NzFormDirective,
  NzFormItemComponent,
  NzFormLabelComponent
} from 'ng-zorro-antd/form';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzEmptyComponent } from 'ng-zorro-antd/empty';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzInputNumberComponent } from 'ng-zorro-antd/input-number';
import { NzRadioComponent, NzRadioGroupComponent } from 'ng-zorro-antd/radio';
import { NzSpinComponent } from 'ng-zorro-antd/spin';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';

@Component({
  imports: [
    FormField,
    NzAlertComponent,
    NzFormControlComponent,
    NzFormDirective,
    NzFormItemComponent,
    NzFormLabelComponent,
    NzButtonComponent,
    NzEmptyComponent,
    NzIconDirective,
    NzInputNumberComponent,
    NzRadioComponent,
    NzRadioGroupComponent,
    NzSpinComponent,
    NzSpaceComponent,
    NzSpaceItemDirective
  ],
  providers: [LocationDropdownStore],
  selector: 'ske-stock-move-form',
  styles: ``,
  templateUrl: './stock-move-form.html'
})
export class StockMoveForm {
  public readonly loading = input.required<boolean>();
  public readonly item = input.required<StockItemDto>();

  protected readonly locationStore = inject(LocationDropdownStore);

  // Same object references as the store list, so nz-radio-group matches by identity.
  protected readonly destinations = computed(() =>
    this.locationStore.locations().filter(location => location.id !== this.item().locationId));

  private readonly _formModel = linkedSignal({
    source: () => this.item(),
    computation: (item) => (<StockMoveFormModel>{
      destination: null,
      quantity: item.quantity > 0 ? 1 : 0
    })
  });

  private readonly _init = this.locationStore.load({
    filters: [],
    cursor: null,
    searchTerm: null,
    pageSize: PAGINATION_PAGE_SIZE,
    sort: [{ value: 'ascend', key: 'name' }]
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

  protected step(delta: number) {
    const quantity = this.stockMoveForm.quantity().value;
    quantity.set(Math.min(this.item().quantity, Math.max(1, (quantity() ?? 0) + delta)));
  }

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
