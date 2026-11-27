import { injectClassReadOnly } from '@ske/shared/school-classes';
import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, effect, input, output, signal, untracked, viewChild } from '@angular/core';
import { CdkVirtualForOf, CdkVirtualScrollViewport } from '@angular/cdk/scrolling';
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
import { CdkDynamicSizeVirtualScroll, DynamicVirtualScrollItem } from '@ske/shared/virtual-scroll';
import { StockItemActions } from '../common/stock-item-actions';


interface StockCategoryView extends Pick<StockItemCategoryGroup, 'categoryId' | 'categoryName' | 'categoryIcon'> {
  readonly groups: StockItemGroup[];
  readonly expiredCount: number;
  readonly lowStockCount: number;
}

type StockRow =
  | { kind: 'header'; category: StockCategoryView }
  | { kind: 'item'; category: StockCategoryView; group: StockItemGroup };

@Component({
  imports: [
    CdkDynamicSizeVirtualScroll,
    CdkVirtualForOf,
    CdkVirtualScrollViewport,
    DynamicVirtualScrollItem,
    NgTemplateOutlet,
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
    class: 'absolute inset-0 flex flex-col overflow-hidden px-3'
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
  // Flat header/item rows so each can be measured and virtualised on its own (sticky headers are an overlay, see stickyCategory).
  protected readonly rows = computed<StockRow[]>(() =>
    this.categories().flatMap(category => [
      <StockRow>{ kind: 'header', category },
      ...category.groups.map(group => <StockRow>{ kind: 'item', category, group })
    ])
  );
  private readonly _viewport = viewChild(CdkVirtualScrollViewport);
  private readonly _dynamicScroll = viewChild(CdkDynamicSizeVirtualScroll);
  private readonly _top = signal({ index: 0, offset: 0 });

  // Keeps measured heights attached to their rows when the rows change. Also runs when the viewport is recreated (empty state -> list starts at scroll 0 without a scroll event) or the rows change under the scroll position.
  private readonly _refreshTopEffect = effect(() => {
    this._viewport();
    const rows = this.rows();
    untracked(() => {
      this._dynamicScroll()?.scrollStrategy.setKeys(rows.map(row => this.trackRow(0, row)));
      this.onScroll();
    });
  });

  // Native `position: sticky` cannot work inside the virtual-scroll content wrapper, so the category of the row at the top is overlaid once scrolled.
  protected readonly stickyCategory = computed(() => {
    const { index, offset } = this._top();

    return offset > 0 ? (this.rows()[index]?.category ?? null) : null;
  });
  protected readonly skeletonPlaceholders = [0, 1, 2, 3];

  protected onScroll(): void {
    const viewport = this._viewport();
    const dynamicScroll = this._dynamicScroll();

    if (!viewport || !dynamicScroll)
      return;

    const offset = viewport.measureScrollOffset();
    this._top.set({ index: dynamicScroll.scrollStrategy.indexAtOffset(offset), offset });
  }

  protected trackRow(_: number, row: StockRow): string {
    return row.kind === 'header' ? `h${row.category.categoryId}` : `i${row.group.itemId}`;
  }

  public addStock(category: StockCategoryView): void {
    this.add.emit({ id: category.categoryId, name: category.categoryName });
  }

  protected readonly locationLabel = stockLocationLabel;

  public cardToneClass(group: StockItemGroup): string {
    return group.isExpired ? '!bg-red-500/15' : group.isLowStock ? '!bg-amber-500/10' : '';
  }
}
