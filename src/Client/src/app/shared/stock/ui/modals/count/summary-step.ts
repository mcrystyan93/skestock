import { Component, input, output } from '@angular/core';
import { LayoutBreakpoint } from '@ske/shared/directives';
import { NzAlertComponent } from 'ng-zorro-antd/alert';
import { StockCountChange } from '../../../services/stock-count.store';
import { ChangesCards } from './changes-cards';
import { ChangesTable } from './changes-table';

/** Step 3: the changed rows (table from md, cards below) and the count of untouched rows. */
@Component({
  imports: [NzAlertComponent, LayoutBreakpoint, ChangesTable, ChangesCards],
  selector: 'ske-stock-count-summary-step',
  templateUrl: './summary-step.html',
  host: { class: 'block' }
})
export class SummaryStep {
  public readonly changes = input.required<StockCountChange[]>();
  public readonly unchangedCount = input.required<number>();

  public readonly changeSelect = output<string>();
}
