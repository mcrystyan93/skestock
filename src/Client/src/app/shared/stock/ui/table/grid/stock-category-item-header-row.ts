import { injectClassReadOnly } from '@ske/shared/school-classes';
import { Component, computed, input, output } from '@angular/core';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzTableCellDirective } from 'ng-zorro-antd/table';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { StockItemCategoryGroup } from '@ske/models';
import { NzTagComponent } from 'ng-zorro-antd/tag';

@Component({
  imports: [
    NzButtonComponent,
    NzIconDirective,
    NzTableCellDirective,
    NzTypographyComponent,
    NzTagComponent
  ],
  selector: 'tr[ske-stock-category-item-header-row]',
  styles: ``,
  templateUrl: './stock-category-item-header-row.html'
})
export class StockCategoryItemHeaderRow {
  public readonly readOnly = injectClassReadOnly();
  public readonly category = input.required<StockItemCategoryGroup>();
  public readonly columnCount = input(5);

  public readonly add = output<StockItemCategoryGroup>();

  public readonly numberOfItems = computed(() => this.category().groups.length);
  public readonly numberOfLowStockItems = computed(() => this.category().groups.filter(g => g.isLowStock).length);
  public readonly numberOfExpiredItems = computed(() => this.category().groups.filter(g => g.isExpired).length);
}
