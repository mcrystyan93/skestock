import { Component, input, output } from '@angular/core';
import { FieldTree } from '@angular/forms/signals';
import { NzListComponent, NzListEmptyComponent } from 'ng-zorro-antd/list';
import { OrderListLine, type OrderListLineFormModel } from './order-list-line';

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
      @for (line of lines(); track line().value().clientKey; let index = $index) {
        <ske-order-list-line [line]="line"
                             [disabled]="disabled()"
                             (remove)="remove.emit(index)" />
      }
    </nz-list>
  `
})
export class OrderListLinesContainer {
  public readonly lines = input.required<FieldTree<OrderListLineFormModel[]>>();
  public readonly disabled = input(false);
  public readonly remove = output<number>();
}
