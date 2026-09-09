import { Component, inject } from '@angular/core';
import { GetAllItemImportsRequest } from '@ske/models';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { ItemImportState } from '@ske/shared/items';
import { ItemImportFilterForm } from './item-import-filter-form';

@Component({
  imports: [NzCardComponent, ItemImportFilterForm],
  selector: 'ske-item-import-filter-container',
  templateUrl: './item-import-filter-container.html'
})
export class ItemImportFilterContainer {
  public readonly store = inject(ItemImportState);

  public onFilterChange(filter: GetAllItemImportsRequest) {
    this.store.load(filter);
  }
}
