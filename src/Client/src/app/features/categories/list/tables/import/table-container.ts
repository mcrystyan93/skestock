import { Component, inject } from '@angular/core';
import { CategoryImportListItemDto } from '@ske/models';
import { FileStorageState } from '@ske/shared/storage';
import { CategoryImportState } from '@ske/shared/categories';
import { Table } from './table';

@Component({
  imports: [
    Table
  ],
  selector: 'ske-category-import-table-container',
  templateUrl: './table-container.html',
  host: {
    class: 'absolute block inset-0'
  },
  providers: [FileStorageState]
})
export class TableContainer {
  public readonly store = inject(CategoryImportState);
  public readonly fileStorageState = inject(FileStorageState);

  public downloadFile(importDto: CategoryImportListItemDto) {
    this.fileStorageState.downloadFile(importDto.fileMetadataId);
  }
}
