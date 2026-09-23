import { Component, input, output } from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzFormControlComponent, NzFormItemComponent } from 'ng-zorro-antd/form';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzInputDirective, NzInputPrefixDirective, NzInputWrapperComponent } from 'ng-zorro-antd/input';
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
    FormField,
    NzInputWrapperComponent,
    NzInputPrefixDirective
  ],
  host: {
    class: 'ant-list-item py-1!'
  },
  selector: 'ske-order-list-line',
  styles: ``,
  template: `
    @let lineForm = line();
    @let productName = lineForm.productName().value();
    <nz-list-item-meta>
      <nz-list-item-meta-title>
        {{ productName }}
        @if (lineForm.productName().touched() && lineForm.productName().errors(); as errors) {
          @for (error of errors; track error.kind) {
            <div class="ant-form-item-explain-error"
                 role="alert">
              <nz-icon nzType="icons:circle-exclamation"></nz-icon>
              {{ error.message }}
            </div>
          }
        }
      </nz-list-item-meta-title>
      <nz-list-item-meta-description>
        <nz-form-item class="mb-0!">
          <nz-form-control [nzErrorTip]="notesErrorTemplate">
            <nz-input-wrapper>
              <nz-icon nzInputPrefix
                       nzType="icons:pencil"></nz-icon>
              <input type="text"
                     nz-input
                     nzVariant="borderless"
                     placeholder="Adaugă notițe"
                     [formField]="lineForm.notes"
                     class="w-75!"
                     [attr.aria-label]="'Notițe pentru ' + productName" />
            </nz-input-wrapper>
          </nz-form-control>
        </nz-form-item>
      </nz-list-item-meta-description>
    </nz-list-item-meta>

    <nz-form-item class="mb-0!">
      <nz-form-control>
        <nz-space-compact>

          <button nz-button
                  nzType="default"
                  type="button"
                  (click)="decreaseQuantity()">
            <nz-icon nzType="icons:minus"></nz-icon>
          </button>
          <nz-input-number [formField]="lineForm.quantity"
                           class="w-20!"
                           nzPlaceHolder="Cantitate"
                           [nzMin]="0"
                           [nzStep]="0.01"
                           [nzControls]="false"
                           [attr.aria-label]="'Cantitate pentru ' + productName">
          </nz-input-number>
          <button nz-button
                  nzType="default"
                  type="button"
                  (click)="increaseQuantity()">
            <nz-icon nzType="icons:plus"></nz-icon>
          </button>
          <input nz-input
                 type="text"
                 placeholder="Unitate de măsură"
                 [formField]="lineForm.unit"
                 class="w-30!"
                 aria-label="Unitate de măsură" />
        </nz-space-compact>
        @if (lineForm.quantity().touched() && lineForm.quantity().errors(); as errors) {
          @for (error of errors; track error.kind) {
            <div class="ant-form-item-explain-error"
                 role="alert">
              <nz-icon nzType="icons:circle-exclamation"></nz-icon>
              {{ error.message }}
            </div>
          }
        }
        @if (lineForm.unit().touched() && lineForm.unit().errors(); as errors) {
          @for (error of errors; track error.kind) {
            <div class="ant-form-item-explain-error"
                 role="alert">
              <nz-icon nzType="icons:circle-exclamation"></nz-icon>
              {{ error.message }}
            </div>
          }
        }
      </nz-form-control>
    </nz-form-item>

    <ul nz-list-item-actions>
      <nz-list-item-action>
        <button type="button"
                nz-button
                nzType="text"
                [disabled]="disabled()"
                (click)="remove.emit()">
          <nz-icon nzType="icons:trash-can"></nz-icon>
        </button>
      </nz-list-item-action>
    </ul>

    <ng-template #notesErrorTemplate>
      @if (lineForm.notes().errors(); as errors) {
        @for (error of errors; track error.kind) {
          <div class="ant-form-item-explain-error">
            <nz-icon nzType="icons:circle-exclamation"></nz-icon>
            {{ error.message }}
          </div>
        }
      }
    </ng-template>
  `
})
export class OrderListLine {
  public readonly line = input.required<FieldTree<OrderListLineFormModel>>();
  public readonly disabled = input(false);
  public readonly remove = output<void>();

  public increaseQuantity() {
    this.line().quantity().value.update(qty => qty + 1);
  }

  public decreaseQuantity() {
    this.line().quantity().value.update(qty => Math.max(0, qty - 1));
  }
}

export type OrderListLineFormModel = {
  id: string | null;
  itemId: string | null;
  productName: string;
  quantity: number;
  unit: string;
  notes: string;
  clientKey: string;
};
