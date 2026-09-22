import { Component, computed, input, output } from '@angular/core';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { CategoryDto, StockItemCategoryGroup, StockItemDto } from '@ske/models';

interface StockCategoryGroupView {
  readonly category: StockItemCategoryGroup;
  readonly items: StockItemDto[];
}

@Component({
  imports: [ScrollingModule],
  selector: 'ske-stock-category-cards',
  styles: ``,
  templateUrl: './stock-category-cards.html',
  host: {
    class: 'absolute block inset-0'
  }
})
export class StockCategoryCards {
  public readonly groupItems = input.required<Map<StockItemCategoryGroup, StockItemDto[]>>();
  public readonly loading = input.required<boolean>();

  public readonly adjust = output<StockItemDto>();
  public readonly move = output<StockItemDto>();
  public readonly removeExpired = output<StockItemDto>();
  public readonly visibilityChange = output<StockItemDto>();
  public readonly add = output<Partial<CategoryDto> | null>();

  public readonly categories = computed<StockCategoryGroupView[]>(() =>
    Array.from(this.groupItems(), ([category, items]) => ({ category, items }))
  );

  public readonly trackCategory = (_: number, group: StockCategoryGroupView): string =>
    group.category.categoryId;
}
