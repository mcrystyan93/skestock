import {
  Component,
  computed,
  DestroyRef,
  effect,
  inject,
  input,
  model,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type FormValueControl } from '@angular/forms/signals';
import {
  type GetAllItemsRequest,
  type ItemAutocompleteValue,
  type ItemDraft,
  type ItemDto,
  type ItemDropdownOption,
  ColumnFilter,
  PAGINATION_PAGE_SIZE,
} from '@ske/models';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import {
  NzAutocompleteComponent,
  NzAutocompleteOptionComponent,
  NzAutocompleteTriggerDirective,
} from 'ng-zorro-antd/auto-complete';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzInputDirective } from 'ng-zorro-antd/input';
import { NzSpinComponent } from 'ng-zorro-antd/spin';
import { ItemDropdownStore } from '../../services/item-dropdown.store';

type ItemSearchRequest = {
  term: string;
  version: number;
};

const CREATE_OPTION = Symbol('item-autocomplete-create');
const LOAD_MORE_OPTION = Symbol('item-autocomplete-load-more');

@Component({
  selector: 'ske-item-autocomplete',
  imports: [
    NzAutocompleteComponent,
    NzAutocompleteOptionComponent,
    NzAutocompleteTriggerDirective,
    NzButtonComponent,
    NzIconDirective,
    NzInputDirective,
    NzSpinComponent,
  ],
  template: `
    <div class="relative w-full">
      <input
        nz-input
        class="w-full pr-8"
        [value]="query()"
        [disabled]="disabled()"
        [placeholder]="placeholder()"
        [nzAutocomplete]="auto"
        [attr.aria-busy]="loading()"
        [attr.aria-invalid]="hasSearchError()"
        aria-autocomplete="list"
        (focus)="onFocus()"
        (input)="onInput($event)"
        (blur)="onBlur()"
        (keydown.enter)="onEnter($event)"
        (keydown.escape)="onEscape($event)"
      />

      @if (loading()) {
        <nz-spin
          nzSize="small"
          class="pointer-events-none absolute right-2 top-1/2 -translate-y-1/2"
        ></nz-spin>
      } @else if (allowClear() && (value() || query())) {
        <button
          nz-button
          nzType="text"
          nzSize="small"
          type="button"
          class="absolute right-1 top-1/2 -translate-y-1/2"
          aria-label="Șterge articolul"
          [disabled]="disabled()"
          (mousedown)="$event.preventDefault()"
          (click)="clear()"
        >
          <nz-icon nzType="close-circle" nzTheme="fill"></nz-icon>
        </button>
      }
    </div>

    @if (hasSearchError()) {
      <div class="text-error text-xs" role="alert">
        Nu s-au putut căuta articolele. Poți crea articolul introdus folosind opțiunea „Creează
        oricum”.
      </div>
    }

    <nz-autocomplete
      #auto
      [nzDefaultActiveFirstOption]="false"
      (selectionChange)="onOptionSelected($event)"
    >
      @for (item of searchResults(); track item.id) {
        <nz-auto-option [nzValue]="item" [nzLabel]="item.name">
          <span>{{ item.name }}</span>
          @if (item.sku) {
            <span class="ml-2 text-gray-500">({{ item.sku }})</span>
          }
        </nz-auto-option>
      }

      @if (showCreateOption()) {
        <nz-auto-option [nzValue]="createOptionValue" [nzLabel]="trimmedQuery()">
          Creează „{{ trimmedQuery() }}”
          @if (hasSearchError()) {
            <span class="ml-1">(oricum)</span>
          }
        </nz-auto-option>
      }

      @if (store.isLoadingMore()) {
        <nz-auto-option nzDisabled [nzLabel]="trimmedQuery()">
          Se încarcă mai multe articole…
        </nz-auto-option>
      } @else if (showLoadMoreOption()) {
        <nz-auto-option [nzValue]="loadMoreOptionValue" [nzLabel]="trimmedQuery()">
          Încarcă mai multe articole…
        </nz-auto-option>
      }
    </nz-autocomplete>
  `,
  providers: [ItemDropdownStore],
})
export class ItemAutocomplete implements FormValueControl<ItemAutocompleteValue> {
  public readonly value = model<ItemAutocompleteValue>(null);
  public readonly disabled = input<boolean>(false);
  public readonly allowClear = input<boolean>(false);
  public readonly placeholder = input<string>('Caută sau creează un articol');
  public readonly categoryId = input<string | null>(null);

