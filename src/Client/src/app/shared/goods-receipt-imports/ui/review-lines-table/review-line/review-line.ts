import { Component, computed, effect, inject, input, output, untracked } from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
// noinspection ES6PreferShortImport
import { ReviewEditableLine } from '../../../services/review.store';
import { NzFormControlComponent, NzFormItemComponent } from 'ng-zorro-antd/form';
import { ItemDropdown } from '@ske/shared/items';
import { getDateForShelfLife, ItemDto } from '@ske/models';
import { LocationDropdown } from '@ske/shared/locations';
import { NzInputNumberComponent } from 'ng-zorro-antd/input-number';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzTooltipDirective } from 'ng-zorro-antd/tooltip';
import { isNil } from 'lodash-es';
import { NzInputAddonAfterDirective, NzInputAddonBeforeDirective } from 'ng-zorro-antd/input';
import { NzDatePickerComponent } from 'ng-zorro-antd/date-picker';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzColDirective } from 'ng-zorro-antd/grid';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { CurrencyPipe } from '@angular/common';

const ITEM_DROPDOWN_PLACEHOLDER = 'Creaza sau alege un produs';

@Component({
  imports: [
    NzFormItemComponent,
    NzFormControlComponent,
    ItemDropdown,
    FormField,
    LocationDropdown,
    NzInputNumberComponent,
    NzIconDirective,
    NzTooltipDirective,
    NzInputAddonBeforeDirective,
    NzDatePickerComponent,
    NzInputAddonAfterDirective,
    NzButtonComponent,
    NzColDirective,
    NzTypographyComponent
  ],
  selector: 'tr[ske-goods-import-review-line-row]',
  styles: ``,
  templateUrl: './review-line.html',
  providers: [CurrencyPipe],
  host: {
    '[class]': 'rowClass()'
  }
})
export class ReviewLine {
  public readonly lineForm = input.required<FieldTree<ReviewEditableLine>>();
  public readonly line = input.required<ReviewEditableLine>();
  public readonly groupTotal = input<{ quantity: number, originalQuantity: number }>();
  public readonly canRemove = input<boolean>();

  public readonly splitLine = output<ReviewEditableLine>();
  public readonly removeLine = output<ReviewEditableLine>();

  private readonly _currencyPipe = inject(CurrencyPipe);

  public readonly isQuantityOver = computed(() => {
    const totals = this.groupTotal();

    if (isNil(totals))
      return false;

    return totals.quantity > totals.originalQuantity;
  });

  public readonly quantityValidateStatus = computed(() => {
    const quantityControl = this.lineForm().quantity();

    if (quantityControl.invalid())
      return 'error';

    if (this.isQuantityOver())
      return 'warning';

    return 'success';
  });

  public readonly rowClass = computed(() => {
    const lineForm = this.lineForm();
    const itemValue = lineForm.item().value();

    return !itemValue?.id ? '[&>td]:!bg-amber-500/10' : '';
  });

  public readonly itemPlaceholder = ITEM_DROPDOWN_PLACEHOLDER;

  private readonly _itemChangeEffectRef = effect(() => {
    const item = this.lineForm().item().value();

    if (isNil(item))
      return;

    // when the item changes and it's perishable, we want to set the expiration based on shelfLifeDays
    if (!item.isPerishable || !item.shelfLifeDays)
      return;

    untracked(() => {
      this.lineForm().expiryDate().value.set(getDateForShelfLife(item.shelfLifeDays ?? 0));
    });
  });

  public unitPriceFormatter = (value: number) => this._currencyPipe.transform(value) ?? '0';

  public unitPriceParser = (value: string) => Number(value.replace(/[^0-9.-]+/g, ''));

  public createPrefill(line: ReviewEditableLine): Partial<ItemDto> {
    return {
      name: line.extractedName ?? '',
      sku: line.extractedSku ?? '',
      unit: line.extractedUnit ?? ''
    };
  }
}
