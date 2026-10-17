import { Component, input, output } from '@angular/core';
import { StockCountChange } from '../../../services/stock-count.store';

/** Mobile (< md) rendering of the changed rows of the stock count. */
@Component({
  selector: 'ske-stock-count-changes-cards',
  templateUrl: './changes-cards.html',
  host: { class: 'flex flex-col gap-2' }
})
export class ChangesCards {
  public readonly changes = input.required<StockCountChange[]>();

  public readonly changeSelect = output<string>();
}
