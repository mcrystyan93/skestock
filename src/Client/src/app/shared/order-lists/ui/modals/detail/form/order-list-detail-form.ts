import { injectClassReadOnly } from '@ske/shared/school-classes';
import { Component, computed, effect, inject, input, linkedSignal, untracked } from '@angular/core';
import { ItemAutocompleteValue, ORDER_LIST_STATUS_LABELS, OrderListDto, OrderListLineDto } from '@ske/models';
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
import { NzFormControlComponent, NzFormDirective, NzFormItemComponent } from 'ng-zorro-antd/form';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { ItemAutocomplete } from '@ske/shared/items';
import { NzDividerComponent } from 'ng-zorro-antd/divider';
import { NzMessageService } from 'ng-zorro-antd/message';
import { OrderListDetailsFields, validateOrderListDetails } from './details/order-list-details-fields';
import { OrderListLinesContainer } from './lines/order-list-lines-container';
import { isNil } from 'lodash-es';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { SkeletonInputLoaderDirective } from '@ske/shared/loader';
import type { OrderListLineFormModel } from './lines/order-list-line';

@Component({
  imports: [
    NzRowDirective,
    NzColDirective,
    FormField,
    NzFormControlComponent,
    NzIconDirective,
    NzFormDirective,
    NzFormItemComponent,
    OrderListDetailsFields,
    ItemAutocomplete,
    NzDividerComponent,
    OrderListLinesContainer,
    NzTypographyComponent,
    SkeletonInputLoaderDirective
  ],
  selector: 'ske-order-list-detail-form',
  host:{
    class: 'grow flex flex-col'
  },
  templateUrl: './order-list-detail-form.html'
})
export class OrderListDetailForm {
  public readonly loading = input.required<boolean>();
  public readonly orderList = input.required<Partial<OrderListDto>>();
  /** The wizard collects the name and note in its own step, so the editor hides them until the order is saved. */
  public readonly showDetails = input(true);
  private readonly _readOnly = injectClassReadOnly();
  public readonly editable = computed(() => {
    if (this._readOnly())
      return false;

    const status = this.orderList().status;
    return isNil(status) || status === 'Draft';
  });

  private readonly _nzMessageService = inject(NzMessageService);
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
    validateOrderListDetails(schemaPath);
    disabled(schemaPath.name, {
      when: () => this.loading() || !this.editable()
    });
    disabled(schemaPath.note, {
      when: () => this.loading() || !this.editable()
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
      const alreadyListed = !isNil(line.itemId)
        && this.orderListForm.lines().value().some((existing) => existing.itemId === line.itemId);

      if (alreadyListed) {
        this._nzMessageService.warning('Articolul este deja în listă.');
      } else {
        this.orderListForm.lines().value.update(lines => [{
          id: null,
          ...line,
          quantity: 1,
          notes: '',
          clientKey: this.createLineKey()
        }, ...lines]);
      }

      // reset line item
      this.orderListForm.lineItem().reset(null);
    });
  });

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
