import { computed, Directive, input, output } from '@angular/core';
import { injectClassReadOnly } from '@ske/shared/school-classes';
import { StockItemDto, StockItemGroup } from '@ske/models';

/** Inputs, outputs and tone shared by the desktop and tablet item rows. */
@Directive({
  host: {
    '[class]': 'rowClass()',
    '(click)': 'open.emit(group())'
  }
})
export abstract class StockItemRowBase {
  public readonly readOnly = injectClassReadOnly();
  public readonly group = input.required<StockItemGroup>();

  public readonly adjust = output<StockItemDto>();
  public readonly open = output<StockItemGroup>();
  public readonly move = output<StockItemDto>();
  public readonly removeExpired = output<StockItemDto>();
  public readonly extendExpiry = output<StockItemDto>();
  public readonly addToOrder = output<StockItemDto>();
  public readonly visibilityChange = output<StockItemDto>();

  public readonly isSingle = computed(() => this.group().locations.length === 1);
  public readonly expiredQuantity = computed(() =>
    this.group().locations.reduce((sum, l) => sum + l.expiredQuantity, 0));

  public readonly rowClass = computed(() => {
    const group = this.group();
    const tone = group.isExpired
      ? '[&>td]:!bg-red-500/15'
      : group.isLowStock
        ? '[&>td]:!bg-amber-500/10'
        : '';

    return this.readOnly() ? tone : `cursor-pointer ${tone}`;
  });
}
