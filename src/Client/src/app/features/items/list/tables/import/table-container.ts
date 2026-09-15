import { Component, inject, output } from '@angular/core';
import { ItemImportBatchFileDto, ItemImportBatchListItemDto } from '@ske/models';
import { FileStorageState } from '@ske/shared/storage';
import { ItemImportState } from '@ske/shared/items';
import { LayoutBreakpoint } from '@ske/shared/directives';
import { ItemImportListSmall } from './small/item-import-list-small';
import { ItemImportTable } from './large/table';

@Component({
  imports: [ItemImportTable, ItemImportListSmall, LayoutBreakpoint],
  selector: 'ske-item-import-table-container',
  templateUrl: './table-container.html',
  providers: [FileStorageState],
})
export class ItemImportTableContainer {
  public readonly store = inject(ItemImportState);
  private readonly _fileStorage = inject(FileStorageState);
  public readonly review = output<ItemImportBatchListItemDto>();

  public download(file: ItemImportBatchFileDto) {
    this._fileStorage.downloadFile(file.fileMetadataId);
  }
}
