import { Component, DestroyRef, effect, inject, input, linkedSignal, model, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { disabled, form, FormField, type FormValueControl } from '@angular/forms/signals';
import {
  GetAllSchoolClassesRequest,
  PAGINATION_PAGE_SIZE,
  type SchoolClassDropdownValue
} from '@ske/models';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { NzOptionComponent, NzSelectComponent } from 'ng-zorro-antd/select';
import { NzSpinComponent } from 'ng-zorro-antd/spin';
import { SchoolClassDropdownStore } from '../../services/school-class-dropdown.store';

@Component({
  selector: 'ske-school-class-dropdown',
  imports: [
    NzSelectComponent,
    FormField,
    NzSpinComponent,
    NzOptionComponent
  ],
  template: `
    <nz-select [formField]="schoolClassForm.schoolClass"
               nzShowSearch
               nzShowArrow
               [nzLoading]="store.schoolClassesLoading()"
               [nzAllowClear]="allowClear()"
               nzServerSearch
               class="w-full"
               [compareWith]="(a, b) => a && b ? a.id === b.id : a === b"
               (nzOnSearch)="onSearch($event)"
               [nzDropdownRender]="loadingMoreTemplate"
               (nzScrollToBottom)="loadMore()"
               [nzPlaceHolder]="placeholder()">
      @if (value(); as schoolClass) {
        <nz-option [nzValue]="schoolClass"
                   nzHide
                   [nzLabel]="schoolClass.name ?? ''"></nz-option>
      }

      @for (schoolClass of store.schoolClasses(); track schoolClass.id) {
        <nz-option [nzValue]="schoolClass"
                   [nzLabel]="schoolClass.name ?? ''"></nz-option>
      }
    </nz-select>

    <ng-template #loadingMoreTemplate>
      @if (store.isLoadingMore()) {
        <nz-spin></nz-spin>
      }
    </ng-template>
  `,
  providers: [SchoolClassDropdownStore]
})
export class SchoolClassDropdown implements FormValueControl<SchoolClassDropdownValue> {
  public readonly value = model<SchoolClassDropdownValue>(null);
  public readonly disabled = input<boolean>(false);
  public readonly allowClear = input<boolean>(false);
  public readonly placeholder = input<string>('Selectați o clasă');

  public readonly store = inject(SchoolClassDropdownStore);
  private readonly _search$ = new Subject<string>();
  private readonly _destroyRef = inject(DestroyRef);

  private readonly _formModel = linkedSignal({
    source: () => this.value(),
    computation: (value) => (<SchoolClassDropdownFormModel>{ schoolClass: value })
  });

  public readonly schoolClassForm = form(this._formModel, (schemaPath) => {
    disabled(schemaPath, { when: () => this.disabled() });
  });

  private readonly _formSchoolClassChangeEffectRef = effect(() => {
    const schoolClass = this.schoolClassForm.schoolClass().value();

    untracked(() => this.value.set(schoolClass));
  });

  private readonly _searchSub = this._search$
    .pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntilDestroyed(this._destroyRef)
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
    partialFilter: Partial<GetAllSchoolClassesRequest>
  ): GetAllSchoolClassesRequest {
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

type SchoolClassDropdownFormModel = {
  schoolClass: SchoolClassDropdownValue
};
