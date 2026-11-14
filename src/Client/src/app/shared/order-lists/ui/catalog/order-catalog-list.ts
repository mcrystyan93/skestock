import { Component, computed, input, output, signal } from '@angular/core';
import { form, FormField } from '@angular/forms/signals';
import type { OrderCatalogItemDto } from '@ske/models';
import { type CatalogGroup, catalogGroups } from '@ske/shared/order-lists/services';
import { NzCheckboxComponent } from 'ng-zorro-antd/checkbox';
import { NzEmptyComponent } from 'ng-zorro-antd/empty';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzInputDirective, NzInputPrefixDirective, NzInputWrapperComponent } from 'ng-zorro-antd/input';
import { OrderCatalogRow } from './order-catalog-row';

type GroupView = CatalogGroup & { selectedCount: number };

/** The whole active catalog grouped by location, with a search that stays pinned while the list scrolls. */
@Component({
  imports: [
    FormField,
    NzCheckboxComponent,
    NzEmptyComponent,
    NzIconDirective,
    NzInputDirective,
    NzInputPrefixDirective,
    NzInputWrapperComponent,
    OrderCatalogRow
  ],
  selector: 'ske-order-catalog-list',
  host: {
    class: 'block'
  },
  template: `
    @if (items().length > 0) {
      <div class="sticky top-0 z-30 bg-white pb-2 dark:bg-slate-900">
        <nz-input-wrapper>
          <nz-icon nzType="icons:magnifying-glass"
                   nzInputPrefix></nz-icon>
          <input nz-input
                 type="search"
                 placeholder="Caută după nume sau SKU"
                 aria-label="Caută produse"
                 [formField]="searchForm.query" />
        </nz-input-wrapper>
      </div>
    }

    @for (group of views(); track group.key) {
      <div class="mt-3 flex items-center justify-between gap-2 border-0 border-b border-solid border-gray-200 pb-1 dark:border-gray-700">
        <h4 class="m-0 min-w-0 text-sm font-semibold wrap-break-word">{{ group.title }}</h4>
        @if (mode() === 'select') {
          <label nz-checkbox
                 class="shrink-0 text-sm"
                 [nzChecked]="group.selectedCount === group.rows.length"
                 [nzIndeterminate]="group.selectedCount > 0 && group.selectedCount < group.rows.length"
                 (nzCheckedChange)="toggleGroup(group, $event)">
            Selectează tot ({{ group.rows.length }})<span class="sr-only"> din {{ group.title }}</span>
          </label>
        }
      </div>
      <ul class="m-0 list-none p-0">
        @for (row of group.rows; track row.item.itemId) {
          <li>
            <ske-order-catalog-row [item]="row.item"
                                   [isLowStock]="row.isLowStock"
                                   [mode]="mode()"
                                   [marked]="mode() === 'select' ? selected().has(row.item.itemId) : addedItemIds().has(row.item.itemId)"
                                   (toggle)="toggleItem(row.item.itemId, $event)"
                                   (add)="add.emit(row.item)" />
          </li>
        }
      </ul>
    } @empty {
      <nz-empty [nzNotFoundContent]="items().length > 0 ? 'Niciun rezultat.' : emptyText()" />
    }
  `
})
export class OrderCatalogList {
  public readonly items = input.required<OrderCatalogItemDto[]>();
  public readonly mode = input.required<'select' | 'add'>();
  public readonly selectedIds = input<readonly string[]>([]);
  public readonly addedItemIds = input<ReadonlySet<string>>(new Set());
  public readonly emptyText = input('Nu există produse active.');
  public readonly selectedIdsChange = output<string[]>();
  public readonly add = output<OrderCatalogItemDto>();

  protected readonly searchModel = signal({ query: '' });
  protected readonly searchForm = form(this.searchModel);
  protected readonly selected = computed(() => new Set(this.selectedIds()));

  protected readonly views = computed((): GroupView[] => {
    const selected = this.selected();

    return catalogGroups(this.items(), this.searchForm.query().value()).map((group) => ({
      ...group,
      selectedCount: group.rows.filter((row) => selected.has(row.item.itemId)).length
    }));
  });

  protected toggleItem(itemId: string, checked: boolean) {
    this._emit(checked ? [...this.selectedIds(), itemId] : this.selectedIds().filter((id) => id !== itemId));
  }

  /** Only this group's visible rows change; ticks elsewhere stay. */
  protected toggleGroup(group: GroupView, checked: boolean) {
    const ids = new Set(group.rows.map((row) => row.item.itemId));
    const others = this.selectedIds().filter((id) => !ids.has(id));

    this._emit(checked ? [...others, ...ids] : others);
  }

  private _emit(ids: string[]) {
    this.selectedIdsChange.emit([...new Set(ids)]);
  }
}
