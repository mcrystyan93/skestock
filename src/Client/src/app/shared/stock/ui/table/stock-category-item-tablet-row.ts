import { Component } from '@angular/core';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { ItemThumbnail } from '@ske/shared/items';
import { TimeAgoPipe } from '@ske/shared/pipes';
import { StockItemActions } from './stock-item-actions';
import { StockItemRowBase } from './stock-item-row-base';
import { StockLocationChip } from './stock-location-chip';

/** Tablet layout of an item row: tags under the name, stacked location chips, one actions menu. */
@Component({
  imports: [ItemThumbnail, NzIconDirective, NzTagComponent, NzTypographyComponent, StockItemActions, StockLocationChip, TimeAgoPipe],
  selector: 'tr[ske-stock-category-item-tablet-row]',
  templateUrl: './stock-category-item-tablet-row.html'
})
export class StockCategoryItemTabletRow extends StockItemRowBase {}