  public readonly store = inject(ItemDropdownStore);
  public readonly createOptionValue = CREATE_OPTION;
  public readonly loadMoreOptionValue = LOAD_MORE_OPTION;

  private readonly _autocomplete = viewChild<NzAutocompleteComponent>('auto');
  private readonly _autocompleteTrigger = viewChild(NzAutocompleteTriggerDirective);
  private readonly _destroyRef = inject(DestroyRef);
  private readonly _query = signal('');
  private readonly _queryVersion = signal(0);
  private readonly _searchStartedVersion = signal(-1);
  private readonly _searchPending = signal(false);
  private readonly _hasUserQuery = signal(false);
  private readonly _pendingEnter = signal<string | null>(null);
  private readonly _search$ = new Subject<ItemSearchRequest>();
  private _firstCategoryLoad = true;
  private _focusVersion = 0;
  private _selectionVersion = 0;
  private _panelElement: HTMLElement | null = null;
  private _removePanelScrollListener: (() => void) | null = null;

  public readonly query = computed(() => this._query());
  public readonly trimmedQuery = computed(() => this._query().trim());
  public readonly normalizedQuery = computed(() => this.normalize(this._query()));
  public readonly selectedItemLabel = computed(() => {
    const value = this.value();

    if (!value) return '';

    if (!this.isPersistedValue(value)) return value.name;

    const resolvedItem = this.store.selectedItem();

    if (resolvedItem?.id === value.id && resolvedItem.name) return this.itemLabel(resolvedItem);

    if (this.store.selectedItemLoading()) return 'Se încarcă articolul…';

    if (this.store.selectedItemUnavailable()) return `Articol indisponibil (ID ${value.id})`;

    return value.name ? this.itemLabel(value) : 'Se încarcă articolul…';
  });

  public readonly loading = computed(
    () =>
      this.store.itemsLoading() || this.store.selectedItemLoading() || this.store.isLoadingMore(),
  );

  public readonly hasSearchError = computed(
    () => this.store.itemsProblemDetail() !== null || this.store.itemsValidationErrors() !== null,
  );

  public readonly isCurrentSearch = computed(() => {
    const query = this.normalizedQuery();

    if (!this._hasUserQuery() || !query) return false;

    return this.normalize(this.store.filter().searchTerm) === query;
  });

  public readonly searchResults = computed(() => {
    if (!this.isCurrentSearch() || this.store.itemsLoading()) return [];

    return this.store.items();
  });

  public readonly exactMatches = computed(() => {
    const query = this.normalizedQuery();

    if (!query) return [];

    return this.searchResults().filter(
      (item) => this.normalize(item.name) === query || this.normalize(item.sku) === query,
    );
  });

  public readonly showCreateOption = computed(() => {
    const query = this.normalizedQuery();

    return (
      !!query &&
      this.isCurrentSearch() &&
      !this._searchPending() &&
      !this.store.itemsLoading() &&
      this.exactMatches().length === 0
    );
  });

  public readonly showLoadMoreOption = computed(
    () =>
      this.isCurrentSearch() &&
      !this.hasSearchError() &&
      this.store.hasNextPage() &&
      !this.store.isLoadingMore(),
  );

  private readonly _searchSub = this._search$
    .pipe(
      debounceTime(300),
      distinctUntilChanged((previous, current) => previous.term === current.term),
      takeUntilDestroyed(this._destroyRef),
    )
    .subscribe(({ term, version }) => {
      const normalizedTerm = this.normalize(term);

      this._searchStartedVersion.set(version);

      if (!normalizedTerm) {
        this._searchPending.set(false);
        return;
      }

      this.store.load(this.buildFilter(normalizedTerm));
    });

