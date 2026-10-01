import { Component, computed, input, output } from '@angular/core';
import { CategoryDto, StockItemCategoryGroup, StockItemDto } from '@ske/models';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzEmptyComponent } from 'ng-zorro-antd/empty';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzDropdownDirective, NzDropdownMenuComponent } from 'ng-zorro-antd/dropdown';
import { NzMenuDirective, NzMenuItemComponent } from 'ng-zorro-antd/menu';
import { NzPopconfirmDirective } from 'ng-zorro-antd/popconfirm';
import { NzSkeletonComponent } from 'ng-zorro-antd/skeleton';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzTooltipDirective } from 'ng-zorro-antd/tooltip';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { TimeAgoPipe } from '@ske/shared/pipes';
import { StopClick } from '@ske/shared/directives';

@Component({
  imports: [
    NzButtonComponent,
    NzCardComponent,
    NzEmptyComponent,
    NzIconDirective,
    NzDropdownDirective,
    NzDropdownMenuComponent,
    NzMenuDirective,
    NzMenuItemComponent,
    NzPopconfirmDirective,
    NzSkeletonComponent,
    NzTagComponent,
    NzTooltipDirective,
    NzTypographyComponent,
    TimeAgoPipe,
    StopClick
  ],
  selector: 'ske-stock-category-items-small',
  templateUrl: './stock-category-items-small.html',
  host: {
    class: 'absolute inset-0 flex flex-col overflow-x-hidden overflow-y-auto px-3'
  }
})
export class StockCategoryItemsSmall {
  public readonly items = input.required<StockItemCategoryGroup[]>();
  public readonly loading = input.required<boolean>();

  public readonly add = output<Partial<CategoryDto> | null>();
  public readonly adjust = output<StockItemDto>();
  public readonly move = output<StockItemDto>();
  public readonly removeExpired = output<StockItemDto>();
  public readonly extendExpiry = output<StockItemDto>();
  public readonly addToOrder = output<StockItemDto>();
  public readonly visibilityChange = output<StockItemDto>();

  public readonly categories = computed(() => this.items().filter(group => group.isHeader));
  protected readonly skeletonPlaceholders = [0, 1, 2, 3];

  public addStock(category: StockItemCategoryGroup): void {
    this.add.emit({ id: category.categoryId, name: category.categoryName });
  }

  public cardToneClass(item: StockItemDto): string {
    return item.isLowStock ? '!bg-amber-500/10' : item.isExpired ? '!bg-red-500/15' : '';
  }

  public expiredItemsCount(category: StockItemCategoryGroup): number {
    return category.items.filter(item => item.isExpired).length;
  }

  public lowStockItemsCount(category: StockItemCategoryGroup): number {
    return category.items.filter(item => item.isLowStock).length;
  }

  public actionLabel(item: StockItemDto): string {
    return `Acțiuni pentru ${item.itemName}`;
  }

  public visibilityLabel(item: StockItemDto): string {
    const action = item.hideWhenZeroStock ? 'Nu mai ascunde la stoc 0' : 'Ascunde când stocul ajunge la 0';

    return `${action} pentru ${item.itemName} la ${item.locationName}`;
  }
}
