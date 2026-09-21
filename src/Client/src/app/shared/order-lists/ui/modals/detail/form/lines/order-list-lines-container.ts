import { Component, input, output } from '@angular/core';
import { FieldTree } from '@angular/forms/signals';
import { OrderListLineDto } from '@ske/models';
import { NzListComponent, NzListEmptyComponent } from 'ng-zorro-antd/list';
import { OrderListLine } from './order-list-line';

@Component({
  imports: [
    NzListComponent,
    NzListEmptyComponent,
    OrderListLine
  ],
  selector: 'ske-order-list-lines-container',
  styles: ``,
  template: `
    <nz-list>
      @if (lines().length === 0) {
        <nz-list-empty />
      }
      @for (line of lines(); track $index) {
        <ske-order-list-line [line]="line"
                             (remove)="remove.emit({item:$event, index:$index})" />
      }
    </nz-list>
  `
})
export class OrderListLinesContainer {
  public readonly lines = input.required<FieldTree<OrderListLineDto[]>>();
  public readonly remove = output<{ item: OrderListLineDto, index: number }>();
}
