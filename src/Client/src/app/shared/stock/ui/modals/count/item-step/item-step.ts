import { Component, input, output } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StockItemDto } from '@ske/models';
import { NzAlertComponent } from 'ng-zorro-antd/alert';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzInputNumberComponent } from 'ng-zorro-antd/input-number';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';

/** Step 2: one item of the walk - current quantity, counted-quantity stepper and the increase/decrease alert. */
@Component({
  imports: [
    FormsModule,
    DecimalPipe,
    NzAlertComponent,
    NzButtonComponent,
    NzIconDirective,
    NzInputNumberComponent,
    NzTypographyComponent
  ],
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

  public readonly countChange = output<number>();

  protected step(delta: number) {
    this.countChange.emit(Math.max(0, this.count() + delta));
  }
}
