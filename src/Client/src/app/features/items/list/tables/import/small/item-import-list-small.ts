import { DatePipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { BaseList } from '@ske/shared/tables';
import {
  ITEM_IMPORT_BATCH_STATUS_COLORS,
  ITEM_IMPORT_BATCH_STATUS_LABELS,
  GetAllItemImportBatchesRequest,
  ItemImportBatchFileDto,
  ItemImportBatchListItemDto,
  ItemImportBatchStatus,
} from '@ske/models';
import {
  CdkFixedSizeVirtualScroll,
  CdkVirtualForOf,
  CdkVirtualScrollViewport,
} from '@angular/cdk/scrolling';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzDividerComponent } from 'ng-zorro-antd/divider';
import { NzDropdownDirective, NzDropdownMenuComponent } from 'ng-zorro-antd/dropdown';
import { NzIconDirective } from 'ng-zorro-antd/icon';
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
import { NzMenuDirective, NzMenuItemComponent } from 'ng-zorro-antd/menu';
import { NzTagComponent } from 'ng-zorro-antd/tag';

@Component({
  imports: [
    DatePipe,
    CdkFixedSizeVirtualScroll,
    CdkVirtualForOf,
    CdkVirtualScrollViewport,
    NzButtonComponent,
    NzDividerComponent,
    NzDropdownDirective,
    NzDropdownMenuComponent,
    NzIconDirective,
    NzListComponent,
    NzListEmptyComponent,
    NzListItemActionComponent,
    NzListItemActionsComponent,
    NzListItemComponent,
    NzListItemMetaComponent,
    NzListItemMetaDescriptionComponent,
    NzListItemMetaTitleComponent,
    NzMenuDirective,
    NzMenuItemComponent,
    NzTagComponent,
  ],
  selector: 'ske-item-import-list-small',
  templateUrl: './item-import-list-small.html',
  host: {
    class: 'absolute block inset-0',
  },
})
export class ItemImportListSmall extends BaseList<
  ItemImportBatchListItemDto,
  GetAllItemImportBatchesRequest
> {
  public readonly loading = input.required<boolean>();
  public readonly downloadFile = output<ItemImportBatchFileDto>();
  public readonly review = output<ItemImportBatchListItemDto>();

  public statusLabel(status: ItemImportBatchStatus): string {
    return ITEM_IMPORT_BATCH_STATUS_LABELS[status];
  }

  public statusColor(status: ItemImportBatchStatus): string {
    return ITEM_IMPORT_BATCH_STATUS_COLORS[status];
  }

  public fileNames(itemImport: ItemImportBatchListItemDto): string {
    return itemImport.files.map((file) => file.originalName).join(', ');
  }
}
