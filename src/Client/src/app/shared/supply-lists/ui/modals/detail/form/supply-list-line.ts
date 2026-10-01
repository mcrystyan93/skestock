import { Component, input, output } from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzFormControlComponent, NzFormItemComponent } from 'ng-zorro-antd/form';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzInputDirective, NzInputPrefixDirective, NzInputWrapperComponent } from 'ng-zorro-antd/input';
import { NzInputNumberComponent } from 'ng-zorro-antd/input-number';
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
    NzSpaceCompactComponent,
    FormField,
    NzInputWrapperComponent,
    NzInputPrefixDirective
  ],
  host: {
    class: 'ant-list-item flex-col! items-stretch! gap-2 py-3!'
  },
  selector: 'ske-supply-list-line',
  template: `
    @let lineForm = line();
    @let itemName = lineForm.itemName().value();
    <div class="flex items-start gap-2">
      <div class="min-w-0 flex-1 font-medium wrap-break-word">{{ itemName }}</div>
      <button type="button"
              nz-button
              nzType="text"
              nzSize="small"
              class="shrink-0"
              [disabled]="disabled()"
              (click)="remove.emit()"
              [attr.aria-label]="'Elimină ' + itemName">
        <nz-icon nzType="icons:trash-can"></nz-icon>
      </button>
    </div>

    <div class="flex flex-col gap-2 sm:flex-row sm:items-start">
      <nz-form-item class="mb-0! sm:flex-1">
        <nz-form-control>
          <nz-input-wrapper>
            <nz-icon nzInputPrefix
                     nzType="icons:pencil"></nz-icon>
            <input type="text"
                   nz-input
                   placeholder="Adaugă observații"
                   [formField]="lineForm.notes"
                   [attr.aria-label]="'Observații pentru ' + itemName" />
          </nz-input-wrapper>
          @for (error of lineForm.notes().errors(); track error.kind) {
            <div class="ant-form-item-explain-error"
                 role="alert">{{ error.message }}</div>
          }
        </nz-form-control>
      </nz-form-item>

      <nz-form-item class="mb-0!">
        <nz-form-control>
          <nz-space-compact class="flex! w-full">
            <button nz-button
                    nzType="default"
                    type="button"
                    [disabled]="disabled()"
                    (click)="changeQuantity(-1)"
                    [attr.aria-label]="'Scade cantitatea pentru ' + itemName">
              <nz-icon nzType="icons:minus"></nz-icon>
            </button>
            <nz-input-number [formField]="lineForm.quantity"
                             class="min-w-0 flex-1 sm:w-20 sm:flex-none"
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
                   placeholder="U.M."
                   [formField]="lineForm.unit"
                   class="w-24! shrink-0"
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
    </div>
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
