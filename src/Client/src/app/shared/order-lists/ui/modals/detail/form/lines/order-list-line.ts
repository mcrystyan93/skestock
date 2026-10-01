import { Component, computed, input, output } from '@angular/core';
import { PurchaseStatisticDto } from '@ske/models';
import { FieldTree, FormField } from '@angular/forms/signals';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzFormControlComponent, NzFormItemComponent } from 'ng-zorro-antd/form';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzInputDirective, NzInputPrefixDirective, NzInputWrapperComponent } from 'ng-zorro-antd/input';
import { NzInputNumberComponent } from 'ng-zorro-antd/input-number';
import { NzSpaceCompactComponent } from 'ng-zorro-antd/space';
import { NzTooltipDirective } from 'ng-zorro-antd/tooltip';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';

const QUANTITY_FORMATTER = new Intl.NumberFormat('ro-RO', { maximumFractionDigits: 2 });
const CURRENCY_FORMATTER = new Intl.NumberFormat('ro-RO', { style: 'currency', currency: 'RON' });
const SHORT_DATE_FORMATTER = new Intl.DateTimeFormat('ro-RO', { day: 'numeric', month: 'short' });
const LONG_DATE_FORMATTER = new Intl.DateTimeFormat('ro-RO', { dateStyle: 'long' });

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
    NzInputPrefixDirective,
    NzTooltipDirective,
    NzTypographyComponent
  ],
  host: {
    class: 'ant-list-item flex-col! items-stretch! gap-2 py-3!'
  },
  selector: 'ske-order-list-line',
  styles: ``,
  template: `
    @let lineForm = line();
    @let productName = lineForm.productName().value();
    <div class="flex items-start gap-2">
      <div class="min-w-0 flex-1">
        <div class="font-medium wrap-break-word">{{ productName }}</div>
        @if (lineForm.productName().touched() && lineForm.productName().errors(); as errors) {
          @for (error of errors; track error.kind) {
            <div class="ant-form-item-explain-error"
                 role="alert">
              <nz-icon nzType="icons:circle-exclamation"></nz-icon>
              {{ error.message }}
            </div>
          }
        }
        @if (historyHint(); as hint) {
          <span class="text-xs"
                nz-typography
                nzType="secondary"
                tabindex="0"
                nz-tooltip
                [nzTooltipTitle]="historyTooltip()"
                [attr.aria-label]="historyTooltip()">{{ hint }}</span>
        }
      </div>
      <button type="button"
              nz-button
              nzType="text"
              nzSize="small"
              nzDanger
              class="shrink-0"
              [disabled]="disabled()"
              [attr.aria-label]="'Șterge articolul ' + productName"
              (click)="remove.emit()">
        <nz-icon nzType="icons:trash-can"></nz-icon>
      </button>
    </div>

    <div class="flex flex-col gap-2 sm:flex-row sm:items-start">
      <nz-form-item class="mb-0! sm:flex-1">
        <nz-form-control [nzErrorTip]="notesErrorTemplate">
          <nz-input-wrapper>
            <nz-icon nzInputPrefix
                     nzType="icons:pencil"></nz-icon>
            <input type="text"
                   nz-input
                   placeholder="Adaugă notițe"
                   [formField]="lineForm.notes"
                   [attr.aria-label]="'Notițe pentru ' + productName" />
          </nz-input-wrapper>
        </nz-form-control>
      </nz-form-item>

      <nz-form-item class="mb-0!">
        <nz-form-control>
          <nz-space-compact class="flex! w-full">
            <button nz-button
                    nzType="default"
                    type="button"
                    [disabled]="disabled()"
                    (click)="decreaseQuantity()"
                    [attr.aria-label]="'Scade cantitatea pentru ' + productName">
              <nz-icon nzType="icons:minus"></nz-icon>
            </button>
            <nz-input-number [formField]="lineForm.quantity"
                             class="min-w-0 flex-1 sm:w-20 sm:flex-none"
                             nzPlaceHolder="Cantitate"
                             [nzMin]="0"
                             [nzStep]="0.01"
                             [nzControls]="false"
                             [attr.aria-label]="'Cantitate pentru ' + productName">
            </nz-input-number>
            <button nz-button
                    nzType="default"
                    type="button"
                    [disabled]="disabled()"
                    (click)="increaseQuantity()"
                    [attr.aria-label]="'Crește cantitatea pentru ' + productName">
              <nz-icon nzType="icons:plus"></nz-icon>
            </button>
            <input nz-input
                   type="text"
                   placeholder="U.M."
                   [formField]="lineForm.unit"
                   class="w-24! shrink-0"
                   [attr.aria-label]="'Unitate de măsură pentru ' + productName" />
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
    </div>

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
  // Last-365-days purchase history of the line's catalog item; null for free-text lines.
  public readonly history = input<PurchaseStatisticDto | null>(null);

  public readonly historyHint = computed(() => {
    const history = this.history();
    if (!history) {
      return null;
    }

    return `Cumpărat de ${history.purchaseCount}× · medie ` +
      `${QUANTITY_FORMATTER.format(history.averageQuantity)} ${history.unit} · ` +
      `ultima: ${SHORT_DATE_FORMATTER.format(new Date(history.lastPurchasedAt))}`;
  });

  public readonly historyTooltip = computed(() => {
    const history = this.history();
    if (!history) {
      return '';
    }

    return `În ultimele 365 de zile: ${history.purchaseCount} achiziții, ` +
      `${QUANTITY_FORMATTER.format(history.totalQuantity)} ${history.unit} în total ` +
      `(${CURRENCY_FORMATTER.format(history.totalValue)}), preț mediu ` +
      `${CURRENCY_FORMATTER.format(history.averageUnitPrice)}/${history.unit}. ` +
      `Ultima achiziție: ${LONG_DATE_FORMATTER.format(new Date(history.lastPurchasedAt))}.`;
  });

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
