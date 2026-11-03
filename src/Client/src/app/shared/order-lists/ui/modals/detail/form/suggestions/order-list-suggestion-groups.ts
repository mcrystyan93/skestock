import { Component, computed, input, output, signal } from '@angular/core';
import { form, FormField } from '@angular/forms/signals';
import type { OrderListLineDto } from '@ske/models';
import { ItemThumbnail } from '@ske/shared/items';
import { NzEmptyComponent } from 'ng-zorro-antd/empty';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzInputDirective, NzInputPrefixDirective, NzInputWrapperComponent } from 'ng-zorro-antd/input';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';

export type SuggestionRow = { itemId: string; line: OrderListLineDto; detail: string };

export type SuggestionGroup = { title: string | null; rows: SuggestionRow[] };

/** Lowercase without diacritics, so "paine" finds "Pâine". */
function normalize(text: string): string {
  return text.normalize('NFD').replace(/\p{Diacritic}/gu, '').toLowerCase();
}

@Component({
  imports: [
    FormField,
    ItemThumbnail,
    NzEmptyComponent,
    NzIconDirective,
    NzInputDirective,
    NzInputPrefixDirective,
    NzInputWrapperComponent,
    NzTypographyComponent
  ],
  selector: 'ske-order-list-suggestion-groups',
  host: {
    class: 'block'
  },
  template: `
    @if (groups().length > 0) {
      <div class="sticky top-0 z-30 pb-2">
      <nz-input-wrapper>
        <nz-icon nzType="icons:magnifying-glass"
                 nzInputPrefix></nz-icon>
        <input nz-input
               type="search"
               placeholder="Caută după nume sau SKU"
               aria-label="Caută în sugestii"
               [formField]="searchForm.query" />
      </nz-input-wrapper>
      </div>
    }

    @for (group of visibleGroups(); track $index) {
      @if (group.title) {
        <h4 nz-typography
            class="mb-1! mt-4!">{{ group.title }}</h4>
      }
      <ul class="m-0 list-none p-0">
        @for (row of group.rows; track row.itemId) {
          @let added = addedItemIds().has(row.itemId);
          <li>
            <button type="button"
                    class="m-0 flex w-full cursor-pointer items-center gap-3 rounded border-0 bg-transparent px-1 py-2 text-left hover:bg-black/5 disabled:cursor-default disabled:hover:bg-transparent dark:hover:bg-white/10"
                    [disabled]="added"
                    [attr.aria-label]="added ? null : 'Adaugă ' + row.line.productName"
                    (click)="add.emit(row.line)">
              <ske-item-thumbnail [itemId]="row.itemId"
                                  [imageFileId]="row.line.itemImageFileId" />
              <span class="flex min-w-0 grow flex-col">
                <span class="truncate font-medium">{{ row.line.productName }}</span>
                <span class="truncate text-sm opacity-70">{{ row.detail }}</span>
              </span>
              @if (added) {
                <span class="me-2 text-sm opacity-60">Adăugat</span>
              } @else {
                <nz-icon nzType="icons:chevrons-right"
                         class="me-2"
                         aria-hidden="true"></nz-icon>
              }
            </button>
          </li>
        }
      </ul>
    } @empty {
      <nz-empty [nzNotFoundContent]="groups().length > 0 ? 'Niciun rezultat.' : emptyText()" />
    }
  `
})
export class OrderListSuggestionGroups {
  public readonly groups = input.required<SuggestionGroup[]>();
  public readonly addedItemIds = input.required<ReadonlySet<string>>();
  public readonly emptyText = input.required<string>();
  public readonly add = output<OrderListLineDto>();

  protected readonly searchModel = signal({ query: '' });
  protected readonly searchForm = form(this.searchModel);

  /** Product names already include the SKU. A group whose title matches stays whole, so "lunar" lists the whole segment. */
  protected readonly visibleGroups = computed((): SuggestionGroup[] => {
    const query = normalize(this.searchForm.query().value().trim());

    if (!query)
      return this.groups();

    return this.groups().flatMap((group) => {
      if (group.title && normalize(group.title).includes(query))
        return [group];

      const rows = group.rows.filter((row) => normalize(row.line.productName).includes(query));

      return rows.length ? [{ ...group, rows }] : [];
    });
  });
}
