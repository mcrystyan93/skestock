import { DatePipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { BaseTable } from '@ske/shared/tables';
import {
  ITEM_IMPORT_STATUS_COLORS,
  ITEM_IMPORT_STATUS_LABELS,
  ITEM_IMPORT_TABLE_COLUMNS,
  GetAllItemImportsRequest,
  ItemImportListItemDto,
  ItemImportStatus
} from '@ske/models';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagComponent } from 'ng-zorro-antd/tag';

@Component({
  imports: [NzTableModule, DatePipe, NzTagComponent, NzButtonComponent, NzIconDirective],
  selector: 'ske-item-import-table',
  templateUrl: './table.html',
  host: { class: 'absolute block inset-0' }
})
export class ItemImportTable extends BaseTable<ItemImportListItemDto, GetAllItemImportsRequest> {
  public readonly loading = input.required<boolean>();
  public readonly columns = ITEM_IMPORT_TABLE_COLUMNS;
  public readonly downloadFile = output<ItemImportListItemDto>();
  public readonly review = output<ItemImportListItemDto>();

  public statusLabel(status: ItemImportStatus): string {
    return ITEM_IMPORT_STATUS_LABELS[status];
  }

  public statusColor(status: ItemImportStatus): string {
    return ITEM_IMPORT_STATUS_COLORS[status];
  }
}
