import { Component, input, output } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StockItemDto } from '@ske/models';
import { NzAlertComponent } from 'ng-zorro-antd/alert';
import { NzInputNumberComponent } from 'ng-zorro-antd/input-number';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';

/** Step 2: one item of the walk - current quantity, counted-quantity input and the increase/decrease alert. */
@Component({
  imports: [FormsModule, DecimalPipe, NzAlertComponent, NzInputNumberComponent, NzTypographyComponent],
  selector: 'ske-stock-count-item-step',
  templateUrl: './item-step.html',
  host: { class: 'block' }
})
export class ItemStep {
  public readonly row = input.required<StockItemDto>();
  /** 1-based position of the row within the walk. */
  public readonly position = input.required<number>();
  public readonly total = input.required<number>();
  public readonly count = input.required<number>();

  public readonly countChange = output<number | null>();
}
