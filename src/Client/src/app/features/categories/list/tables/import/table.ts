import { DatePipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { BaseTable } from '@ske/shared/tables';
import {
  CATEGORY_IMPORT_STATUS_COLORS,
  CATEGORY_IMPORT_STATUS_LABELS,
  CATEGORY_IMPORT_TABLE_COLUMNS,
  CategoryImportListItemDto,
  CategoryImportStatus,
  GetAllCategoryImportsRequest
} from '@ske/models';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagComponent } from 'ng-zorro-antd/tag';

@Component({
  imports: [
    NzTableModule,
    DatePipe,
    NzTagComponent,
    NzButtonComponent,
    NzIconDirective
  ],
  selector: 'ske-category-import-table',
  templateUrl: './table.html',
  host: {
    class: 'absolute block inset-0'
  }
})
export class Table extends BaseTable<CategoryImportListItemDto, GetAllCategoryImportsRequest> {
  public readonly loading = input.required<boolean>();
  public readonly columns = CATEGORY_IMPORT_TABLE_COLUMNS;
  public readonly downloadFile = output<CategoryImportListItemDto>();

  public statusLabel(status: CategoryImportStatus): string {
    return CATEGORY_IMPORT_STATUS_LABELS[status];
  }

  public statusColor(status: CategoryImportStatus): string {
    return CATEGORY_IMPORT_STATUS_COLORS[status];
  }
}
