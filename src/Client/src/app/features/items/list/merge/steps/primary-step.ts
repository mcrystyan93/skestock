import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ItemDto } from '@ske/models';
import { NzRadioComponent, NzRadioGroupComponent } from 'ng-zorro-antd/radio';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { CHECKABLE_CARD_CLASSES } from '../../../../../shared/ui/card-classes';
@Component({
  imports: [FormsModule, NzRadioComponent, NzRadioGroupComponent, NzTypographyComponent],
  selector: 'ske-item-merge-primary-step',
  template: `
    <p id="merge-primary-label"
       nz-typography
       nzType="secondary">
      Alege articolul care rămâne. Celelalte vor fi dezactivate, iar datele lor vor fi mutate pe articolul ales.
    </p>
    <nz-radio-group class="flex! flex-col gap-2"
                    nzName="primaryItem"
                    aria-labelledby="merge-primary-label"
                    [ngModel]="primaryId()"
                    (ngModelChange)="primaryIdChange.emit($event)">
      @for (item of items(); track item.id) {
        <label nz-radio
               [class]="cardClasses"
               [nzValue]="item.id"
               [nzDisabled]="!item.isActive">
          <span class="block font-medium">{{ item.name }}</span>
          <span class="block text-sm text-slate-500">
            {{ item.sku || 'fără cod' }} · {{ item.unit }} · {{ item.categoryName || '—' }}
            @if (!item.isActive) {
              · inactiv (nu poate fi principal)
            }
          </span>
        </label>
      }
    </nz-radio-group>
  `
})
export class ItemMergePrimaryStep {
  public readonly items = input.required<ItemDto[]>();
  public readonly primaryId = input<string | null>(null);
  public readonly primaryIdChange = output<string>();
  protected readonly cardClasses = `m-0! flex! items-start p-3! ${CHECKABLE_CARD_CLASSES}`;
}
