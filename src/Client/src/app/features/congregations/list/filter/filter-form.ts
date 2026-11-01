import { Component, effect, input, linkedSignal, output, untracked } from '@angular/core';
import { ColumnFilter, GetAllCongregationsRequest, getFilterValue } from '@ske/models';
import { debounce, form, FormField, submit } from '@angular/forms/signals';
import { isEqual, isNil } from 'lodash-es';
import { FormsModule } from '@angular/forms';
import { NzFormDirective } from 'ng-zorro-antd/form';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzInputDirective, NzInputPrefixDirective, NzInputWrapperComponent } from 'ng-zorro-antd/input';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzSegmentedComponent, NzSegmentedItemComponent } from 'ng-zorro-antd/segmented';

export type BooleanSegment = 'all' | 'yes' | 'no';

const BOOLEAN_FILTER_FIELDS = ['canReceiveStudents', 'isSmallCongregation'] as const;

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
    NzInputPrefixDirective,
    NzSegmentedComponent,
    NzSegmentedItemComponent
  ],
  selector: 'ske-congregation-filter-form',
  styles: ``,
  templateUrl: './filter-form.html'
})
export class FilterForm {
  public readonly loading = input.required<boolean>();
  public readonly filter = input.required<GetAllCongregationsRequest>();

  public readonly onFilterChange = output<GetAllCongregationsRequest>();

  public readonly segmentOptions: { label: string, value: BooleanSegment }[] = [
    { label: 'Toate', value: 'all' },
    { label: 'Da', value: 'yes' },
    { label: 'Nu', value: 'no' }
  ];

  private _initialFilterEmitted = false;
  private _initialFormChangeHandled = false;

  private readonly _formModel = linkedSignal({
    source: () => this.filter(),
    computation: (filter) => (<CongregationListFilterModel>{
      searchTerm: filter.searchTerm ?? '',
      canReceiveStudents: toSegment(getFilterValue<boolean>(filter.filters, 'canReceiveStudents')),
      isSmallCongregation: toSegment(getFilterValue<boolean>(filter.filters, 'isSmallCongregation'))
    })
  });

  public readonly congregationListFilterForm = form(this._formModel, (schemaPath) => {
    debounce(schemaPath.searchTerm, 300);
  });

  private readonly _initialFilterEffectRef = effect(() => {
    if (this._initialFilterEmitted)
      return;
    // Wait until required inputs are initialized, then trigger the first list load.
    this.filter();

    this.onFilterChange.emit(this.buildFilterCriteria());

    this._initialFilterEmitted = true;
  });

  private readonly _formChangeEffectRef = effect(() => {
    this.congregationListFilterForm().value();

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
    let data: CongregationListFilterModel | null = null;
    const isValid = submit(this.congregationListFilterForm, async () => {
      data = this.congregationListFilterForm().value();
    });

    if (!isValid || isNil(data))
      return;

    this.onFilterChange.emit(this.buildFilterCriteria());
  }

  public clear() {
    this.congregationListFilterForm().reset({
      searchTerm: '',
      canReceiveStudents: 'all',
      isSmallCongregation: 'all'
    });

    this.onSubmit();
  }

  private buildFilterCriteria(): GetAllCongregationsRequest {
    const criteria = this.congregationListFilterForm().value();

    return {
      ...this.filter(),
      searchTerm: criteria.searchTerm,
      filters: [
        ...this.filter().filters.filter((f) => !(BOOLEAN_FILTER_FIELDS as readonly string[]).includes(f.field)),
        ...BOOLEAN_FILTER_FIELDS
          .map((field) => buildBooleanFilter(field, criteria[field]))
          .filter((f): f is ColumnFilter => f !== null)
      ]
    };
  }
}

function toSegment(value: boolean | null): BooleanSegment {
  if (value === null)
    return 'all';

  return value ? 'yes' : 'no';
}

export function buildBooleanFilter(field: string, segment: BooleanSegment): ColumnFilter | null {
  if (segment === 'all')
    return null;

  return {
    field,
    operator: 'equals',
    value: segment === 'yes',
    fieldType: 'boolean',
    displayValue: segment === 'yes' ? 'Da' : 'Nu'
  };
}

type CongregationListFilterModel = {
  searchTerm: string;
  canReceiveStudents: BooleanSegment;
  isSmallCongregation: BooleanSegment;
}
