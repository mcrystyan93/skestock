import { DatePipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { BaseTableWithFilter } from '@ske/shared/tables';
import {
  CATEGORY_IMPORT_BATCH_STATUS_COLORS,
  CATEGORY_IMPORT_BATCH_STATUS_LABELS,
  CATEGORY_IMPORT_BATCH_TABLE_COLUMNS,
  CategoryImportBatchFileDto,
  CategoryImportBatchListItemDto,
  CategoryImportBatchStatus,
  GetAllCategoryImportBatchesRequest
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
    NzIconDirective,
    NzDropdownDirective,
    NzDropdownMenuComponent,
    NzMenuDirective,
    NzMenuItemComponent,
    NzSpaceComponent,
    NzSpaceItemDirective,
    StopClick
  ],
  selector: 'ske-category-import-table',
  templateUrl: './table.html',
  host: {
    class: 'absolute block inset-0'
  }
})
export class Table extends BaseTableWithFilter<CategoryImportBatchListItemDto, GetAllCategoryImportBatchesRequest> {
  public readonly loading = input.required<boolean>();
  public readonly columns = CATEGORY_IMPORT_BATCH_TABLE_COLUMNS;
  public readonly downloadFile = output<CategoryImportBatchFileDto>();
  public readonly review = output<CategoryImportBatchListItemDto>();

  public statusLabel(status: CategoryImportBatchStatus): string {
    return CATEGORY_IMPORT_BATCH_STATUS_LABELS[status];
  }

  public statusColor(status: CategoryImportBatchStatus): string {
    return CATEGORY_IMPORT_BATCH_STATUS_COLORS[status];
  }

  public fileNames(categoryImport: CategoryImportBatchListItemDto): string {
    return categoryImport.files.map((file) => file.originalName).join(', ');
  }
}
