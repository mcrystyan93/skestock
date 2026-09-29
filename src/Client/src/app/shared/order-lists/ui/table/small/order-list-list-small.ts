import { Component, input, output } from '@angular/core';
import { BaseList } from '@ske/shared/tables';
import {
  GetAllOrderListsRequest,
  ORDER_LIST_STATUS_COLORS,
  ORDER_LIST_STATUS_LABELS,
  OrderListListItemDto,
  OrderListStatus,
  OrderListStatusAction,
  OrderListStatusChange
} from '@ske/models';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzDropdownDirective, NzDropdownMenuComponent } from 'ng-zorro-antd/dropdown';
import { NzEmptyComponent } from 'ng-zorro-antd/empty';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzMenuDirective, NzMenuItemComponent } from 'ng-zorro-antd/menu';
import { NzSkeletonComponent } from 'ng-zorro-antd/skeleton';
import { NzSpinComponent } from 'ng-zorro-antd/spin';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzTooltipDirective } from 'ng-zorro-antd/tooltip';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { TimeAgoPipe } from '@ske/shared/pipes';
import { StopClick } from '@ske/shared/directives';

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
    NzCardComponent,
    NzDropdownDirective,
    NzDropdownMenuComponent,
    NzEmptyComponent,
    NzIconDirective,
    NzMenuDirective,
    NzMenuItemComponent,
    NzSkeletonComponent,
    NzSpinComponent,
    NzTagComponent,
    NzTooltipDirective,
    NzTypographyComponent,
    TimeAgoPipe,
    StopClick
  ],
  selector: 'ske-order-list-list-small',
  templateUrl: './order-list-list-small.html',
  host: {
    class: 'absolute inset-0 flex flex-col overflow-hidden'
  }
})
export class OrderListListSmall extends BaseList<OrderListListItemDto, GetAllOrderListsRequest> {
  public readonly loading = input.required<boolean>();
  public readonly statusChangingId = input<string | null>(null);
  public readonly downloadingIds = input<ReadonlySet<string>>(new Set());

  public readonly onView = output<OrderListListItemDto>();
  public readonly onStatusChange = output<OrderListStatusChange>();
  public readonly onDownload = output<OrderListListItemDto>();

  protected readonly skeletonPlaceholders = [0, 1, 2, 3];

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
