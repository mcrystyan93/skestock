import { Component, input, output } from '@angular/core';
import { BaseList } from '@ske/shared/tables';
import { GetAllItemsRequest, ItemDto } from '@ske/models';
import {
  NzListComponent,
  NzListEmptyComponent,
  NzListItemActionComponent,
  NzListItemActionsComponent,
  NzListItemComponent,
  NzListItemMetaComponent,
  NzListItemMetaDescriptionComponent,
  NzListItemMetaTitleComponent,
} from 'ng-zorro-antd/list';
import { NzDividerComponent } from 'ng-zorro-antd/divider';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import {
  CdkFixedSizeVirtualScroll,
  CdkVirtualForOf,
  CdkVirtualScrollViewport,
} from '@angular/cdk/scrolling';

@Component({
  imports: [
    NzListComponent,
    NzListEmptyComponent,
    NzListItemActionComponent,
    NzListItemActionsComponent,
    NzListItemComponent,
    NzListItemMetaComponent,
    NzListItemMetaDescriptionComponent,
    NzListItemMetaTitleComponent,
    NzDividerComponent,
    NzIconDirective,
    NzButtonComponent,
    NzTagComponent,
    CdkFixedSizeVirtualScroll,
    CdkVirtualForOf,
    CdkVirtualScrollViewport,
  ],
  selector: 'ske-item-list-small',
  templateUrl: './item-list-small.html',
  host: {
    class: 'absolute block inset-0',
  },
})
export class ItemListSmall extends BaseList<ItemDto, GetAllItemsRequest> {
  public readonly loading = input.required<boolean>();
  public readonly onEdit = output<ItemDto>();
}
