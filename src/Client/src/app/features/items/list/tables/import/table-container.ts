import { Component, inject, output } from '@angular/core';
import { ItemImportBatchFileDto, ItemImportBatchListItemDto } from '@ske/models';
import { FileStorageState } from '@ske/shared/storage';
import { ItemImportState } from '@ske/shared/items';
import { ItemImportTable } from './table';

@Component({
  imports: [ItemImportTable],
  selector: 'ske-item-import-table-container',
  templateUrl: './table-container.html',
  providers: [FileStorageState]
})
export class ItemImportTableContainer {
  public readonly store = inject(ItemImportState);
  private readonly _fileStorage = inject(FileStorageState);
  public readonly review = output<ItemImportBatchListItemDto>();

  public download(file: ItemImportBatchFileDto) {
    this._fileStorage.downloadFile(file.fileMetadataId);
  }
}