  private readonly _selectedValueEffectRef = effect(() => {
    const value = this.value();

    untracked(() => this.store.resolveSelectedItem(value));
  });

  private readonly _selectedLabelEffectRef = effect(() => {
    const isEditing = this._hasUserQuery();
    const label = this.selectedItemLabel();

    if (!isEditing) untracked(() => this._query.set(label));
  });

  private readonly _categoryChangeEffectRef = effect(() => {
    this.categoryId();

    if (this._firstCategoryLoad) {
      this._firstCategoryLoad = false;
      return;
    }

    const query = untracked(() => this.normalizedQuery());
    const hasUserQuery = untracked(() => this._hasUserQuery());

    if (!hasUserQuery || !query) return;

    untracked(() => {
      this._searchPending.set(true);
      this._searchStartedVersion.set(this._queryVersion());
      this.store.load(this.buildFilter(query));
    });
  });

  private readonly _searchSettledEffectRef = effect(() => {
    const pendingQuery = this._pendingEnter();
    const query = this.normalizedQuery();
    const currentVersion = this._queryVersion();
    const startedVersion = this._searchStartedVersion();
    const isSearchPending = this._searchPending();
    const isLoading = this.store.itemsLoading();
    const isCurrentSearch = this.isCurrentSearch();
    const hasSearchError = this.hasSearchError();

    if (!isSearchPending || startedVersion !== currentVersion || !isCurrentSearch || isLoading)
      return;

    untracked(() => {
      this._searchPending.set(false);

      if (pendingQuery === query) {
        this._pendingEnter.set(null);

        if (!hasSearchError) this.commitResolvedQuery(query, this.trimmedQuery());
      }
    });
  });

  constructor() {
    this._destroyRef.onDestroy(() => this._removePanelScrollListener?.());
  }

  public onFocus() {
    this._hasUserQuery.set(false);
    this._focusVersion++;
    this.schedulePanelScrollBinding();
  }

  public onInput(event: Event) {
    if (this.disabled()) return;

    const target = event.target;

    if (!(target instanceof HTMLInputElement)) return;

    const value = target.value;
    const query = this.normalize(value);
    const version = this._queryVersion() + 1;

    this.store.clearItemsErrors();
    this._query.set(value);
    this._queryVersion.set(version);
    this._searchStartedVersion.set(-1);
    this._pendingEnter.set(null);
    this._searchPending.set(!!query);
    this._hasUserQuery.set(!!query);
    this._search$.next({ term: value, version });
    this.schedulePanelScrollBinding();
  }

  public onBlur() {
    const focusVersion = this._focusVersion;

    setTimeout(() => {
      if (focusVersion !== this._focusVersion || !this._hasUserQuery()) return;

      this.restoreCommittedValue();
    });
  }

  public onEnter(event: Event) {
    if (this.disabled()) return;

    event.preventDefault();
    const selectionVersion = this._selectionVersion;

    setTimeout(() => {
      if (selectionVersion !== this._selectionVersion) return;

      this.commitQueryOrWait();
    });
  }

  public onEscape(event: Event) {
    event.preventDefault();
    this.restoreCommittedValue();
    this._autocompleteTrigger()?.closePanel();
  }

  public onOptionSelected(option: NzAutocompleteOptionComponent) {
    this._selectionVersion++;

    if (option.nzValue === LOAD_MORE_OPTION) {
      this.loadMore();
      setTimeout(() => this._autocompleteTrigger()?.openPanel());
      return;
    }

    if (option.nzValue === CREATE_OPTION) {
      this.commitDraft(this.trimmedQuery());
      return;
    }

    if (this.isItemDto(option.nzValue)) this.commitItem(option.nzValue);
  }

  public clear() {
    if (this.disabled()) return;

    this.reset();
  }

  public reset() {
    this.value.set(null);
    this._query.set('');
    this._hasUserQuery.set(false);
    this.invalidatePendingSearch();
    this._autocompleteTrigger()?.closePanel();
  }

