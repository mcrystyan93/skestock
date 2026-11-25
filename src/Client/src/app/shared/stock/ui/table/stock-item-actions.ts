import { Component, computed, input, output } from '@angular/core';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzDropdownDirective, NzDropdownMenuComponent } from 'ng-zorro-antd/dropdown';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzMenuDirective, NzMenuItemComponent, NzSubMenuComponent } from 'ng-zorro-antd/menu';
import { NzPopconfirmDirective } from 'ng-zorro-antd/popconfirm';
import { StockItemDto, StockItemGroup } from '@ske/models';
import { StopClick } from '@ske/shared/directives';

/** "⋯" menu with the item's stock actions; with several locations each action asks for the location. */
@Component({
  imports: [
    NzButtonComponent,
    NzDropdownDirective,
    NzDropdownMenuComponent,
    NzIconDirective,
    NzMenuDirective,
    NzMenuItemComponent,
    NzPopconfirmDirective,
    NzSubMenuComponent,
    StopClick
  ],
  selector: 'ske-stock-item-actions',
  templateUrl: './stock-item-actions.html'
})
export class StockItemActions {
  public readonly group = input.required<StockItemGroup>();

  public readonly move = output<StockItemDto>();
  public readonly removeExpired = output<StockItemDto>();
  public readonly extendExpiry = output<StockItemDto>();
  public readonly addToOrder = output<StockItemDto>();
  public readonly visibilityChange = output<StockItemDto>();

  public readonly isSingle = computed(() => this.group().locations.length === 1);
  public readonly movable = computed(() => this.group().locations.filter(l => l.quantity > 0));
  public readonly expired = computed(() => this.group().locations.filter(l => l.expiredQuantity > 0));

  public hideLabel(location: StockItemDto): string {
    return location.hideWhenZeroStock ? 'Nu mai ascunde la stoc 0' : 'Ascunde la stoc 0';
  }
}
