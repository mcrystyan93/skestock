import { injectClassReadOnly } from '@ske/shared/school-classes';
import { Component, computed, input, output } from '@angular/core';
import { CategoryDto, StockItemCategoryGroup, stockLocationLabel, StockItemDto, StockItemGroup } from '@ske/models';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzEmptyComponent } from 'ng-zorro-antd/empty';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzSkeletonComponent } from 'ng-zorro-antd/skeleton';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzTooltipDirective } from 'ng-zorro-antd/tooltip';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { ItemThumbnail } from '@ske/shared/items';
import { TimeAgoPipe } from '@ske/shared/pipes';
import { StockItemActions } from '../common/stock-item-actions';


interface StockCategoryView extends Pick<StockItemCategoryGroup, 'categoryId' | 'categoryName' | 'categoryIcon'> {
  readonly groups: StockItemGroup[];
  readonly expiredCount: number;
  readonly lowStockCount: number;
}

@Component({
  imports: [
    ItemThumbnail,
    NzButtonComponent,
    NzCardComponent,
    NzEmptyComponent,
    NzIconDirective,
    NzSkeletonComponent,
    NzTagComponent,
    NzTooltipDirective,
    NzTypographyComponent,
    TimeAgoPipe,
    StockItemActions
  ],
  selector: 'ske-stock-category-items-small',
  templateUrl: './stock-category-items-small.html',
  host: {
    class: 'absolute inset-0 flex flex-col overflow-x-hidden overflow-y-auto px-3'
  }
})
export class StockCategoryItemsSmall {
  public readonly readOnly = injectClassReadOnly();
  public readonly items = input.required<StockItemCategoryGroup[]>();
  public readonly loading = input.required<boolean>();

  public readonly add = output<Partial<CategoryDto> | null>();
  public readonly adjust = output<StockItemDto>();
  public readonly openItem = output<StockItemGroup>();
  public readonly move = output<StockItemDto>();
  public readonly removeExpired = output<StockItemDto>();
  public readonly extendExpiry = output<StockItemDto>();
  public readonly addToOrder = output<StockItemDto>();
  public readonly visibilityChange = output<StockItemDto>();

  public readonly categories = computed<StockCategoryView[]>(() =>
    this.items()
      .filter(group => group.isHeader)
      .map(({ categoryId, categoryName, categoryIcon, groups }) => ({
        categoryId,
        categoryName,
        categoryIcon,
        groups,
        expiredCount: groups.filter(g => g.isExpired).length,
        lowStockCount: groups.filter(g => g.isLowStock).length
      }))
  );
  protected readonly skeletonPlaceholders = [0, 1, 2, 3];

  public addStock(category: StockCategoryView): void {
    this.add.emit({ id: category.categoryId, name: category.categoryName });
  }

  protected readonly locationLabel = stockLocationLabel;

  public cardToneClass(group: StockItemGroup): string {
    return group.isExpired ? '!bg-red-500/15' : group.isLowStock ? '!bg-amber-500/10' : '';
  }
}
