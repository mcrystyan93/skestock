import { Component, DestroyRef, effect, inject, input, linkedSignal, model, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, FormField, type FormValueControl } from '@angular/forms/signals';
import { type ItemDropdownValue, ItemDto, GetAllItemsRequest, PAGINATION_PAGE_SIZE, ColumnFilter } from '@ske/models';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { ItemDropdownStore } from '../../services/item-dropdown.store';
import { NzOptionComponent, NzSelectComponent } from 'ng-zorro-antd/select';
import { NzSpinComponent } from 'ng-zorro-antd/spin';
import { NzSpaceCompactComponent } from 'ng-zorro-antd/space';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { isNil } from 'lodash-es';
import { ItemDetailModal } from '../modals/item-detail-modal';
import { NzModalService } from 'ng-zorro-antd/modal';

@Component({
  selector: 'ske-item-dropdown',
  imports: [
    NzSelectComponent,
    FormField,
    NzSpinComponent,
    NzOptionComponent,
    NzSpaceCompactComponent,
    NzButtonComponent,
    NzIconDirective
  ],
  template: `
    <nz-space-compact class="w-full">
      <nz-select [formField]="itemForm.item"
                 nzShowSearch
                 nzShowArrow
                 [nzLoading]="store.itemsLoading()"
                 [nzAllowClear]="allowClear()"
                 nzServerSearch
                 class="w-full"
                 [compareWith]="(a, b) => a && b ? a.id === b.id : a === b"
                 (nzOnSearch)="onSearch($event)"
                 [nzDropdownRender]="loadingMoreTemplate"
                 (nzScrollToBottom)="loadMore()">
        @if (value(); as item) {
          <nz-option [nzValue]="item"
                     [nzLabel]="item.name ?? ''"></nz-option>
        }

        @for (item of store.items(); track item.id) {
          <nz-option [nzValue]="item"
                     [nzLabel]="item.name ?? ''"></nz-option>
        }
      </nz-select>
      @if (value()?.id) {
        <button nz-button
                nzType="primary"
                type="button"
                (click)="onEdit(value())">
          <nz-icon nzType="icons:pencil"></nz-icon>
        </button>
      }
      <button nz-button
              nzType="primary"
              type="button"
              (click)="onAdd()">
        <nz-icon nzType="icons:plus"></nz-icon>
      </button>
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
  public readonly placeholder = input<string>('Selectați un articol');
  public readonly categoryId = input<string | null>(null);

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
      this.store.load(this.buildFilter({ searchTerm }));
    });

  private readonly _categoryChangeEffectRef = effect(() => {
    const categoryId = this.categoryId();

    if (this._firstCategoryLoad) {
      this._firstCategoryLoad = false;
      return;
    }

    untracked(() => this.store.load(this.buildFilter({})));
  });

  public loadMore() {
    this.store.loadMore();
  }

  public onSearch(searchTerm: string) {
    this._search$.next(searchTerm);
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
        item
      },
      nzCentered: true,
      nzMaskClosable: false
    });

    modalRef.afterClose.pipe(
      takeUntilDestroyed(this._destroyRef)
    ).subscribe(() => {
      this.store.load(this.store.filter());
    });
  }

  private buildFilter(
    partialFilter: Partial<GetAllItemsRequest>
  ): GetAllItemsRequest {
    return {
      ...partialFilter,
      pageSize: partialFilter.pageSize ?? PAGINATION_PAGE_SIZE,
      filters: this.buildFilterWithCategory(partialFilter.filters ?? untracked(() => this.store.filter().filters)),
      sort: [{
        value: 'ascend',
        key: 'name'
      }]
    };
  }

  private buildFilterWithCategory(filters: Array<ColumnFilter> = []): Array<ColumnFilter> {
    const categoryId = untracked(() => this.categoryId());

    // remove any categoryId filter
    filters = filters.filter((filter) => filter.field !== 'categoryId');

    if (isNil(categoryId))
      return filters;

    return [...filters, {
      field: 'categoryId',
      value: categoryId,
      operator: 'equals',
      fieldType: 'number'
    }];
  }

}

type ItemDropdownFormModel = {
  item: ItemDropdownValue
};
