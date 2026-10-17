import { Component, input, output } from '@angular/core';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzTableModule } from 'ng-zorro-antd/table';
import { StockCountChange } from '../../../services/stock-count.store';

/** Desktop (>= md) rendering of the changed rows of the stock count. */
@Component({
  imports: [NzButtonComponent, NzTableModule],
  selector: 'ske-stock-count-changes-table',
  templateUrl: './changes-table.html',
  host: { class: 'block' }
})
export class ChangesTable {
  public readonly changes = input.required<StockCountChange[]>();

  public readonly changeSelect = output<string>();
}
