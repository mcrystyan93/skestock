import { Component, effect, input, linkedSignal, output, untracked } from '@angular/core';
import {
  buildEqualsFilterForValue,
  ColumnFilter,
  GetAllCategoryImportBatchesRequest,
  GetAllItemImportBatchesRequest,
  getFilterValue,
  ItemImportBatchStatus
} from '@ske/models';
import { debounce, form, FormField, submit } from '@angular/forms/signals';
import { FormsModule } from '@angular/forms';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzFormControlComponent, NzFormDirective, NzFormItemComponent } from 'ng-zorro-antd/form';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzInputDirective, NzInputPrefixDirective, NzInputWrapperComponent } from 'ng-zorro-antd/input';
import { isEqual, isNil } from 'lodash-es';
import { NzSegmentedComponent, NzSegmentedItemComponent } from 'ng-zorro-antd/segmented';
import { NzBadgeComponent } from 'ng-zorro-antd/badge';

@Component({
  imports: [FormsModule, NzFormDirective, NzFormItemComponent, NzFormControlComponent, NzRowDirective,
    NzColDirective, NzInputWrapperComponent, NzIconDirective, NzInputDirective, FormField,
    NzButtonComponent, NzInputPrefixDirective, NzSegmentedComponent, NzSegmentedItemComponent, NzBadgeComponent],
  selector: 'ske-item-import-filter-form',
  templateUrl: './item-import-filter-form.html'
})
export class ItemImportFilterForm {
  public readonly loading = input.required<boolean>();
  public readonly filter = input.required<GetAllItemImportBatchesRequest>();
  public readonly onFilterChange = output<GetAllItemImportBatchesRequest>();

  private _initialFilterEmitted = false;
  private _initialFormChangeHandled = false;

  private readonly _formModel = linkedSignal({
    source: () => this.filter(),
    computation: (filter) => (<ItemImportFilterModel>{
      searchTerm: filter.searchTerm ?? '',
      status: getFilterValue<string>(filter.filters, 'status') ?? 'all'
    })
  });

  public readonly statusSegmentOptions: { label: string, value: ItemImportBatchStatus }[] = [
    { label: 'Toate', value: 'all' },
    { label: 'Procesare', value: 'processing' },
    { label: 'Asteptare', value: 'pendingReview' },
    { label: 'Confirmate', value: 'confirmed' },
    { label: 'Esuate', value: 'failed' }
  ];

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
    let data: ItemImportFilterModel | null = null;
    const valid = await submit(this.filterForm, async () => {
      data = this.filterForm().value();
    });

    if (!valid || isNil(data))
      return;

    this.onFilterChange.emit(this.buildFilterCriteria());
  }

  public clear() {
    this.filterForm().reset({ searchTerm: '', status: 'all' });
    void this.onSubmit();
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

type ItemImportFilterModel = {
  searchTerm: string;
  status: string | null;
};
