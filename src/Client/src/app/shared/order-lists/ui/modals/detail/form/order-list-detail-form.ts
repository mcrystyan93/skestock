import { Component, computed, effect, input, linkedSignal, untracked } from '@angular/core';
import {
  ItemAutocompleteValue,
  LowStockItemDto,
  ORDER_LIST_STATUS_LABELS,
  OrderListDto,
  OrderListLineDto
} from '@ske/models';
import {
  applyEach,
  disabled,
  form,
  FormField,
  maxLength,
  required,
  schema,
  submit,
  validate
} from '@angular/forms/signals';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzFormControlComponent, NzFormDirective, NzFormItemComponent, NzFormLabelComponent } from 'ng-zorro-antd/form';
import { NzInputDirective, NzInputWrapperComponent, NzTextareaCountComponent } from 'ng-zorro-antd/input';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { ItemAutocomplete } from '@ske/shared/items';
import { NzDividerComponent } from 'ng-zorro-antd/divider';
import { OrderListLinesContainer } from './lines/order-list-lines-container';
import { isNil } from 'lodash-es';
import { OrderListLowStockItems } from './order-list-low-stock-items';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { SkeletonInputLoaderDirective } from '@ske/shared/loader';
import type { OrderListLineFormModel } from './lines/order-list-line';

@Component({
  imports: [
    NzRowDirective,
    NzColDirective,
    NzFormLabelComponent,
    FormField,
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
    NzTypographyComponent,
    SkeletonInputLoaderDirective
  ],
  selector: 'ske-order-list-detail-form',
  styles: ``,
  templateUrl: './order-list-detail-form.html'
})
export class OrderListDetailForm {
  public readonly loading = input.required<boolean>();
  public readonly classId = input<string | null>(null);
  public readonly orderList = input.required<Partial<OrderListDto>>();
  public readonly editable = computed(() => {
    const status = this.orderList().status;
    return isNil(status) || status === 'Draft';
  });

  private _lineKeySequence = 0;

  private readonly _formModel = linkedSignal({
    source: () => this.orderList(),
    computation: (orderList): OrderListDetailFormModel => ({
      id: orderList.id ?? null,
      name: orderList.name ?? '',
      note: orderList.note ?? '',
      lines: (orderList.lines ?? []).map((line, index) => this.toFormLine(line, index)),
      lineItem: null
    })
  });

  public readonly statusLabel = computed(() =>
    ORDER_LIST_STATUS_LABELS[this.orderList().status ?? 'Draft']
  );

  private readonly lineSchemaPath = schema<OrderListLineFormModel>(schemaPath => {
    validate(schemaPath.productName, ({ valueOf }) => {
      const itemId = valueOf(schemaPath.itemId);
      const productName = valueOf(schemaPath.productName);

      if (isNil(itemId) && (!productName || !productName.trim())) {
        return {
          kind: 'required',
          message: 'Numele articolului este obligatoriu.'
        };
      }

      return null;
    });
    maxLength(schemaPath.productName, 500, {
      message: 'Numele articolului nu poate depăși 500 de caractere.'
    });
    disabled(schemaPath.quantity, {
      when: () => this.loading() || !this.editable()
    });
    required(schemaPath.quantity, {
      message: 'Cantitatea este obligatorie.'
    });
    validate(schemaPath.quantity, ({ valueOf }) => {
      const quantity = valueOf(schemaPath.quantity);

      if (isNil(quantity))
        return null;

      if (typeof quantity !== 'number' || !Number.isFinite(quantity) || quantity <= 0) {
        return {
          kind: 'greaterThan',
          message: 'Cantitatea trebuie să fie mai mare decât 0.'
        };
      }

      return null;
    });
    required(schemaPath.unit, {
      message: 'Unitatea este obligatorie.'
    });
    disabled(schemaPath.unit, {
      when: () => this.loading() || !this.editable()
    });
    maxLength(schemaPath.unit, 50, {
      message: 'Unitatea nu poate depăși 50 de caractere.'
    });
    disabled(schemaPath.notes, {
      when: () => this.loading() || !this.editable()
    });
    maxLength(schemaPath.notes, 1000, {
      message: 'Notițele articolului nu pot depăși 1000 de caractere.'
    });
  });

  public readonly orderListForm = form(this._formModel, (schemaPath) => {
    required(schemaPath.name, {
      message: 'Numele listei de comenzi este obligatoriu.'
    });
    maxLength(schemaPath.name, 200, {
      message: 'Numele listei de comenzi nu poate depăși 200 de caractere.'
    });
    disabled(schemaPath.name, {
      when: () => this.loading() || !this.editable()
    });
    disabled(schemaPath.note, {
      when: () => this.loading() || !this.editable()
    });
    maxLength(schemaPath.note, 1000, {
      message: 'Nota listei de comenzi nu poate depăși 1000 de caractere.'
    });
    disabled(schemaPath.lineItem, {
      when: () => this.loading() || !this.editable()
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
        notes: '',
        clientKey: this.createLineKey()
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
      notes: '',
      clientKey: this.createLineKey()
    }));

    // Add lines to the form. If an item already exists in the lines, do not add it again.
    this.orderListForm.lines().value.update(existingLines => {
      const existingItemIds = new Set(existingLines.map(line => line.itemId));
      const newLines = lines.filter(line => !existingItemIds.has(line.itemId));
      return [...newLines, ...existingLines];
    });
  }

  protected removeLine(line: OrderListLineFormModel) {
    const lines = this.orderListForm.lines().value();

    this.orderListForm.lines().value.set(lines.filter((l) => l.clientKey !== line.clientKey));
  }

  public async submit(): Promise<OrderListDetailFormSubmit> {
    let formData: OrderListDetailFormModel | null = null;

    const isValid = await submit(this.orderListForm, async () => {
      formData = this.orderListForm().value();
    });

    return { isValid, formData };
  }

  private toFormLine(line: OrderListLineDto, index: number): OrderListLineFormModel {
    return {
      id: line.id ?? null,
      itemId: line.itemId ?? null,
      productName: line.productName ?? '',
      quantity: line.quantity ?? 0,
      unit: line.unit ?? '',
      notes: line.notes ?? '',
      clientKey: line.id ?? `loaded-${index}`
    };
  }

  private createLineKey(): string {
    this._lineKeySequence += 1;
    return `line-${this._lineKeySequence}`;
  }
}

export type OrderListDetailFormModel = {
  id: string | null;
  name: string;
  note: string;
  lines: OrderListLineFormModel[];
  lineItem: ItemAutocompleteValue;
}
export type OrderListDetailFormSubmit = {
  isValid: boolean;
  formData: OrderListDetailFormModel | null;
};
