import { Component, computed, input } from '@angular/core';
import { NzStepComponent, NzStepsComponent } from 'ng-zorro-antd/steps';
import { StockCountStep } from '../../../services/stock-count.store';

const STEP_ORDER: readonly StockCountStep[] = ['locations', 'items', 'summary'];

/** Progress bar of the stock count wizard (Locații -> Articole -> Rezumat). */
@Component({
  imports: [NzStepsComponent, NzStepComponent],
  selector: 'ske-stock-count-steps',
  template: `
    <nz-steps class="mb-4! [&_.ant-steps-item-content]:w-20! [&_.ant-steps-item-icon]:ms-7! [&_.ant-steps-item-tail]:ms-10!"
              nzSize="small"
              nzLabelPlacement="vertical"
              [nzCurrent]="index()">
      <nz-step nzTitle="Locații" />
      <nz-step nzTitle="Articole" />
      <nz-step nzTitle="Rezumat" />
    </nz-steps>
  `
})
export class StockCountSteps {
  public readonly step = input.required<StockCountStep>();

  protected readonly index = computed(() => STEP_ORDER.indexOf(this.step()));
}
