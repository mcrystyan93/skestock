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
import { NzDropdownDirective, NzDropdownMenuComponent } from 'ng-zorro-antd/dropdown';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzEmptyComponent } from 'ng-zorro-antd/empty';
import { NzSkeletonComponent } from 'ng-zorro-antd/skeleton';
import { NzSpinComponent } from 'ng-zorro-antd/spin';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { NzMenuDirective, NzMenuItemComponent } from 'ng-zorro-antd/menu';
import { NzTagComponent } from 'ng-zorro-antd/tag';

@Component({
  imports: [
    DatePipe,
    CdkFixedSizeVirtualScroll,
    CdkVirtualForOf,
    CdkVirtualScrollViewport,
    NzButtonComponent,
    NzDropdownDirective,
    NzDropdownMenuComponent,
    NzIconDirective,
    NzCardComponent,
    NzEmptyComponent,
    NzSkeletonComponent,
    NzSpinComponent,
    NzTypographyComponent,
    NzMenuDirective,
    NzMenuItemComponent,
    NzTagComponent,
  ],
  selector: 'ske-item-import-list-small',
  templateUrl: './item-import-list-small.html',
  host: {
    class: 'absolute inset-0 flex flex-col',
  },
})
export class ItemImportListSmall extends BaseList<
  ItemImportBatchListItemDto,
  GetAllItemImportBatchesRequest
> {
  public readonly loading = input.required<boolean>();
  public readonly downloadFile = output<ItemImportBatchFileDto>();
  public readonly review = output<ItemImportBatchListItemDto>();

  protected readonly cardRowHeight = 128;
  protected readonly skeletonPlaceholders = [0, 1, 2, 3];

  protected readonly trackById = (_: number, item: ItemImportBatchListItemDto) => item.id;

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
