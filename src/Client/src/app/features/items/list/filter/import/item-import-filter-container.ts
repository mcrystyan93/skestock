import { Component, inject } from '@angular/core';
import { GetAllItemImportBatchesRequest } from '@ske/models';
import { ItemImportState } from '@ske/shared/items';
import { ItemImportFilterForm } from './item-import-filter-form';

@Component({
  imports: [ItemImportFilterForm],
  selector: 'ske-item-import-filter-container',
  templateUrl: './item-import-filter-container.html',
  host: {
    class: 'px-4'
  }
})
export class ItemImportFilterContainer {
  public readonly store = inject(ItemImportState);

  public onFilterChange(filter: GetAllItemImportBatchesRequest) {
    this.store.load(filter);
  }
}
