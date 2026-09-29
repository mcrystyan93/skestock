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

export type SupplyListLineFormModel = {
  itemId: string;
  itemName: string;
  quantity: number;
  unit: string;
  notes: string;
};

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
  selector: 'ske-supply-list-line',
  template: `
    @let lineForm = line();
    @let itemName = lineForm.itemName().value();
    <nz-list-item-meta>
      <nz-list-item-meta-title>{{ itemName }}</nz-list-item-meta-title>
      <nz-list-item-meta-description>
        <nz-form-item class="mb-0!">
          <nz-form-control>
            <nz-input-wrapper>
              <nz-icon nzInputPrefix
                       nzType="icons:pencil"></nz-icon>
              <input type="text"
                     nz-input
                     nzVariant="borderless"
                     placeholder="Adaugă observații"
                     [formField]="lineForm.notes"
                     class="w-75!"
                     [attr.aria-label]="'Observații pentru ' + itemName" />
            </nz-input-wrapper>
          </nz-form-control>
        </nz-form-item>
        @for (error of lineForm.notes().errors(); track error.kind) {
          <div class="ant-form-item-explain-error"
               role="alert">{{ error.message }}</div>
        }
      </nz-list-item-meta-description>
    </nz-list-item-meta>

    <nz-form-item class="mb-0!">
      <nz-form-control>
        <nz-space-compact>
          <button nz-button
                  nzType="default"
                  type="button"
                  [disabled]="disabled()"
                  (click)="changeQuantity(-1)"
                  [attr.aria-label]="'Scade cantitatea pentru ' + itemName">
            <nz-icon nzType="icons:minus"></nz-icon>
          </button>
          <nz-input-number [formField]="lineForm.quantity"
                           class="w-20!"
                           nzPlaceHolder="Cantitate"
                           [nzMin]="0"
                           [nzStep]="0.01"
                           [nzControls]="false"
                           [attr.aria-label]="'Cantitate pentru ' + itemName">
          </nz-input-number>
          <button nz-button
                  nzType="default"
                  type="button"
                  [disabled]="disabled()"
                  (click)="changeQuantity(1)"
                  [attr.aria-label]="'Crește cantitatea pentru ' + itemName">
            <nz-icon nzType="icons:plus"></nz-icon>
          </button>
          <input nz-input
                 type="text"
                 placeholder="Unitate de măsură"
                 [formField]="lineForm.unit"
                 class="w-30!"
                 [attr.aria-label]="'Unitate de măsură pentru ' + itemName" />
        </nz-space-compact>
        @if (lineForm.quantity().touched()) {
          @for (error of lineForm.quantity().errors(); track error.kind) {
            <div class="ant-form-item-explain-error"
                 role="alert">{{ error.message }}</div>
          }
        }
        @if (lineForm.unit().touched()) {
          @for (error of lineForm.unit().errors(); track error.kind) {
            <div class="ant-form-item-explain-error"
                 role="alert">{{ error.message }}</div>
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
                (click)="remove.emit()"
                [attr.aria-label]="'Elimină ' + itemName">
          <nz-icon nzType="icons:trash-can"></nz-icon>
        </button>
      </nz-list-item-action>
    </ul>
  `
})
export class SupplyListLine {
  public readonly line = input.required<FieldTree<SupplyListLineFormModel>>();
  public readonly disabled = input(false);
  public readonly remove = output<void>();

  protected changeQuantity(delta: number) {
    const quantity = this.line().quantity();
    const next = Math.max(1, Math.round(((quantity.value() ?? 0) + delta) * 100) / 100);

    quantity.value.set(next);
    quantity.markAsTouched();
  }
}
