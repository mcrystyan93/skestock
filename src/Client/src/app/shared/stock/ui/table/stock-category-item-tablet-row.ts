import { injectClassReadOnly } from '@ske/shared/school-classes';
import { Component, computed, input, output } from '@angular/core';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { StockItemDto, StockItemGroup } from '@ske/models';
import { TimeAgoPipe } from '@ske/shared/pipes';
import { StockItemActions } from './stock-item-actions';

/** Tablet layout of an item row: tags under the name, stacked location chips, one actions menu. */
@Component({
  imports: [NzIconDirective, NzTagComponent, NzTypographyComponent, StockItemActions, TimeAgoPipe],
  selector: 'tr[ske-stock-category-item-tablet-row]',
  templateUrl: './stock-category-item-tablet-row.html',
  host: {
    '[class]': 'rowClass()'
  }
})
export class StockCategoryItemTabletRow {
  public readonly readOnly = injectClassReadOnly();
  public readonly group = input.required<StockItemGroup>();

  public readonly adjust = output<StockItemDto>();
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

    return group.isExpired
      ? '[&>td]:!bg-red-500/15'
      : group.isLowStock
        ? '[&>td]:!bg-amber-500/10'
        : '';
  });
}
