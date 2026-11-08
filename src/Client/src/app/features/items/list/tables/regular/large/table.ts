import { Component, computed, input, output } from '@angular/core';
import { BaseTableWithFilter } from '@ske/shared/tables';
import { GetAllItemsRequest, ITEM_TABLE_COLUMNS, ItemDto } from '@ske/models';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { TimeAgoPipe } from '@ske/shared/pipes';
import { ItemThumbnail } from '@ske/shared/items';

@Component({
  imports: [ItemThumbnail, NzTableModule, NzTypographyComponent, NzIconDirective, TimeAgoPipe],
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

  public readonly selectedIds = input<ReadonlySet<string>>(new Set());

  public readonly onToggleSelected = output<{ item: ItemDto; checked: boolean }>();
  public readonly onToggleAllSelected = output<boolean>();
  public readonly onEdit = output<ItemDto>();
  public readonly onToggleActive = output<ItemDto>();
  public readonly onImageUploaded = output<{ item: ItemDto; fileId: string }>();
  public readonly columns = ITEM_TABLE_COLUMNS;

  protected readonly selectedLoadedCount = computed(() =>
    this.items().filter((item) => this.selectedIds().has(item.id)).length
  );
  protected readonly allSelected = computed(() =>
    this.items().length > 0 && this.selectedLoadedCount() === this.items().length
  );
  protected readonly someSelected = computed(() => this.selectedLoadedCount() > 0 && !this.allSelected());

  constructor() {
    super();
  }
}
