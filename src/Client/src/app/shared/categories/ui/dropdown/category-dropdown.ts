import { Component, DestroyRef, effect, inject, input, linkedSignal, model, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { disabled, form, FormField, type FormValueControl } from '@angular/forms/signals';
import { type CategoryDropdownValue, CategoryDto, GetAllCategoriesRequest, PAGINATION_PAGE_SIZE } from '@ske/models';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { CategoryDropdownStore } from '../../services/category-dropdown.store';
import { NzOptionComponent, NzSelectComponent } from 'ng-zorro-antd/select';
import { NzSpinComponent } from 'ng-zorro-antd/spin';
import { NzSpaceCompactComponent } from 'ng-zorro-antd/space';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { isNil } from 'lodash-es';
import { CategoryDetailModal } from '../modals/detail/category-detail-modal';
import { NzModalService } from 'ng-zorro-antd/modal';

@Component({
  selector: 'ske-category-dropdown',
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
      <nz-select [formField]="categoryForm.category"
                 [nzPlaceHolder]="placeholder()"
                 nzShowSearch
                 nzShowArrow
                 [nzLoading]="store.categoriesLoading()"
                 [nzAllowClear]="allowClear()"
                 nzServerSearch
                 class="w-full"
                 [compareWith]="(a, b) => a && b ? a.id === b.id : a === b"
                 (nzOnSearch)="onSearch($event)"
                 [nzDropdownRender]="loadingMoreTemplate"
                 (nzScrollToBottom)="loadMore()">
        @if (value(); as category) {
          <nz-option [nzValue]="category"
                     nzHide
                     [nzLabel]="category.name ?? ''"></nz-option>
        }

        @for (category of store.categories(); track category.id) {
          <nz-option [nzValue]="category"
                     [nzLabel]="category.name ?? ''"></nz-option>
        }
      </nz-select>
      @if (allowEdit() && value()?.id && !categoryForm().disabled()) {
        <button nz-button
                nzType="primary"
                type="button"
                (click)="onEdit(value())">
          <nz-icon nzType="icons:pencil"></nz-icon>
        </button>
      }
      @if (allowCreate() && !categoryForm().disabled()) {
        <button nz-button
                nzType="primary"
                type="button"
                (click)="onAdd()">
          <nz-icon nzType="icons:plus"></nz-icon>
        </button>
      }
    </nz-space-compact>

    <ng-template #loadingMoreTemplate>
      @if (store.isLoadingMore()) {
        <nz-spin></nz-spin>
      }
    </ng-template>
  `,
  providers: [CategoryDropdownStore, NzModalService]
})
export class CategoryDropdown implements FormValueControl<CategoryDropdownValue> {
  public readonly value = model<CategoryDropdownValue>(null);
  public readonly disabled = input<boolean>(false);
  public readonly allowClear = input<boolean>(false);
  public readonly allowEdit = input<boolean>(true);
  public readonly allowCreate = input<boolean>(true);
  public readonly placeholder = input<string>('Selectați o categorie');
  public readonly createPrefill = input<Partial<CategoryDto> | null>(null);

  public readonly store = inject(CategoryDropdownStore);
  private readonly _search$ = new Subject<string>();
  private readonly _modalService = inject(NzModalService);
  private readonly _destroyRef = inject(DestroyRef);

  private readonly _formModel = linkedSignal({
    source: () => this.value(),
    computation: (value) => (<CategoryDropdownFormModel>{ category: value })
  });

  public readonly categoryForm = form(this._formModel, (schemaPath) => {
    disabled(schemaPath, { when: () => this.disabled() });
  });

  private readonly _formCategoryChangeEffectRef = effect(() => {
    const category = this.categoryForm.category().value();

    untracked(() => this.value.set(category));
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

  public loadMore() {
    this.store.loadMore();
  }

  public onSearch(searchTerm: string) {
    this._search$.next(searchTerm);
  }

  public onAdd() {
    this.openCategoryModal(null, this.createPrefill());
  }

  public onEdit(category: CategoryDropdownValue) {
    if (isNil(category))
      return;

    this.openCategoryModal(category as CategoryDto);
  }

  private openCategoryModal(category: CategoryDto | null = null, prefill: Partial<CategoryDto> | null = null) {
    const modalRef = this._modalService.create({
      nzContent: CategoryDetailModal,
      nzData: {
        category,
        prefill
      },
      nzCentered: true,
      nzMaskClosable: false
    });

    modalRef.afterClose.pipe(
      takeUntilDestroyed(this._destroyRef)
    ).subscribe((savedCategory: Partial<CategoryDto> | null) => {
      if (savedCategory?.id)
        this.value.set(savedCategory);

      this.store.load(this.store.filter());
    });
  }

  private buildFilter(
    partialFilter: Partial<GetAllCategoriesRequest>
  ): GetAllCategoriesRequest {
    return {
      ...partialFilter,
      pageSize: partialFilter.pageSize ?? PAGINATION_PAGE_SIZE,
      filters: partialFilter.filters ?? untracked(() => this.store.filter().filters),
      sort: [{
        value: 'ascend',
        key: 'name'
      }]
    };
  }

}

type CategoryDropdownFormModel = {
  category: CategoryDropdownValue
};
