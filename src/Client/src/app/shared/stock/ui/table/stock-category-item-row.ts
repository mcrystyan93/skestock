import { Component, computed, input, output } from '@angular/core';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzDropdownDirective, NzDropdownMenuComponent } from 'ng-zorro-antd/dropdown';
import { NzMenuDirective, NzMenuItemComponent } from 'ng-zorro-antd/menu';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzPopconfirmDirective } from 'ng-zorro-antd/popconfirm';
import { NzTableCellDirective } from 'ng-zorro-antd/table';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzTooltipDirective } from 'ng-zorro-antd/tooltip';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { StockItemDto } from '@ske/models';
import { StopClick } from '@ske/shared/directives';
import { TimeAgoPipe } from '@ske/shared/pipes';

@Component({
  imports: [
    NzButtonComponent,
    NzDropdownDirective,
    NzDropdownMenuComponent,
    NzIconDirective,
    NzMenuDirective,
    NzMenuItemComponent,
    NzPopconfirmDirective,
    NzTableCellDirective,
    NzTagComponent,
    NzTooltipDirective,
    NzTypographyComponent,
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
  public readonly item = input.required<StockItemDto>();

  public readonly move = output<StockItemDto>();
  public readonly removeExpired = output<StockItemDto>();
  public readonly extendExpiry = output<StockItemDto>();
  public readonly addToOrder = output<StockItemDto>();
  public readonly visibilityChange = output<StockItemDto>();

  public readonly rowClass = computed(() => {
    const item = this.item();

    return item.isLowStock
      ? '[&>td]:!bg-amber-500/10'
      : item.isExpired
        ? '[&>td]:!bg-red-500/15'
        : '';
  });
}
