import { Component, input, output } from '@angular/core';
import { NzTableModule } from 'ng-zorro-antd/table';
import { STOCK_TABLE_COLUMNS, StockItemDto } from '@ske/models';
import { StockCategoryItemRow } from './stock-category-item-row';

@Component({
  imports: [
    NzTableModule,
    StockCategoryItemRow
  ],
  selector: 'ske-stock-category-items-table',
  templateUrl: './stock-category-items-table.html'
})
export class StockCategoryItemsTable {
  public readonly items = input.required<StockItemDto[]>();
  public readonly loading = input.required<boolean>();

  public readonly adjust = output<StockItemDto>();
  public readonly move = output<StockItemDto>();
  public readonly removeExpired = output<StockItemDto>();
  public readonly visibilityChange = output<StockItemDto>();

  public readonly columns = STOCK_TABLE_COLUMNS;
}
