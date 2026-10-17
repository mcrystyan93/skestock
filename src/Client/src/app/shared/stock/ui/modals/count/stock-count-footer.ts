import { Component, input, output } from '@angular/core';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { StockCountStep } from '../../../services/stock-count.store';

/** Step-dependent action buttons of the stock count wizard. */
@Component({
  imports: [NzButtonComponent],
  selector: 'ske-stock-count-footer',
  templateUrl: './stock-count-footer.html',
  host: { class: 'flex flex-col-reverse gap-2 sm:flex-row sm:items-center sm:justify-between' }
})
export class StockCountFooter {
  public readonly step = input.required<StockCountStep>();
  public readonly loading = input(false);
  /** Step 1: at least one row exists at the selected locations. */
  public readonly canStart = input(false);
  /** Step 3: at least one row changed. */
  public readonly canSubmit = input(false);

  public readonly close = output<void>();
  public readonly previous = output<void>();
  public readonly next = output<void>();
  public readonly jumpToSummary = output<void>();
  public readonly submit = output<void>();
}
