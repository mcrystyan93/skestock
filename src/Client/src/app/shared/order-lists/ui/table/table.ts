import { Component, input, output } from '@angular/core';
import { BaseTableWithFilter } from '@ske/shared/tables';
import {
  GetAllOrderListsRequest,
  ORDER_LIST_STATUS_COLORS,
  ORDER_LIST_STATUS_LABELS,
  ORDER_LIST_TABLE_COLUMNS,
  OrderListListItemDto,
  OrderListStatus,
  OrderListStatusAction,
  OrderListStatusChange
} from '@ske/models';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzDropdownDirective, NzDropdownMenuComponent } from 'ng-zorro-antd/dropdown';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzMenuDirective, NzMenuItemComponent } from 'ng-zorro-antd/menu';
import { NzTableModule } from 'ng-zorro-antd/table';
import { StopClick } from '@ske/shared/directives';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { TimeAgoPipe } from '@ske/shared/pipes';
import { NzTooltipDirective } from 'ng-zorro-antd/tooltip';

const ACTIONS_BY_STATUS: Record<OrderListStatus, OrderListStatusAction[]> = {
  All: [],
  Draft: ['submit', 'cancel'],
  Submitted: ['cancel'],
  Cancelled: ['reopen']
};

const ACTION_LABELS: Record<OrderListStatusAction, string> = {
  submit: 'Aprobă',
  cancel: 'Anulează',
  reopen: 'Redeschide ca ciornă'
};

@Component({
  imports: [
    NzButtonComponent,
    NzDropdownDirective,
    NzDropdownMenuComponent,
    NzIconDirective,
    NzMenuDirective,
    NzMenuItemComponent,
    NzTableModule,
    StopClick,
    NzTagComponent,
    NzTypographyComponent,
    TimeAgoPipe,
    NzTooltipDirective
  ],
  selector: 'ske-order-list-table',
  styles: ``,
  templateUrl: './table.html',
  host: {
    class: 'absolute block inset-0'
  }
})
export class Table extends BaseTableWithFilter<OrderListListItemDto, GetAllOrderListsRequest> {
  public readonly loading = input.required<boolean>();
  public readonly statusChangingId = input<string | null>(null);
  public readonly downloadingIds = input<ReadonlySet<string>>(new Set());

  public readonly onView = output<OrderListListItemDto>();
  public readonly onStatusChange = output<OrderListStatusChange>();
  public readonly onDownload = output<OrderListListItemDto>();
  public readonly columns = ORDER_LIST_TABLE_COLUMNS;

  constructor() {
    super();
  }

  public isDownloading(id: string): boolean {
    return this.downloadingIds().has(id);
  }

  public statusLabel(status: OrderListStatus): string {
    return ORDER_LIST_STATUS_LABELS[status];
  }

  public statusColor(status: OrderListStatus): string {
    return ORDER_LIST_STATUS_COLORS[status];
  }

  public availableActions(status: OrderListStatus): OrderListStatusAction[] {
    return ACTIONS_BY_STATUS[status];
  }

  public actionLabel(action: OrderListStatusAction): string {
    return ACTION_LABELS[action];
  }
}
