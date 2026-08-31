import { Component, input, output } from '@angular/core';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { STOCK_TABLE_COLUMNS, StockItemDto } from '@ske/models';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';

/**
 * Renders the full stock report for a class in one shot - no cursor/`loadMore`, since
 * `GetClassLocationStockQuery` isn't paginated (see `withStockCollection`).
 */
@Component({
  imports: [
    NzTableModule,
    NzTagComponent,
    NzButtonComponent,
    NzIconDirective
  ],
  selector: 'ske-stock-table',
  styles: ``,
  templateUrl: './table.html',
  host: {
    class: 'absolute block inset-0'
  }
})
export class Table {
  public readonly items = input.required<StockItemDto[]>();
  public readonly loading = input.required<boolean>();

  public readonly onAdjust = output<StockItemDto>();

  public readonly columns = STOCK_TABLE_COLUMNS;

  public readonly sortByItemName = (a: StockItemDto, b: StockItemDto) => a.itemName.localeCompare(b.itemName);
  public readonly sortByLocationName = (a: StockItemDto, b: StockItemDto) => a.locationName.localeCompare(b.locationName);
  public readonly sortByQuantity = (a: StockItemDto, b: StockItemDto) => a.quantity - b.quantity;

  public trackByRow(_index: number, item: StockItemDto): string {
    return `${item.itemId}-${item.locationId}`;
  }
}
