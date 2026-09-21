import { Component, input, output } from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { OrderListLineDto } from '@ske/models';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzFormControlComponent, NzFormItemComponent } from 'ng-zorro-antd/form';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzInputDirective } from 'ng-zorro-antd/input';
import { NzInputNumberComponent } from 'ng-zorro-antd/input-number';
import {
  NzListItemActionComponent,
  NzListItemActionsComponent,
  NzListItemMetaComponent,
  NzListItemMetaDescriptionComponent,
  NzListItemMetaTitleComponent
} from 'ng-zorro-antd/list';
import { NzSpaceCompactComponent } from 'ng-zorro-antd/space';

@Component({
  imports: [
    NzButtonComponent,
    NzFormControlComponent,
    NzFormItemComponent,
    NzIconDirective,
    NzInputDirective,
    NzInputNumberComponent,
    NzListItemActionComponent,
    NzListItemActionsComponent,
    NzListItemMetaComponent,
    NzListItemMetaDescriptionComponent,
    NzListItemMetaTitleComponent,
    NzSpaceCompactComponent,
    FormField
  ],
  host: {
    class: 'ant-list-item'
  },
  selector: 'ske-order-list-line',
  styles: ``,
  template: `
    @let lineForm = line();
    <nz-list-item-meta>
      <nz-list-item-meta-title>{{ lineForm.productName().value() }}</nz-list-item-meta-title>
      <nz-list-item-meta-description>
        <nz-form-item class="mb-0!">
          <nz-form-control>
            <input type="text"
                   nz-input
                   nzVariant="borderless"
                   placeholder="Adaugă notițe"
                   [formField]="lineForm.notes"
                   class="w-75!" />
          </nz-form-control>
        </nz-form-item>
      </nz-list-item-meta-description>
    </nz-list-item-meta>

    <nz-form-item class="mb-0!">
      <nz-form-control>
        <nz-space-compact>
          <nz-input-number [formField]="lineForm.quantity"
                           class="w-20!"
                           nzVariant="borderless" />
          <input nz-input
                 type="text"
                 nzVariant="borderless"
                 [formField]="lineForm.unit"
                 class="w-32!" />
        </nz-space-compact>
      </nz-form-control>
    </nz-form-item>

    <ul nz-list-item-actions>
      <nz-list-item-action>
        <button type="button"
                nz-button
                nzType="link"
                nzDanger
                (click)="remove.emit(lineForm().value())">
          <nz-icon nzType="icons:trash-can"></nz-icon>
          Sterge
        </button>
      </nz-list-item-action>
    </ul>
  `
})
export class OrderListLine {
  public readonly line = input.required<FieldTree<OrderListLineDto>>();
  public readonly remove = output<OrderListLineDto>();
}
