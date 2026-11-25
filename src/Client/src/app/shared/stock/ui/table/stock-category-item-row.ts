import { injectClassReadOnly } from '@ske/shared/school-classes';
import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, input, output } from '@angular/core';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzDropdownDirective, NzDropdownMenuComponent } from 'ng-zorro-antd/dropdown';
import { NzMenuDirective, NzMenuItemComponent, NzSubMenuComponent } from 'ng-zorro-antd/menu';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzPopconfirmDirective } from 'ng-zorro-antd/popconfirm';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzTooltipDirective } from 'ng-zorro-antd/tooltip';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { StockItemDto, StockItemGroup } from '@ske/models';
import { StopClick } from '@ske/shared/directives';
import { TimeAgoPipe } from '@ske/shared/pipes';

import { ItemThumbnail } from '@ske/shared/items';

/**
 * One item with all its locations. Item-level actions act on the only location,
 * or let the user pick the location when the item is stocked in several.
 */
@Component({
  imports: [
    ItemThumbnail,
    NzButtonComponent,
    NzDropdownDirective,
    NzDropdownMenuComponent,
    NzIconDirective,
    NzMenuDirective,
    NzMenuItemComponent,
    NzPopconfirmDirective,
    NzSubMenuComponent,
    NzTagComponent,
    NzTooltipDirective,
    NzTypographyComponent,
    NgTemplateOutlet,
    StopClick,
    TimeAgoPipe
  ],
  selector: 'tr[ske-stock-category-item-row]',
  templateUrl: './stock-category-item-row.html',
  host: {
    '[class]': 'rowClass()'
  }
})
export class StockCategoryItemRow {
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
  public readonly movable = computed(() => this.group().locations.filter(l => l.quantity > 0));
  public readonly expired = computed(() => this.group().locations.filter(l => l.expiredQuantity > 0));

  public readonly rowClass = computed(() => {
    const group = this.group();

    return group.isExpired
      ? '[&>td]:!bg-red-500/15'
      : group.isLowStock
        ? '[&>td]:!bg-amber-500/10'
        : '';
  });
}
