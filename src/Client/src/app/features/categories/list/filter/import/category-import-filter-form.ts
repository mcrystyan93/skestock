import { Component, computed, effect, input, linkedSignal, output, signal, untracked } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import {
  buildEqualsFilterForValue,
  CategoryImportBatchStatus,
  ColumnFilter,
  GetAllCategoryImportBatchesRequest,
  getFilterValue
} from '@ske/models';
import { debounce, form, FormField, submit } from '@angular/forms/signals';
import { isEqual, isNil } from 'lodash-es';
import { FormsModule } from '@angular/forms';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzFormDirective } from 'ng-zorro-antd/form';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzInputDirective, NzInputPrefixDirective, NzInputWrapperComponent } from 'ng-zorro-antd/input';
import { NzSegmentedComponent, NzSegmentedItemComponent } from 'ng-zorro-antd/segmented';
import { NzFormControlComponent, NzFormItemComponent } from 'ng-zorro-antd/form';
import { NzBadgeComponent } from 'ng-zorro-antd/badge';
import { NzDrawerComponent, NzDrawerContentDirective } from 'ng-zorro-antd/drawer';
import { NzOptionComponent, NzSelectComponent } from 'ng-zorro-antd/select';
import { LayoutBreakpoint } from '@ske/shared/directives';

@Component({
  imports: [
    FormsModule,
    NzFormDirective,
    NzRowDirective,
    NzColDirective,
    NzInputWrapperComponent,
    NzIconDirective,
    NzInputDirective,
    FormField,
    NzButtonComponent,
    NzSegmentedComponent,
    NzSegmentedItemComponent,
    NzFormItemComponent,
    NzFormControlComponent,
    NzInputPrefixDirective,
    NzBadgeComponent,
    NzDrawerComponent,
    NzDrawerContentDirective,
    NzSelectComponent,
    NzOptionComponent,
    NgTemplateOutlet,
    LayoutBreakpoint
  ],
  selector: 'ske-category-import-filter-form',
  templateUrl: './category-import-filter-form.html'
})
export class FilterForm {
  public readonly loading = input.required<boolean>();
  public readonly filter = input.required<GetAllCategoryImportBatchesRequest>();
  public readonly onFilterChange = output<GetAllCategoryImportBatchesRequest>();

  private _initialFilterEmitted = false;
  private _initialFormChangeHandled = false;

  private readonly _formModel = linkedSignal({
    source: () => this.filter(),
    computation: (filter) => (<CategoryImportFilterModel>{
      searchTerm: filter.searchTerm ?? '',
      status: getFilterValue<string>(filter.filters, 'status') ?? 'all'
    })
  });

  public readonly statusSegmentOptions: { label: string, value: CategoryImportBatchStatus }[] = [
    { label: 'Toate', value: 'all' },
    { label: 'Procesare', value: 'processing' },
    { label: 'Asteptare', value: 'pendingReview' },
    { label: 'Confirmate', value: 'confirmed' },
    { label: 'Esuate', value: 'failed' }
  ];

  public readonly filtersOpen = signal(false);

  public readonly activeFilterCount = computed(() => {
    const status = this.filterForm().value().status;
    return status && status !== 'all' ? 1 : 0;
  });

  public readonly filtersButtonLabel = computed(() => {
    const count = this.activeFilterCount();
    return count === 0 ? 'Filtre' : `Filtre, ${count} ${count === 1 ? 'filtru activ' : 'filtre active'}`;
  });

  public readonly filterForm = form(this._formModel, (schemaPath) => {
    debounce(schemaPath.searchTerm, 300);
  });

  private readonly _initialFilterEffectRef = effect(() => {
    if (this._initialFilterEmitted)
      return;

    this.filter();
    this.onFilterChange.emit(this.buildFilterCriteria());
    this._initialFilterEmitted = true;
  });

  private readonly _formEffectChange = effect(() => {
    this.filterForm().value();

    if (!this._initialFormChangeHandled) {
      this._initialFormChangeHandled = true;
      return;
    }

    const currentFilter = untracked(() => this.filter());
    const nextFilter = untracked(() => this.buildFilterCriteria());

    if (isEqual(currentFilter, nextFilter))
      return;

    untracked(() => this.onSubmit());
  });

  public async onSubmit() {
    let data: CategoryImportFilterModel | null = null;
    const isValid = submit(this.filterForm, async (_) => {
      data = this.filterForm().value();
    });

    if (!isValid || isNil(data))
      return;

    this.onFilterChange.emit(this.buildFilterCriteria());
  }

  public clear() {
    this.filterForm().reset({ searchTerm: '', status: 'all' });
    this.onSubmit();
  }

  private buildFilterCriteria(): GetAllCategoryImportBatchesRequest {
    const criteria = this.filterForm().value();
    const statusFilter = criteria.status && criteria.status !== 'all' ? criteria.status : null;
    return {
      ...this.filter(),
      searchTerm: criteria.searchTerm,
      filters: [
        ...this.filter().filters.filter(f => f.field !== 'status'),
        ...[
          buildEqualsFilterForValue('status', statusFilter)
        ].filter((filter): filter is ColumnFilter => filter !== null)
      ]
    };
  }
}

type CategoryImportFilterModel = {
  searchTerm: string;
  status: string | null;
};
