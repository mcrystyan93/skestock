import {
  Component,
  computed,
  DestroyRef,
  effect,
  inject,
  input,
  linkedSignal,
  model,
  TemplateRef,
  untracked
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, FormField, type FormValueControl } from '@angular/forms/signals';
import {
  ColumnFilter,
  GetAllItemsRequest,
  type ItemDropdownOption,
  type ItemDropdownValue,
  type ItemDto,
  PAGINATION_PAGE_SIZE
} from '@ske/models';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { ItemDropdownStore } from '../../services/item-dropdown.store';
import { NzOptionComponent, NzSelectComponent } from 'ng-zorro-antd/select';
import { NzSpinComponent } from 'ng-zorro-antd/spin';
import { NzSpaceCompactComponent } from 'ng-zorro-antd/space';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { isNil } from 'lodash-es';
import { ItemDetailModal } from '../modals/detail/item-detail-modal';
import { NzModalService } from 'ng-zorro-antd/modal';
import { NgTemplateOutlet } from '@angular/common';

@Component({
  selector: 'ske-item-dropdown',
  imports: [
    NzSelectComponent,
    FormField,
    NzSpinComponent,
    NzOptionComponent,
    NzSpaceCompactComponent,
    NzButtonComponent,
    NzIconDirective,
    NgTemplateOutlet
  ],
  template: `
    <nz-space-compact class="w-full">
      <nz-select [formField]="itemForm.item"
                 nzShowSearch
                 nzShowArrow
                 [nzPlaceHolder]="placeholder()"
                 [nzLoading]="store.loading()"
                 [nzAllowClear]="allowClear()"
                 [nzDisabled]="disabled()"
                 [attr.aria-busy]="store.loading()"
                 nzServerSearch
                 class="w-full"
                 [nzDropdownMatchSelectWidth]="false"
                 [compareWith]="(a, b) => a && b ? a.id === b.id : a === b"
                 (nzOnSearch)="onSearch($event)"
                 [nzDropdownRender]="loadingMoreTemplate"
                 (nzScrollToBottom)="loadMore()">
        @if (value(); as item) {
          <nz-option [nzValue]="item"
                     nzHide
                     [nzLabel]="selectedItemLabel()"></nz-option>
        }

        @for (item of store.items(); track item.id) {
          <nz-option [nzValue]="item"
                     [nzLabel]="item.name" />
        }
      </nz-select>

      @if (allowEdit()) {
        <button nz-button
                nzType="default"
                type="button"
                (click)="onEdit(value())"
                [disabled]="!value()?.id">
          @if (editButtonTemplate(); as editTemplate) {
            <ng-container *ngTemplateOutlet="editTemplate" />
          } @else {
            <nz-icon nzType="icons:pencil"></nz-icon>
          }
        </button>
      }
      @if (allowCreate()) {
        <button nz-button
                nzType="default"
                type="button"
                (click)="onAdd()">
          @if (createButtonTemplate(); as createTemplate) {
            <ng-container *ngTemplateOutlet="createTemplate" />
          } @else {
            <nz-icon nzType="icons:plus"></nz-icon>
          }
        </button>
      }
    </nz-space-compact>

    <ng-template #loadingMoreTemplate>
      @if (store.isLoadingMore()) {
        <nz-spin></nz-spin>
      }
    </ng-template>
  `,
  providers: [ItemDropdownStore, NzModalService]
})
export class ItemDropdown implements FormValueControl<ItemDropdownValue> {
  public readonly value = model<ItemDropdownValue>(null);
  public readonly disabled = input<boolean>(false);
  public readonly allowClear = input<boolean>(false);
  public readonly allowEdit = input<boolean>(true);
  public readonly allowCreate = input<boolean>(true);
  public readonly placeholder = input<string>('Selectați un articol');
  public readonly categoryId = input<string | null>(null);
  public readonly itemIds = input<readonly string[] | null>(null);
  public readonly createPrefill = input<Partial<ItemDto> | null>(null);
  public readonly createButtonTemplate = input<TemplateRef<unknown> | null>(null);
  public readonly editButtonTemplate = input<TemplateRef<unknown> | null>(null);

  public readonly store = inject(ItemDropdownStore);
  private readonly _search$ = new Subject<string>();
  private readonly _modalService = inject(NzModalService);
  private readonly _destroyRef = inject(DestroyRef);

  private _firstCategoryLoad = true;

  private readonly _formModel = linkedSignal({
    source: () => this.value(),
    computation: (value) => (<ItemDropdownFormModel>{ item: value })
  });

  public readonly itemForm = form(this._formModel);

  private readonly _formItemChangeEffectRef = effect(() => {
    const item = this.itemForm.item().value();

    untracked(() => this.value.set(item));
  });

