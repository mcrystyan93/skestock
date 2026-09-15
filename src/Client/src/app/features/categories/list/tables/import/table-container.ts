import { Component, inject, output } from '@angular/core';
import { CategoryImportBatchFileDto, CategoryImportBatchListItemDto } from '@ske/models';
import { FileStorageState } from '@ske/shared/storage';
import { CategoryImportState } from '@ske/shared/categories';
import { Table } from './large/table';
import { CategoryImportListSmall } from './small/category-import-list-small';
import { LayoutBreakpoint } from '@ske/shared/directives';

@Component({
  imports: [Table, CategoryImportListSmall, LayoutBreakpoint],
  selector: 'ske-category-import-table-container',
  templateUrl: './table-container.html',
  host: {
    class: 'absolute block inset-0',
  },
  providers: [FileStorageState],
})
export class TableContainer {
  public readonly store = inject(CategoryImportState);
  private readonly _fileStorage = inject(FileStorageState);
  public readonly review = output<CategoryImportBatchListItemDto>();

  public download(file: CategoryImportBatchFileDto) {
    this._fileStorage.downloadFile(file.fileMetadataId);
  }
}
