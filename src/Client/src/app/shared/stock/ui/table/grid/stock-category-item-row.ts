import { Component, computed } from '@angular/core';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzDropdownDirective, NzDropdownMenuComponent } from 'ng-zorro-antd/dropdown';
import { NzMenuDirective, NzMenuItemComponent, NzSubMenuComponent } from 'ng-zorro-antd/menu';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzPopconfirmDirective } from 'ng-zorro-antd/popconfirm';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzTooltipDirective } from 'ng-zorro-antd/tooltip';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { StopClick } from '@ske/shared/directives';
import { TimeAgoPipe } from '@ske/shared/pipes';

import { ItemThumbnail } from '@ske/shared/items';
import { StockItemRowBase } from './stock-item-row-base';
import { StockLocationChip } from '../common/stock-location-chip';

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
    StockLocationChip,
    StopClick,
    TimeAgoPipe
  ],
  selector: 'tr[ske-stock-category-item-row]',
  templateUrl: './stock-category-item-row.html'
})
export class StockCategoryItemRow extends StockItemRowBase {
  public readonly movable = computed(() => this.group().locations.filter(l => l.quantity > 0));
  public readonly expired = computed(() => this.group().locations.filter(l => l.expiredQuantity > 0));
}
