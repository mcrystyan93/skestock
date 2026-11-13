import { CurrencyPipe } from '@angular/common';
import { Component, computed, effect, inject, input, output, untracked } from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { getDateForShelfLife, ItemDto } from '@ske/models';
import { ItemDropdown } from '@ske/shared/items';
import { LocationDropdown } from '@ske/shared/locations';
import { ReviewEditableLine } from '../../../services/review.store';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzDatePickerComponent } from 'ng-zorro-antd/date-picker';
import { NzFormControlComponent, NzFormItemComponent } from 'ng-zorro-antd/form';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzInputAddonAfterDirective, NzInputAddonBeforeDirective } from 'ng-zorro-antd/input';
import { NzInputNumberComponent } from 'ng-zorro-antd/input-number';
import { NzTooltipDirective } from 'ng-zorro-antd/tooltip';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { isNil } from 'lodash-es';

const ITEM_DROPDOWN_PLACEHOLDER = 'Creează sau alege un produs';

@Component({
  imports: [
    FormField,
    ItemDropdown,
    LocationDropdown,
    NzButtonComponent,
    NzDatePickerComponent,
    NzFormControlComponent,
    NzFormItemComponent,
    NzIconDirective,
    NzInputAddonAfterDirective,
    NzInputAddonBeforeDirective,
    NzInputNumberComponent,
    NzTooltipDirective,
    NzTypographyComponent
  ],
  selector: 'ske-goods-import-review-line-small',
  templateUrl: './review-line-small.html',
  providers: [CurrencyPipe]
})
export class ReviewLineSmall {
  public readonly lineForm = input.required<FieldTree<ReviewEditableLine>>();
  public readonly line = input.required<ReviewEditableLine>();
  public readonly groupTotal = input<{ quantity: number; originalQuantity: number }>();

  public readonly splitLine = output<ReviewEditableLine>();
  public readonly removeLine = output<ReviewEditableLine>();

  private readonly _currencyPipe = inject(CurrencyPipe);

  private readonly _itemChangeEffectRef = effect(() => {
    const item = this.lineForm().item().value();

    const locationId = item?.locationId;

    if (locationId) {
      untracked(() => this.lineForm().location().value.set({ id: locationId }));
    }

    if (!item?.isPerishable || !item.shelfLifeDays) {
      return;
    }

    untracked(() => {
      this.lineForm().expiryDate().value.set(getDateForShelfLife(item.shelfLifeDays ?? 0));
    });
  });

  public readonly isQuantityOver = computed(() => {
    const totals = this.groupTotal();
    return !!totals && totals.quantity > totals.originalQuantity;
  });

  public readonly quantityValidateStatus = computed(() => {
    const quantityControl = this.lineForm().quantity();

    if (quantityControl.invalid()) {
      return 'error';
    }

    return this.isQuantityOver() ? 'warning' : 'success';
  });

  // nz-input-number re-formats on every keystroke, which would turn "16," into "16,00" and make 16,3 untypable:
  // keep the raw text while it still parses to the current value.
  private _typedPrice = '';

  public unitPriceFormatter = (value: number) =>
    this._typedPrice !== '' && parseUnitPrice(this._typedPrice) === value
      ? this._typedPrice
      : this._currencyPipe.transform(value, 'RON', '') ?? '0';

  public unitPriceParser = (value: string) => {
    this._typedPrice = value;
    return parseUnitPrice(value);
  };

  public createPrefill(line: ReviewEditableLine): Partial<ItemDto> {
    return {
      name: line.extractedName ?? '',
      sku: line.extractedSku ?? '',
      unit: line.extractedUnit ?? ''
    };
  }

  public split(): void {
    this.splitLine.emit(this.line());
  }

  public remove(): void {
    this.removeLine.emit(this.line());
  }

  public get itemPlaceholder(): string {
    return ITEM_DROPDOWN_PLACEHOLDER;
  }

  public get productName(): string {
    return this.line().item?.name ?? this.line().extractedName ?? 'Produs fără nume';
  }

  public get hasProduct(): boolean {
    return !isNil(this.line().item?.id);
  }

  public get missingItemSource(): string {
    return this.line().rawItemText ?? this.line().extractedName ?? '—';
  }

}

function parseUnitPrice(value: string) {
  return Number(value.replace(/[^0-9,-]+/g, '').replace(',', '.'));
}
