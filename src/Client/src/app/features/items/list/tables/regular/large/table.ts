import { Component, input, output } from '@angular/core';
import { BaseTableWithFilter } from '@ske/shared/tables';
import { GetAllItemsRequest, ITEM_TABLE_COLUMNS, ItemDto } from '@ske/models';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { TimeAgoPipe } from '@ske/shared/pipes';

@Component({
  imports: [NzTableModule, NzTypographyComponent, NzIconDirective, TimeAgoPipe],
  selector: 'ske-item-table',
  styles: ``,
  templateUrl: './table.html',
  host: {
    class: 'absolute block inset-0',
  },
})
export class Table extends BaseTableWithFilter<ItemDto, GetAllItemsRequest> {
  public readonly loading = input.required<boolean>();
  public readonly togglingItemId = input<string | null>(null);

  public readonly onEdit = output<ItemDto>();
  public readonly onToggleActive = output<ItemDto>();
  public readonly columns = ITEM_TABLE_COLUMNS;

  constructor() {
    super();
  }
}
