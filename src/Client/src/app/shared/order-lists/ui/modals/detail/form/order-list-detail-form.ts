import { Component, input, linkedSignal } from '@angular/core';
import { OrderListDto, OrderListLineDto, OrderListStatus } from '@ske/models';
import { applyEach, form, FormField, maxLength, required, schema } from '@angular/forms/signals';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzFormControlComponent, NzFormDirective, NzFormItemComponent, NzFormLabelComponent } from 'ng-zorro-antd/form';
import { NzOptionComponent, NzSelectComponent } from 'ng-zorro-antd/select';
import { NzInputDirective, NzInputWrapperComponent, NzTextareaCountComponent } from 'ng-zorro-antd/input';
import { NzIconDirective } from 'ng-zorro-antd/icon';

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
    NzTextareaCountComponent
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
      lines: orderList.lines ?? []
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
  });
}

export type OrderListDetailFormModel = {
  id: string | null;
  name: string;
  note: string;
  status: OrderListStatus;
  lines: OrderListLineDto[];
}
