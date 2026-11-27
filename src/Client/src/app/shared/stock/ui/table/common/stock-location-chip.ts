import { Component, input, output } from '@angular/core';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { StockItemDto, stockLocationLabel } from '@ske/models';
import { StopClick } from '@ske/shared/directives';

/** One location of an item as a button: name, quantity (when the item has several) and its flags. */
@Component({
  imports: [NzIconDirective, NzTagComponent, StopClick],
  selector: 'ske-stock-location-chip',
  templateUrl: './stock-location-chip.html',
  host: {
    class: 'inline-flex max-w-full'
  }
})
export class StockLocationChip {
  public readonly location = input.required<StockItemDto>();
  public readonly showQuantity = input(false);
  public readonly disabled = input(false);

  public readonly adjust = output<StockItemDto>();

  protected readonly label = stockLocationLabel;
}
