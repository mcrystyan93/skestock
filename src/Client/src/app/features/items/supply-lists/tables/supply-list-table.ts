import { Component, input, output } from '@angular/core';
import { BaseTableWithFilter } from '@ske/shared/tables';
import {
  formatSupplyListFrequency,
  GetAllSupplyListsRequest,
  SUPPLY_LIST_TABLE_COLUMNS,
  SupplyListListItemDto
} from '@ske/models';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzTooltipDirective } from 'ng-zorro-antd/tooltip';
import { StopClick } from '@ske/shared/directives';
import { TimeAgoPipe } from '@ske/shared/pipes';

@Component({
  imports: [NzTableModule, NzButtonComponent, NzIconDirective, NzTagComponent, NzTooltipDirective, StopClick, TimeAgoPipe],
  selector: 'ske-supply-list-table',
  templateUrl: './supply-list-table.html',
  host: { class: 'absolute block inset-0' }
})
export class SupplyListTable extends BaseTableWithFilter<SupplyListListItemDto, GetAllSupplyListsRequest> {
  public readonly loading = input.required<boolean>();
  public readonly togglingId = input<string | null>(null);
  public readonly columns = SUPPLY_LIST_TABLE_COLUMNS;
  public readonly open = output<SupplyListListItemDto>();
  public readonly toggleActive = output<SupplyListListItemDto>();

  public frequencyLabel(supplyList: SupplyListListItemDto): string {
    return formatSupplyListFrequency(supplyList.frequency, supplyList.intervalWeeks);
  }
}
