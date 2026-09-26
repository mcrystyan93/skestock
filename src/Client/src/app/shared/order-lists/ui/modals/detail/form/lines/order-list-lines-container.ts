import { Component, computed, inject, input, output } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FieldTree } from '@angular/forms/signals';
import { NzListComponent, NzListEmptyComponent } from 'ng-zorro-antd/list';
import { PurchaseStatisticDto } from '@ske/models';
import { ClassStatisticsHttp } from '@ske/shared/class-statistics';
import { catchError, map, of } from 'rxjs';
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
    @let history = purchaseHistory.value();
    <nz-list>
      @if (lines().length === 0) {
        <nz-list-empty />
      }
      @for (line of lines(); track $index; let index = $index) {
        @let itemId = line.itemId().value();
        <ske-order-list-line [line]="line"
                             [disabled]="disabled()"
                             [history]="itemId ? (history.get(itemId) ?? null) : null"
                             (remove)="remove.emit(line().value())" />
      }
    </nz-list>
  `
})
export class OrderListLinesContainer {
  public readonly lines = input.required<FieldTree<OrderListLineFormModel[]>>();
  public readonly disabled = input(false);
  public readonly remove = output<OrderListLineFormModel>();

  private readonly _classStatisticsHttp = inject(ClassStatisticsHttp);

  // A stable string key, so editing quantities or notes does not refetch the history.
  private readonly _itemIdsKey = computed(() =>
    [...new Set(
      this.lines()().value()
        .map((line) => line.itemId)
        .filter((itemId): itemId is string => !!itemId)
    )].sort().join(',')
  );

  // The hint is optional: a failed request simply shows no history.
  public readonly purchaseHistory = rxResource({
    params: () => this._itemIdsKey(),
    stream: ({ params }) => {
      if (!params) {
        return of(new Map<string, PurchaseStatisticDto>());
      }

      return this._classStatisticsHttp.getItemsPurchaseHistory(params.split(',')).pipe(
        map((history) => new Map(history.map((entry) => [entry.itemId, entry]))),
        catchError(() => of(new Map<string, PurchaseStatisticDto>()))
      );
    },
    defaultValue: new Map<string, PurchaseStatisticDto>()
  });
}
