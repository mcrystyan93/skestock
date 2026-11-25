import { injectClassReadOnly } from '@ske/shared/school-classes';
import { Component, effect, input, output } from '@angular/core';
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
import { StockCategoryItemTabletRow } from './stock-category-item-tablet-row';

@Component({
  imports: [
    NzTableModule,
    StockCategoryItemHeaderRow,
    StockCategoryItemRow,
    StockCategoryItemTabletRow
  ],
  selector: 'ske-stock-category-items-table',
  templateUrl: './stock-category-items-table.html',
  host: {
    class: 'absolute block inset-0'
  }
})
export class StockCategoryItemsTable extends BaseTableWithFilter<StockItemCategoryGroup, BasePaginationFilter> {
  public readonly readOnly = injectClassReadOnly();
  public readonly loading = input.required<boolean>();
  /** Tablet layout: stacked rows with a single actions menu. */
  public readonly compact = input(false);

  public readonly add = output<Partial<CategoryDto> | null>();
  public readonly adjust = output<StockItemDto>();
  public readonly move = output<StockItemDto>();
  public readonly removeExpired = output<StockItemDto>();
  public readonly extendExpiry = output<StockItemDto>();
  public readonly addToOrder = output<StockItemDto>();
  public readonly visibilityChange = output<StockItemDto>();

  public readonly columns = STOCK_TABLE_COLUMNS;

  // Compact rows are taller (stacked location chips).
  private readonly _rowSizeEffect = effect(() => this.virtualItemSize.set(this.compact() ? 96 : 49));

  public addStock(category: StockItemCategoryGroup): void {
    this.add.emit({ id: category.categoryId, name: category.categoryName });
  }
}
