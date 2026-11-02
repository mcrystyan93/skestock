import { Component, computed, input, output } from '@angular/core';
import type { PurchaseSegmentItemDto, PurchaseSegmentsDto } from '@ske/models';
import { ItemThumbnail } from '@ske/shared/items';
import type { SuggestedItem } from '@ske/shared/order-lists/services';
import { NzCheckboxComponent } from 'ng-zorro-antd/checkbox';
import { NzEmptyComponent } from 'ng-zorro-antd/empty';
import { NzSkeletonComponent } from 'ng-zorro-antd/skeleton';
import { NzTabComponent, NzTabsComponent } from 'ng-zorro-antd/tabs';

type SegmentRow = {
  itemId: string;
  name: string;
  sku: string | null;
  imageFileId: string | null;
  /** Shown only for purchase segments; low stock lines start at 1 and are edited in the last step. */
  quantity: number | null;
  unit: string;
};

type SegmentGroup = { title: string | null; rows: SegmentRow[] };

type SegmentTabBase = { key: string; title: string; emptyText: string; groups: SegmentGroup[]; ids: string[] };

type SegmentTab = SegmentTabBase & { selectedCount: number };

const EMPTY_PURCHASES = 'Nu există date încă';

@Component({
  imports: [ItemThumbnail, NzCheckboxComponent, NzEmptyComponent, NzSkeletonComponent, NzTabComponent, NzTabsComponent],
  selector: 'ske-order-list-suggestions-step',
  template: `
    <p class="mb-3 mt-0 opacity-80">
      Alege produsele pentru comandă din oricare dintre grupe. Le poți ajusta în pasul următor.
    </p>

    @if (loading()) {
      <nz-skeleton aria-busy="true" [nzActive]="true" [nzParagraph]="{ rows: 4 }" />
    } @else {
      <nz-tabs nzSize="small">
        @for (tab of tabs(); track tab.key) {
          <nz-tab [nzTitle]="titleTemplate">
            <ng-template #titleTemplate>
              {{ tab.title }}
              <span class="ml-1 rounded-full bg-gray-100 px-2 text-xs dark:bg-gray-700">{{ tab.ids.length }}</span>
            </ng-template>

            @if (tab.ids.length === 0) {
              <nz-empty [nzNotFoundContent]="tab.emptyText" />
            } @else {
              <div class="mb-2 flex items-center justify-between gap-2 border-0 border-b border-solid border-gray-200 pb-2 dark:border-gray-700">
                <label nz-checkbox
                       [nzChecked]="tab.selectedCount === tab.ids.length"
                       [nzIndeterminate]="tab.selectedCount > 0 && tab.selectedCount < tab.ids.length"
                       (nzCheckedChange)="toggleTab(tab, $event)">
                  Selectează tot
                </label>
                <span class="text-sm opacity-70" aria-live="polite">
                  {{ tab.selectedCount }} din {{ tab.ids.length }} selectate
                </span>
              </div>

              @for (group of tab.groups; track group.title) {
                @if (group.title) {
                  <h4 class="m-0 mt-3 text-sm font-semibold opacity-80">{{ group.title }}</h4>
                }
                <ul class="m-0 list-none p-0">
                  @for (row of group.rows; track row.itemId) {
                    <li class="flex items-center gap-3 py-2">
                      <ske-item-thumbnail [itemId]="row.itemId"
                                          [imageFileId]="row.imageFileId"
                                          [large]="true" />
                      <label nz-checkbox
                             class="flex! min-w-0 grow cursor-pointer items-center! m-0! [&>span:last-child]:min-w-0 [&>span:last-child]:flex-1 [&>span:last-child]:ps-3 [&>span:last-child]:pe-0"
                             [nzChecked]="selected().has(row.itemId)"
                             (nzCheckedChange)="toggle(row.itemId, $event)">
                        <span class="flex min-w-0 flex-col">
                          <span class="truncate font-medium">
                            {{ row.name }}
                            @if (row.sku) {
                              <span class="ml-1 text-sm font-normal opacity-60">({{ row.sku }})</span>
                            }
                          </span>
                          @if (row.quantity !== null) {
                            <span class="text-sm opacity-70">{{ row.quantity }} {{ row.unit }}</span>
                          }
                        </span>
                      </label>
                    </li>
                  }
                </ul>
              }
            }
          </nz-tab>
        }
      </nz-tabs>
    }
  `
})
export class OrderListSuggestionsStep {
  public readonly items = input.required<SuggestedItem[]>();
  public readonly segments = input<PurchaseSegmentsDto | null>(null);
  public readonly selectedIds = input.required<string[]>();
  public readonly loading = input(false);
  public readonly selectedIdsChange = output<string[]>();

  protected readonly selected = computed(() => new Set(this.selectedIds()));

  protected readonly tabs = computed((): SegmentTab[] => {
    const segments = this.segments();
    const selected = this.selected();
    const purchaseTab = (key: string, title: string, list: PurchaseSegmentItemDto[] | undefined): SegmentTabBase => {
      const rows: SegmentRow[] = (list ?? []).map((item) => ({
        itemId: item.itemId,
        name: item.name,
        sku: item.sku,
        imageFileId: item.imageFileId,
        quantity: item.averageQuantity,
        unit: item.unit
      }));

      return { key, title, emptyText: EMPTY_PURCHASES, groups: [{ title: null, rows }], ids: rows.map((row) => row.itemId) };
    };

    const tabs: SegmentTabBase[] = [
      this._lowStockTab(),
      purchaseTab('weekly', 'Comandate săptămânal', segments?.weekly),
      purchaseTab('everyTwoWeeks', 'La 2 săptămâni', segments?.everyTwoWeeks),
      purchaseTab('monthly', 'Lunar', segments?.monthly),
      purchaseTab('oncePerClass', 'O dată pe clasă', segments?.oncePerClass)
    ];

    return tabs.map((tab) => ({ ...tab, selectedCount: tab.ids.filter((id) => selected.has(id)).length }));
  });

  protected toggle(itemId: string, checked: boolean) {
    this._emit(checked ? [...this.selectedIds(), itemId] : this.selectedIds().filter((id) => id !== itemId));
  }

  /** Only this tab's ids change; ticks made in other tabs stay. */
  protected toggleTab(tab: SegmentTab, checked: boolean) {
    const others = this.selectedIds().filter((id) => !tab.ids.includes(id));

    this._emit(checked ? [...others, ...tab.ids] : others);
  }

  private _emit(ids: string[]) {
    this.selectedIdsChange.emit([...new Set(ids)]);
  }

  /** An item low in two locations shows under both headings, but is one id and ticks together. */
  private _lowStockTab(): SegmentTabBase {
    const byLocation = new Map<string, SegmentRow[]>();

    for (const item of this.items()) {
      const row: SegmentRow = {
        itemId: item.itemId,
        name: item.itemName,
        sku: item.sku,
        imageFileId: item.imageFileId,
        quantity: null,
        unit: item.unit
      };

      for (const location of item.locations)
        byLocation.set(location, [...(byLocation.get(location) ?? []), row]);
    }

    const groups = [...byLocation]
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([title, rows]) => ({ title, rows }));

    return {
      key: 'lowStock',
      title: 'Stoc redus',
      emptyText: 'Nu există articole cu stoc scăzut.',
      groups,
      ids: this.items().map((item) => item.itemId)
    };
  }
}
