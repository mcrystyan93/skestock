import { Component, computed, input } from '@angular/core';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { StockItemDto } from '@ske/models';
import { ItemThumbnail } from '@ske/shared/items';
import { TimeAgoPipe } from '@ske/shared/pipes';

/** Item stocked in several locations: shows the total; each location follows as its own row. */
@Component({
  imports: [ItemThumbnail, NzIconDirective, NzTagComponent, NzTypographyComponent, TimeAgoPipe],
  selector: 'tr[ske-stock-category-item-summary-row]',
  templateUrl: './stock-category-item-summary-row.html'
})
export class StockCategoryItemSummaryRow {
  public readonly item = input.required<StockItemDto>();
  public readonly locations = input.required<StockItemDto[]>();

  public readonly totalQuantity = computed(() => this.locations().reduce((sum, l) => sum + l.quantity, 0));
}
