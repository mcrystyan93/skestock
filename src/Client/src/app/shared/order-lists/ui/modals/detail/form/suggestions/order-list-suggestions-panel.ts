import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, inject, input, output, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import type { OrderListLineDto } from '@ske/models';
import { ClassStatisticsHttp } from '@ske/shared/class-statistics';
import { ItemThumbnail } from '@ske/shared/items';
import { groupLowStockItems, purchaseSegmentLists, suggestionsToLines, supplyListsToLines } from '@ske/shared/order-lists/services';
import { StockHttp } from '@ske/shared/stock';
import { SupplyListsHttp } from '@ske/shared/supply-lists';
import { catchError, forkJoin, map, of, switchMap } from 'rxjs';
import { NzAlertComponent } from 'ng-zorro-antd/alert';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzEmptyComponent } from 'ng-zorro-antd/empty';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzSkeletonComponent } from 'ng-zorro-antd/skeleton';
import { NzTabComponent, NzTabsComponent } from 'ng-zorro-antd/tabs';

type SuggestionRow = { itemId: string; line: OrderListLineDto; detail: string };

type SuggestionGroup = { title: string | null; rows: SuggestionRow[] };

const SUPPLY_LISTS_TAB_INDEX = 2;

const FREQUENCY_TITLES = ['Săptămânal', 'La 2 săptămâni', 'Lunar', 'O dată pe clasă'];

@Component({
  imports: [
    ItemThumbnail,
    NgTemplateOutlet,
    NzAlertComponent,
    NzButtonComponent,
    NzEmptyComponent,
    NzIconDirective,
    NzSkeletonComponent,
    NzTabComponent,
    NzTabsComponent
  ],
  selector: 'ske-order-list-suggestions-panel',
  host: {
    class: 'relative min-h-0 -mb-6'
  },
  template: `
    <ng-template #groupList
                 let-groups
                 let-emptyText="emptyText">
      @for (group of groups; track $index) {
        @if (group.title) {
          <h4 class="m-0 mt-4 text-base font-semibold opacity-80">{{ group.title }}</h4>
        }
        <ul class="m-0 list-none p-0">
          @for (row of group.rows; track row.itemId) {
            @let added = addedItemIds().has(row.itemId);
            <li class="flex items-center gap-3 py-2">
              <ske-item-thumbnail [itemId]="row.itemId"
                                  [imageFileId]="row.line.itemImageFileId" />
              <span class="flex min-w-0 grow flex-col">
                <span class="truncate font-medium">{{ row.line.productName }}</span>
                <span class="truncate text-sm opacity-70">{{ row.detail }}</span>
              </span>
              <button nz-button
                      nzType="text"
                      nzSize="small"
                      class="me-2"
                      type="button"
                      [disabled]="added"
                      [attr.aria-label]="added ? null : 'Adaugă ' + row.line.productName"
                      (click)="add.emit(row.line)">
                @if (added) {
                  <nz-icon nzType="icons:circle-check"></nz-icon>
                  <span>Adăugat</span>
                } @else {
                  <nz-icon nzType="icons:chevrons-right"></nz-icon>
                }
              </button>
            </li>
          }
        </ul>
      } @empty {
        <nz-empty [nzNotFoundContent]="emptyText" />
      }
    </ng-template>

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
          <ng-container *ngTemplateOutlet="groupList; context: { $implicit: lowStockGroups(), emptyText: 'Nu există articole cu stoc scăzut.' }" />
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
          <ng-container *ngTemplateOutlet="groupList; context: { $implicit: frequencyGroups(), emptyText: 'Nu există date încă' }" />
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
          <ng-container *ngTemplateOutlet="groupList; context: { $implicit: supplyListGroups(), emptyText: 'Nu există liste predefinite active.' }" />
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

    // Average quantities are fractional; order whole units, never below 1.
    return new Map(lines.map((line) => [line.itemId ?? '', { ...line, quantity: Math.max(1, Math.floor(line.quantity)) }]));
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