  public loadMore() {
    if (!this.isCurrentSearch()) return;

    this.store.loadMore();
    this.schedulePanelScrollBinding();
  }

  public itemLabel(item: ItemDropdownOption): string {
    return item.name ?? 'Articol indisponibil';
  }

  private commitQueryOrWait() {
    const query = this.normalizedQuery();
    const draftName = this.trimmedQuery();

    if (!this._hasUserQuery() || !draftName) return;

    if (this.hasSearchError()) {
      this.commitDraft(draftName);
      return;
    }

    if (this._searchPending() || this.store.itemsLoading() || !this.isCurrentSearch()) {
      this._pendingEnter.set(query);
      return;
    }

    this._pendingEnter.set(null);
    this.commitResolvedQuery(query, draftName);
  }

  private commitResolvedQuery(query: string, draftName: string) {
    const matches = this.exactMatches();

    if (matches.length === 1) {
      this.commitItem(matches[0]);
      return;
    }

    if (matches.length === 0) this.commitDraft(draftName);
  }

  private commitItem(item: ItemDto) {
    this.value.set(item);
    this.completeSelection(this.itemLabel(item));
  }

  private commitDraft(name: string) {
    if (!name) return;

    const draft: ItemDraft = { name };
    this.value.set(draft);
    this.completeSelection(name);
  }

  private completeSelection(label: string) {
    this._query.set(label);
    this._hasUserQuery.set(false);
    this._searchPending.set(false);
    this._pendingEnter.set(null);
    this._queryVersion.update((version) => version + 1);
    this._searchStartedVersion.set(-1);
    this._autocompleteTrigger()?.closePanel();
  }

  private restoreCommittedValue() {
    this._query.set(this.selectedItemLabel());
    this._hasUserQuery.set(false);
    this.invalidatePendingSearch();
  }

  private invalidatePendingSearch() {
    this._pendingEnter.set(null);
    this._searchPending.set(false);
    this._queryVersion.update((version) => version + 1);
    this._searchStartedVersion.set(-1);
  }

  private buildFilter(searchTerm: string): GetAllItemsRequest {
    return {
      searchTerm,
      pageSize: PAGINATION_PAGE_SIZE,
      filters: this.buildFilterWithCategory(untracked(() => this.store.filter().filters)),
      sort: [
        {
          value: 'ascend',
          key: 'name',
        },
      ],
    };
  }

  private buildFilterWithCategory(filters: Array<ColumnFilter> = []): Array<ColumnFilter> {
    const categoryId = untracked(() => this.categoryId());
    const filtersWithoutCategory = filters.filter((filter) => filter.field !== 'categoryId');

    if (categoryId === null) return filtersWithoutCategory;

    return [
      ...filtersWithoutCategory,
      {
        field: 'categoryId',
        value: categoryId,
        operator: 'equals',
        fieldType: 'number',
      },
    ];
  }

  private normalize(value: string | null | undefined): string {
    return (value ?? '')
      .trim()
      .normalize('NFD')
      .replace(/\p{Diacritic}/gu, '')
      .toLocaleLowerCase();
  }

  private isPersistedValue(value: ItemAutocompleteValue): value is ItemDto | ItemDropdownOption {
    return value !== null && 'id' in value;
  }

  private isItemDto(value: unknown): value is ItemDto {
    return (
      typeof value === 'object' && value !== null && 'id' in value && typeof value.id === 'string'
    );
  }

  private schedulePanelScrollBinding() {
    setTimeout(() => {
      const panel = this._autocomplete()?.panel?.nativeElement as HTMLElement | undefined;

      if (!panel || panel === this._panelElement) return;

      this._removePanelScrollListener?.();

      const onScroll = () => {
        const isNearBottom = panel.scrollTop + panel.clientHeight >= panel.scrollHeight - 16;

        if (isNearBottom) this.loadMore();
      };

      panel.addEventListener('scroll', onScroll, { passive: true });
      this._panelElement = panel;
      this._removePanelScrollListener = () => panel.removeEventListener('scroll', onScroll);
    });
  }
}
