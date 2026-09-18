import { DatePipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { BaseTableWithFilter } from '@ske/shared/tables';
import {
  GetAllItemImportBatchesRequest,
  ITEM_IMPORT_BATCH_STATUS_COLORS,
  ITEM_IMPORT_BATCH_STATUS_LABELS,
  ITEM_IMPORT_BATCH_TABLE_COLUMNS,
  ItemImportBatchFileDto,
  ItemImportBatchListItemDto,
  ItemImportBatchStatus
} from '@ske/models';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzDropdownDirective, NzDropdownMenuComponent } from 'ng-zorro-antd/dropdown';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzMenuDirective, NzMenuItemComponent } from 'ng-zorro-antd/menu';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { StopClick } from '@ske/shared/directives';

@Component({
  imports: [
    NzTableModule,
    DatePipe,
    NzTagComponent,
    NzButtonComponent,
    NzDropdownDirective,
    NzDropdownMenuComponent,
    NzIconDirective,
    NzMenuDirective,
    NzMenuItemComponent,
    NzSpaceComponent,
    NzSpaceItemDirective,
    StopClick
  ],
  selector: 'ske-item-import-table',
  templateUrl: './table.html',
  host: { class: 'absolute block inset-0' },
})
export class ItemImportTable extends BaseTableWithFilter<
  ItemImportBatchListItemDto,
  GetAllItemImportBatchesRequest
> {
  public readonly loading = input.required<boolean>();
  public readonly columns = ITEM_IMPORT_BATCH_TABLE_COLUMNS;
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
