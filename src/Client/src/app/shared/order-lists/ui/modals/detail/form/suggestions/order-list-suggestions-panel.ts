import { Component, computed, inject, input, output, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import type { OrderListLineDto } from '@ske/models';
import { ClassStatisticsHttp } from '@ske/shared/class-statistics';
import { groupLowStockItems, purchaseSegmentLists, suggestionsToLines, supplyListsToLines } from '@ske/shared/order-lists/services';
import { StockHttp } from '../../../../../../stock/services/stock.http';
import { SupplyListsHttp } from '@ske/shared/supply-lists';
import { catchError, forkJoin, map, of, switchMap } from 'rxjs';
import { NzAlertComponent } from 'ng-zorro-antd/alert';
import { NzSkeletonComponent } from 'ng-zorro-antd/skeleton';
import { NzTabComponent, NzTabsComponent } from 'ng-zorro-antd/tabs';
import { OrderListSuggestionGroups, type SuggestionGroup } from './order-list-suggestion-groups';

const SUPPLY_LISTS_TAB_INDEX = 2;

const FREQUENCY_TITLES = ['Săptămânal', 'La 2 săptămâni', 'Lunar', 'O dată pe clasă'];

@Component({
  imports: [
    NzAlertComponent,
    NzSkeletonComponent,
    NzTabComponent,
    NzTabsComponent,
    OrderListSuggestionGroups
  ],
  selector: 'ske-order-list-suggestions-panel',
  host: {
    class: 'relative min-h-0 -mb-6'
  },
  template: `
    <div class="absolute inset-0 flex flex-col pb-4">
    <nz-tabs nzSize="small"
             class="h-full [&_.ant-tabs-tab+.ant-tabs-tab]:ml-4! [&_.ant-tabs-content-holder]:min-h-0 [&_.ant-tabs-content-holder]:overflow-y-auto"
             (nzSelectedIndexChange)="onTabChange($event)">
      <nz-tab nzTitle="Stoc redus">
        @if (lowStock.isLoading()) {
          <nz-skeleton aria-busy="true"
                       [nzActive]="true"
                       [nzParagraph]="{ rows: 4 }" />
        } @else if (lowStock.error()) {
          <nz-alert nzType="error"
                    nzShowIcon
                    nzMessage="Nu am putut încărca produsele cu stoc redus." />
        } @else {
          <ske-order-list-suggestion-groups [groups]="lowStockGroups()"
                                              [addedItemIds]="addedItemIds()"
                                              emptyText="Nu există articole cu stoc scăzut."
                                              (add)="add.emit($event)" />
        }
      </nz-tab>
      <nz-tab nzTitle="După frecvență">
        @if (segments.isLoading()) {
          <nz-skeleton aria-busy="true"
                       [nzActive]="true"
                       [nzParagraph]="{ rows: 4 }" />
        } @else if (segments.error()) {
          <nz-alert nzType="error"
                    nzShowIcon
                    nzMessage="Nu am putut încărca produsele după frecvență." />
        } @else {
          <ske-order-list-suggestion-groups [groups]="frequencyGroups()"
                                              [addedItemIds]="addedItemIds()"
                                              emptyText="Nu există date încă"
                                              (add)="add.emit($event)" />
        }
      </nz-tab>
      <nz-tab nzTitle="Predefinite">
        @if (!supplyTabOpened() || supplyLists.isLoading()) {
          <nz-skeleton aria-busy="true"
                       [nzActive]="true"
                       [nzParagraph]="{ rows: 4 }" />
        } @else if (supplyLists.error()) {
          <nz-alert nzType="error"
                    nzShowIcon
                    nzMessage="Nu am putut încărca listele predefinite." />
        } @else {
          <ske-order-list-suggestion-groups [groups]="supplyListGroups()"
                                              [addedItemIds]="addedItemIds()"
                                              emptyText="Nu există liste predefinite active."
                                              (add)="add.emit($event)" />
        }
      </nz-tab>
    </nz-tabs>
    </div>
  `
})
export class OrderListSuggestionsPanel {
  public readonly classId = input.required<string>();
  public readonly addedItemIds = input.required<ReadonlySet<string>>();
  public readonly add = output<OrderListLineDto>();

  private readonly _stockHttp = inject(StockHttp);
  private readonly _classStatisticsHttp = inject(ClassStatisticsHttp);
  private readonly _supplyListsHttp = inject(SupplyListsHttp);
  protected readonly supplyTabOpened = signal(false);

  protected readonly lowStock = rxResource({
    params: () => this.classId(),
    stream: ({ params }) => this._stockHttp.getLowStockItems(params).pipe(map(groupLowStockItems))
  });

  // Separate from low stock: a failed statistics request must not blank the low stock tab.
  protected readonly segments = rxResource({
    stream: () => this._classStatisticsHttp.getPurchaseSegments()
  });

  // Needs one request per list for its lines, so it waits until the tab is first opened.
  protected readonly supplyLists = rxResource({
    params: () => this.supplyTabOpened() || undefined,
    // A list that fails to load (e.g. deactivated meanwhile) is skipped instead of failing the whole tab.
    stream: () => this._supplyListsHttp.getAllActive().pipe(
      switchMap((lists) => lists.length
        ? forkJoin(lists.map((list) => this._supplyListsHttp.getById(list.id).pipe(catchError(() => of(null)))))
        : of([])),
      map((lists) => lists.filter((list) => !!list))
    )
  });

  // The same lines the wizard builds, so the quantity shown is the quantity added.
  private readonly _linesByItem = computed(() => {
    const items = this.lowStock.hasValue() ? this.lowStock.value() : [];
    const segments = this.segments.hasValue() ? this.segments.value() : null;
    const lines = suggestionsToLines(items, segments, [
      ...items.map((item) => item.itemId),
      ...purchaseSegmentLists(segments).flat().map((item) => item.itemId)
    ]);

    return new Map(lines.map((line) => [line.itemId ?? '', line]));
  });

  protected readonly lowStockGroups = computed((): SuggestionGroup[] => {
    const items = this.lowStock.hasValue() ? this.lowStock.value() : [];
    const rows = items.flatMap((item) => {
      const line = this._linesByItem().get(item.itemId);

      return line ? [{ itemId: item.itemId, line, detail: item.locations.join(', ') }] : [];
    });

    return rows.length ? [{ title: null, rows }] : [];
  });

  protected readonly frequencyGroups = computed((): SuggestionGroup[] => {
    const segments = this.segments.hasValue() ? this.segments.value() : null;

    return purchaseSegmentLists(segments)
      .map((list, index) => ({
        title: FREQUENCY_TITLES[index],
        rows: list.flatMap((item) => {
          const line = this._linesByItem().get(item.itemId);

          return line ? [{ itemId: item.itemId, line, detail: `${line.quantity} ${line.unit}` }] : [];
        })
      }))
      .filter((group) => group.rows.length > 0);
  });

  protected readonly supplyListGroups = computed((): SuggestionGroup[] =>
    (this.supplyLists.hasValue() ? this.supplyLists.value() : [])
      .map((list) => ({
        title: list.name,
        rows: supplyListsToLines([list]).map((line) => ({
          itemId: line.itemId ?? '',
          line,
          detail: `${line.quantity} ${line.unit}`
        }))
      }))
      .filter((group) => group.rows.length > 0)
  );

  protected onTabChange(index: number) {
    if (index === SUPPLY_LISTS_TAB_INDEX)
      this.supplyTabOpened.set(true);
  }
}
