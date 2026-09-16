import { Component, input, output } from '@angular/core';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { CategoryDto, STOCK_TABLE_COLUMNS, StockItemCategoryGroup, StockItemDto } from '@ske/models';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { isNil } from 'lodash-es';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { NzTooltipDirective } from 'ng-zorro-antd/tooltip';
import { NzPopconfirmDirective } from 'ng-zorro-antd/popconfirm';

/**
 * Renders the full stock report for a class in one shot - no cursor/`loadMore`, since
 * `GetClassLocationStockQuery` isn't paginated (see `withStockCollection`).
 */
@Component({
  imports: [
    NzTableModule,
    NzTagComponent,
    NzButtonComponent,
    NzIconDirective,
    NzCardComponent,
    NzTypographyComponent,
    NzTooltipDirective,
    NzPopconfirmDirective
  ],
  selector: 'ske-stock-table',
  styles: ``,
  templateUrl: './table.html',
  host: {
    class: 'absolute block inset-0'
  }
})
export class Table {
  public readonly groupItems = input.required<Map<StockItemCategoryGroup, StockItemDto[]>>();
  public readonly loading = input.required<boolean>();

  public readonly onAdjust = output<StockItemDto>();
  public readonly onMove = output<StockItemDto>();
  public readonly onRemoveExpired = output<StockItemDto>();
  public readonly onVisibilityChange = output<StockItemDto>();
  public readonly onAdd = output<Partial<CategoryDto> | null>();

  public readonly columns = STOCK_TABLE_COLUMNS;

  public categoryHasExpiredItems(items: StockItemDto[]): boolean {
    return items.some(item => item.isExpired);
  }

  public add(category: StockItemCategoryGroup | null = null) {
    this.onAdd.emit(!isNil(category) ? { id: category.categoryId, name: category.categoryName } : null);
  }
}
