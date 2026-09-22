import { Component, input, output } from '@angular/core';
import { NzTableModule } from 'ng-zorro-antd/table';
import {
  BasePaginationFilter,
  CategoryDto,
  STOCK_TABLE_COLUMNS,
  StockItemCategoryGroup,
  StockItemDto
} from '@ske/models';
import { BaseTableWithFilter } from '@ske/shared/tables';
import { StockCategoryItemHeaderRow } from './stock-category-item-header-row';
import { StockCategoryItemRow } from './stock-category-item-row';

@Component({
  imports: [
    NzTableModule,
    StockCategoryItemHeaderRow,
    StockCategoryItemRow
  ],
  selector: 'ske-stock-category-items-table',
  templateUrl: './stock-category-items-table.html',
  host: {
    class: 'absolute block inset-0'
  }
})
export class StockCategoryItemsTable extends BaseTableWithFilter<StockItemCategoryGroup, BasePaginationFilter> {
  public readonly loading = input.required<boolean>();

  public readonly add = output<Partial<CategoryDto> | null>();
  public readonly adjust = output<StockItemDto>();
  public readonly move = output<StockItemDto>();
  public readonly removeExpired = output<StockItemDto>();
  public readonly visibilityChange = output<StockItemDto>();

  public readonly columns = STOCK_TABLE_COLUMNS;

  public addStock(category: StockItemCategoryGroup): void {
    this.add.emit({ id: category.categoryId, name: category.categoryName });
  }
}
