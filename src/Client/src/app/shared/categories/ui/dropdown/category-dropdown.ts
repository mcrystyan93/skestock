import { Component, inject, input, linkedSignal, model, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, FormField, type FormValueControl } from '@angular/forms/signals';
import { type CategoryDropdownValue, GetAllCategoriesRequest, PAGINATION_PAGE_SIZE } from '@ske/models';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { CategoryDropdownStore } from '../../services/category-dropdown.store';
import { NzOptionComponent, NzSelectComponent } from 'ng-zorro-antd/select';
import { NzSpinComponent } from 'ng-zorro-antd/spin';

@Component({
  selector: 'ske-category-dropdown',
  imports: [
    NzSelectComponent,
    FormField,
    NzSpinComponent,
    NzOptionComponent
  ],
  template: `
    <nz-select [formField]="categoryForm.category"
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
                   [nzLabel]="category.name ?? ''"></nz-option>
      }

      @for (category of store.categories(); track category.id) {
        <nz-option [nzValue]="category"
                   [nzLabel]="category.name ?? ''"></nz-option>
      }
    </nz-select>

    <ng-template #loadingMoreTemplate>
      @if (store.isLoadingMore()) {
        <nz-spin></nz-spin>
      }
    </ng-template>
  `,
  providers: [CategoryDropdownStore]
})
export class CategoryDropdown implements FormValueControl<CategoryDropdownValue> {
  public readonly value = model<CategoryDropdownValue>(null);
  public readonly disabled = input<boolean>(false);
  public readonly allowClear = input<boolean>(false);
  public readonly placeholder = input<string>('Selectați o categorie');

  public readonly store = inject(CategoryDropdownStore);
  private readonly _search$ = new Subject<string>();

  private readonly _formModel = linkedSignal({
    source: () => this.value(),
    computation: (value) => (<CategoryDropdownFormModel>{ category: value })
  });

  public readonly categoryForm = form(this._formModel);

  private readonly _searchSub = this._search$
    .pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntilDestroyed(),
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

  private buildFilter(
    partialFilter: Partial<GetAllCategoriesRequest>,
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
