import { Component, computed, inject, input, output } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FieldTree } from '@angular/forms/signals';
import { PurchaseStatisticDto } from '@ske/models';
import { ClassStatisticsHttp } from '@ske/shared/class-statistics';
import { catchError, map, of } from 'rxjs';
import { OrderListLine, type OrderListLineFormModel } from './order-list-line';
import { NzEmptyComponent } from 'ng-zorro-antd/empty';

@Component({
  imports: [
    NzEmptyComponent,
    OrderListLine
  ],
  selector: 'ske-order-list-lines-container',
  host:{
    class: 'grow -mx-6 -mb-6 relative',
  },
  template: `
    <div class="absolute inset-0 overflow-y-auto">
      <div class="p-2 grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 xxxl:grid-cols-4 gap-2">
        @let history = purchaseHistory.value();
        @for (line of lines(); track $index; let index = $index) {
          @let itemId = line.itemId().value();
          <ske-order-list-line [line]="line"
                               [disabled]="disabled()"
                               [history]="itemId ? (history.get(itemId) ?? null) : null"
                               (remove)="remove.emit(line().value())" />
        } @empty {
          <nz-empty nzNotFoundContent="Adauga articole" />
        }
        <!--      <nz-list>-->
        <!--        @if (lines().length === 0) {-->
        <!--          <nz-list-empty />-->
        <!--        }-->
        <!--        @for (line of lines(); track $index; let index = $index) {-->
        <!--          @let itemId = line.itemId().value();-->
        <!--          <ske-order-list-line [line]="line"-->
        <!--                               [disabled]="disabled()"-->
        <!--                               [history]="itemId ? (history.get(itemId) ?? null) : null"-->
        <!--                               (remove)="remove.emit(line().value())" />-->
        <!--        }-->
        <!--      </nz-list>-->
      </div>
    </div>
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
