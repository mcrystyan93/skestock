import { Component, inject, output } from '@angular/core';
import { ErrorAlert } from '@ske/shared/errors';
import { ItemImportListItemDto } from '@ske/models';
import { ItemImportState } from '@ske/shared/items';
import { ItemImportFilterContainer } from '../filter/item-import-filter-container';
import { ItemImportTableContainer } from '../tables/import/table-container';

@Component({
  imports: [ItemImportFilterContainer, ErrorAlert, ItemImportTableContainer],
  selector: 'ske-item-import-list-tab',
  templateUrl: './item-import-list-tab.html',
  host: { class: 'flex grow flex-col gap-2 absolute inset-0' }
})
export class ItemImportListTab {
  public readonly store = inject(ItemImportState);
  public readonly review = output<ItemImportListItemDto>();
}
