import { Component, input, output } from '@angular/core';
import { BaseTable } from '@ske/shared/tables';
import { GetAllItemsRequest, ITEM_TABLE_COLUMNS, ItemDto } from '@ske/models';
import { NzTableModule } from 'ng-zorro-antd/table';
import { DatePipe } from '@angular/common';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzTagComponent } from 'ng-zorro-antd/tag';

@Component({
  imports: [
    DatePipe,
    NzButtonComponent,
    NzIconDirective,
    NzTagComponent,
    NzTableModule
  ],
  selector: 'ske-item-table',
  styles: ``,
  templateUrl: './table.html',
  host: {
    class: 'absolute block inset-0'
  }
})
export class Table extends BaseTable<ItemDto, GetAllItemsRequest> {
  public readonly loading = input.required<boolean>();
  public readonly togglingItemId = input<string | null>(null);

  public readonly onEdit = output<ItemDto>();
  public readonly onToggleActive = output<ItemDto>();
  public readonly columns = ITEM_TABLE_COLUMNS;

  constructor() {
    super();
  }
}
