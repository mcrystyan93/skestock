import { Component, computed, input, output } from '@angular/core';
import { CategoryDto, StockItemCategoryGroup, StockItemDto } from '@ske/models';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { StockCategoryItemsTable } from './stock-category-items-table';
import {NzTooltipDirective} from 'ng-zorro-antd/tooltip';

@Component({
  imports: [
    NzButtonComponent,
    NzCardComponent,
    NzIconDirective,
    NzTagComponent,
    NzTypographyComponent,
    StockCategoryItemsTable,
    NzTooltipDirective
  ],
  selector: 'ske-stock-category-card',
  templateUrl: './stock-category-card.html'
})
export class StockCategoryCard {
  public readonly category = input.required<StockItemCategoryGroup>();
  public readonly items = input.required<StockItemDto[]>();
  public readonly loading = input.required<boolean>();

  public readonly adjust = output<StockItemDto>();
  public readonly move = output<StockItemDto>();
  public readonly removeExpired = output<StockItemDto>();
  public readonly visibilityChange = output<StockItemDto>();
  public readonly add = output<Partial<CategoryDto> | null>();

  public readonly hasExpiredItems = computed(() => this.items().some(item => item.isExpired));

  public addStock(): void {
    const category = this.category();
    this.add.emit({ id: category.categoryId, name: category.categoryName });
  }
}
