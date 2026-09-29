import { Component, input, output } from '@angular/core';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { CHECKABLE_CARD_CLASSES } from './card-classes';
import type { OrderListSource } from '@ske/shared/order-lists/services';

type SourceOption = {
  value: OrderListSource;
  icon: string;
  title: string;
  description: string;
};

@Component({
  imports: [NzIconDirective],
  selector: 'ske-order-list-source-step',
  template: `
    <fieldset class="m-0 border-0 p-0">
      <legend class="mb-3 text-base font-medium">Cum vrei să creezi comanda?</legend>
      <div class="grid grid-cols-1 gap-3 md:grid-cols-2">
        @for (option of options; track option.value) {
          <label class="group flex items-start gap-3 p-4"
                 [class]="cardClasses">
            <input type="radio"
                   name="order-list-source"
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
export class OrderListSourceStep {
  public readonly source = input<OrderListSource | null>(null);
  public readonly sourceChange = output<OrderListSource>();

  protected readonly cardClasses = CHECKABLE_CARD_CLASSES;

  protected readonly options: SourceOption[] = [
    {
      value: 'new',
      icon: 'icons:triangle-exclamation',
      title: 'Comandă nouă',
      description: 'Pornește de la articolele cu stoc scăzut din clasă și adaugă ce mai ai nevoie.'
    },
    {
      value: 'supplyList',
      icon: 'icons:clipboard-list',
      title: 'Listă predefinită',
      description: 'Folosește o listă de aprovizionare deja definită, cu cantitățile ei.'
    }
  ];
}
