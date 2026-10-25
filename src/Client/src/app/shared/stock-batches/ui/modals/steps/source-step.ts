import { Component, input, output } from '@angular/core';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import type { StockBatchSource } from '@ske/models';
import { CHECKABLE_CARD_CLASSES } from '../../../../order-lists/ui/modals/wizard/steps/card-classes';

type SourceOption = {
  value: StockBatchSource;
  icon: string;
  title: string;
  description: string;
};

@Component({
  imports: [NzIconDirective],
  selector: 'ske-stock-batch-source-step',
  template: `
    <fieldset class="m-0 border-0 p-0">
      <legend class="mb-3 text-base font-medium">De unde provin articolele?</legend>
      <div class="grid grid-cols-1 gap-3 md:grid-cols-2">
        @for (option of options; track option.value) {
          <label class="group flex items-start gap-3 p-4"
                 [class]="cardClasses">
            <input type="radio"
                   name="stock-batch-source"
                   class="sr-only"
                   [value]="option.value"
                   [checked]="source() === option.value"
                   (change)="sourceChange.emit(option.value)">
            <nz-icon [nzType]="option.icon"
                     class="mt-1 text-2xl"
                     aria-hidden="true" />
            <span class="flex flex-col gap-1">
              <span class="font-medium">{{ option.title }}</span>
              <span class="text-sm opacity-70">{{ option.description }}</span>
            </span>
            <nz-icon nzType="icons:circle-check"
                     class="invisible ml-auto text-xl text-primary-600 group-has-checked:visible dark:text-primary-400"
                     aria-hidden="true" />
          </label>
        }
      </div>
    </fieldset>
  `
})
export class StockBatchSourceStep {
  public readonly source = input<StockBatchSource | null>(null);
  public readonly sourceChange = output<StockBatchSource>();

  protected readonly cardClasses = CHECKABLE_CARD_CLASSES;

  protected readonly options: SourceOption[] = [
    {
      value: 'order',
      icon: 'icons:cart-shopping',
      title: 'Comandă',
      description: 'Articole achiziționate printr-o comandă.'
    },
    {
      value: 'donation',
      icon: 'icons:gift',
      title: 'Donație',
      description: 'Articole primite gratuit.'
    }
  ];
}