  private readonly _searchSub = this._search$
    .pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntilDestroyed()
    )
    .subscribe((searchTerm) => {
      this.loadItems({ searchTerm });
    });

  private readonly _categoryChangeEffectRef = effect(() => {
    const categoryId = this.categoryId();

    if (this._firstCategoryLoad) {
      this._firstCategoryLoad = false;
      return;
    }

    untracked(() => this.loadItems({}));
  });

  private _firstItemIdsLoad = true;

  private readonly _itemIdsChangeEffectRef = effect(() => {
    const itemIds = this.itemIds();

    if (this._firstItemIdsLoad && itemIds === null) {
      this._firstItemIdsLoad = false;
      return;
    }

    this._firstItemIdsLoad = false;
    untracked(() => {
      const selectedItemId = this.value()?.id;
      if (itemIds !== null && selectedItemId && !itemIds.includes(selectedItemId)) {
        this.value.set(null);
      }

      this.loadItems({});
    });
  });

  private readonly _selectedValueEffectRef = effect(() => {
    const item = this.value();

    untracked(() => this.store.resolveSelectedItem(item));
  });

  public readonly selectedItemLabel = computed(() => {
    const item = this.value();

    if (!item?.id)
      return '';

    const resolvedItem = this.store.selectedItem();

    if (resolvedItem?.id === item.id && resolvedItem.name)
      return this.itemLabel(resolvedItem);

    if (this.store.selectedItemLoading())
      return 'Se încarcă articolul…';

    if (this.store.selectedItemUnavailable())
      return `Articol indisponibil (ID ${item.id})`;

    return item.name ? this.itemLabel(item) : 'Se încarcă articolul…';
  });

  public readonly selectedItemStatus = computed(() => {
    const item = this.value();

    if (!item?.id)
      return '';

    if (this.store.selectedItemLoading())
      return 'Se încarcă articolul selectat.';

    if (this.store.selectedItemUnavailable())
      return `Articolul cu ID ${item.id} nu este disponibil.`;

    return '';
  });

  public loadMore() {
    if (this.itemIds()?.length === 0)
      return;

    this.store.loadMore();
  }

  public onSearch(searchTerm: string) {
    this._search$.next(searchTerm);
  }

  public itemLabel(item: ItemDropdownOption): string {
    return item?.name ?? 'Articol indisponibil';
  }

  public onAdd() {
    this.openItemModal();
  }

  public onEdit(item: ItemDropdownValue) {
    if (isNil(item))
      return;

    this.openItemModal(item as ItemDto);
  }

  private openItemModal(item: ItemDto | null = null) {
    const modalRef = this._modalService.create({
      nzContent: ItemDetailModal,
      nzData: {
        item,
        prefill: item ? null : this.buildCreatePrefill()
      },
      nzCentered: true,
      nzMaskClosable: false,
      nzWrapClassName: 'modal-w-50'
    });

    modalRef.afterClose.pipe(
      takeUntilDestroyed(this._destroyRef)
    ).subscribe((savedItem: ItemDropdownValue) => {
      if (savedItem?.id)
        this.value.set(savedItem);

      this.loadItems(this.store.filter());
    });
  }

  private buildFilter(
    partialFilter: Partial<GetAllItemsRequest>
  ): GetAllItemsRequest {
    return {
      ...partialFilter,
      pageSize: partialFilter.pageSize ?? PAGINATION_PAGE_SIZE,
      filters: this.buildFilterWithConstraints(
        partialFilter.filters ?? untracked(() => this.store.filter().filters)
      ),
      sort: [{
        value: 'ascend',
        key: 'name'
      }]
    };
  }

  private buildCreatePrefill(): Partial<ItemDto> {
    const categoryId = this.categoryId();

    return {
      ...(isNil(categoryId) ? {} : { categoryId }),
      ...this.createPrefill()
    };
  }

  private loadItems(partialFilter: Partial<GetAllItemsRequest>) {
    if (this.itemIds()?.length === 0) {
      this.store.clearItems();
      return;
    }

    this.store.load(this.buildFilter(partialFilter));
  }

  private buildFilterWithConstraints(filters: Array<ColumnFilter> = []): Array<ColumnFilter> {
    const categoryId = untracked(() => this.categoryId());
    const itemIds = untracked(() => this.itemIds());

    filters = filters.filter((filter) => filter.field !== 'categoryId' && filter.field !== 'id');

    if (!isNil(categoryId)) {
      filters = [...filters, {
        field: 'categoryId',
        value: categoryId,
        operator: 'equals',
        fieldType: 'number'
      }];
    }

    if (itemIds !== null && itemIds.length > 0) {
      filters = [...filters, {
        field: 'id',
        value: [...itemIds],
        operator: 'in',
        fieldType: 'string'
      }];
    }

    return filters;
  }

}

type ItemDropdownFormModel = {
  item: ItemDropdownValue
};
