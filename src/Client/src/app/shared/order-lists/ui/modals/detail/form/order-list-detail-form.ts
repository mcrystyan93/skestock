import { Component, effect, input, linkedSignal, untracked } from '@angular/core';
import { ItemAutocompleteValue, LowStockItemDto, OrderListDto, OrderListLineDto, OrderListStatus } from '@ske/models';
import { applyEach, form, FormField, maxLength, required, schema, submit, validate } from '@angular/forms/signals';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzFormControlComponent, NzFormDirective, NzFormItemComponent, NzFormLabelComponent } from 'ng-zorro-antd/form';
import { NzOptionComponent, NzSelectComponent } from 'ng-zorro-antd/select';
import { NzInputDirective, NzInputWrapperComponent, NzTextareaCountComponent } from 'ng-zorro-antd/input';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { ItemAutocomplete } from '@ske/shared/items';
import { NzDividerComponent } from 'ng-zorro-antd/divider';
import { OrderListLinesContainer } from './lines/order-list-lines-container';
import { isNil } from 'lodash-es';
import { OrderListLowStockItems } from './order-list-low-stock-items';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';

@Component({
  imports: [
    NzRowDirective,
    NzColDirective,
    NzFormLabelComponent,
    NzSelectComponent,
    FormField,
    NzOptionComponent,
    NzInputDirective,
    NzFormControlComponent,
    NzInputWrapperComponent,
    NzIconDirective,
    NzFormDirective,
    NzFormItemComponent,
    NzTextareaCountComponent,
    ItemAutocomplete,
    NzDividerComponent,
    OrderListLinesContainer,
    OrderListLowStockItems,
    NzTypographyComponent
  ],
  selector: 'ske-order-list-detail-form',
  styles: ``,
  templateUrl: './order-list-detail-form.html'
})
export class OrderListDetailForm {
  public readonly loading = input.required<boolean>();
  public readonly orderList = input.required<Partial<OrderListDto>>();

  private readonly _formModel = linkedSignal({
    source: () => this.orderList(),
    computation: (orderList) => (<OrderListDetailFormModel>{
      id: orderList.id ?? null,
      name: orderList.name ?? '',
      note: orderList.note ?? '',
      status: orderList.status ?? 'Draft',
      lines: orderList.lines ?? [],
      lineItem: null
    })
  });

  private readonly lineSchemaPath = schema<OrderListLineDto>(schemaPath => {
    required(schemaPath.productName, {
      message: 'Numele articolului este obligatoriu.'
    });
    required(schemaPath.quantity, {
      message: 'Cantitatea este obligatorie.'
    });
    required(schemaPath.unit, {
      message: 'Unitatea este obligatorie.'
    });
  });

  public readonly orderListForm = form(this._formModel, (schemaPath) => {
    required(schemaPath.name, {
      message: 'Numele listei de comenzi este obligatoriu.'
    });
    maxLength(schemaPath.name, 200, {
      message: 'Numele listei de comenzi nu poate depăși 200 de caractere.'
    });
    maxLength(schemaPath.note, 1000, {
      message: 'Nota listei de comenzi nu poate depăși 1000 de caractere.'
    });
    required(schemaPath.status, {
      message: 'Statusul listei de comenzi este obligatoriu.'
    });
    applyEach(schemaPath.lines, this.lineSchemaPath);

    // at least one line is needed
    validate(schemaPath.lines, ({ valueOf }) => {
      if (valueOf(schemaPath.lines).length === 0) {
        return {
          kind: 'atLeastOneLine',
          message: 'Trebuie să adăugați cel puțin un articol în listă.'
        };
      }
      return null;
    });
  });

  private readonly lineItemChangeRef = effect(() => {
    const lineItem = this.orderListForm.lineItem().value();

    if (isNil(lineItem))
      return;

    const line = 'id' in lineItem
      ? {
        itemId: lineItem.id,
        productName: lineItem.sku ? `(${lineItem.sku}) ${lineItem.name ?? ''}` : lineItem.name ?? '',
        unit: lineItem.unit ?? 'buc'
      }
      : {
        itemId: null,
        productName: lineItem.name,
        unit: 'buc'
      };

    untracked(() => {
      // add line item to lines
      this.orderListForm.lines().value.update(lines => [{
        id: null,
        ...line,
        quantity: 1,
        notes: ''
      }, ...lines]);

      // reset line item
      this.orderListForm.lineItem().reset(null);
    });
  });

  public addLowStockItems(items: LowStockItemDto[]) {
    const lines = items.map(item => ({
      id: null,
      itemId: item.itemId,
      productName: item.sku ? `(${item.sku}) ${item.itemName}` : item.itemName,
      quantity: 1,
      unit: item.unit ?? 'buc',
      notes: ''
    }));

// add lines to the form. If an item already exists in the lines, we should not add it again
    this.orderListForm.lines().value.update(existingLines => {
      const existingItemIds = new Set(existingLines.map(line => line.itemId));
      const newLines = lines.filter(line => !existingItemIds.has(line.itemId));
      return [...newLines, ...existingLines];
    });
  }

  protected removeLine($event: { item: OrderListLineDto; index: number }) {
    this.orderListForm.lines().value.update(lines => {
      lines.splice($event.index, 1);
      return [...lines];
    });
  }

  public async submit(): Promise<OrderListDetailFormSubmit> {
    let formData: OrderListDetailFormModel | null = null;

    const isValid = await submit(this.orderListForm, async (_) => {
      formData = this.orderListForm().value();
    });

    return { isValid, formData };
  }
}

export type OrderListDetailFormModel = {
  id: string | null;
  name: string;
  note: string;
  status: OrderListStatus;
  lines: OrderListLineDto[];
  lineItem: ItemAutocompleteValue;
}
export type OrderListDetailFormSubmit = {
  isValid: boolean;
  formData: OrderListDetailFormModel | null;
};
