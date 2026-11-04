import { Component, computed, effect, input, linkedSignal, output, untracked } from '@angular/core';
import { ColumnFilter, GetAllCongregationsRequest, getFilterValue } from '@ske/models';
import { debounce, form, FormField } from '@angular/forms/signals';
import { isEqual } from 'lodash-es';
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

  // Only the fields the form edits: a reload replaces the filter object (new cursor/sort), and mirroring
  // that into the form would reset what the user typed while the request was in flight.
  private readonly _editableFilter = computed(() => {
    const filter = this.filter();

    return {
      searchTerm: filter.searchTerm ?? '',
      canReceiveStudents: toSegment(getFilterValue<boolean>(filter.filters, 'canReceiveStudents')),
      isSmallCongregation: toSegment(getFilterValue<boolean>(filter.filters, 'isSmallCongregation'))
    } satisfies CongregationListFilterModel;
  }, { equal: isEqual });

  private readonly _formModel = linkedSignal<CongregationListFilterModel>(() => ({ ...this._editableFilter() }));

  public readonly congregationListFilterForm = form(this._formModel, (schemaPath) => {
    debounce(schemaPath.searchTerm, 300);
  });

  private readonly _formChangeEffectRef = effect(() => {
    this.congregationListFilterForm().value();

    // Also runs once on init, where the form mirrors the filter and nothing is emitted.
    const nextFilter = untracked(() => this.buildFilterCriteria());

    if (!isEqual(untracked(() => this.filter()), nextFilter))
      this.onFilterChange.emit(nextFilter);
  });

  public clear() {
    this.congregationListFilterForm().reset({
      searchTerm: '',
      canReceiveStudents: 'all',
      isSmallCongregation: 'all'
    });
  }

  private buildFilterCriteria(): GetAllCongregationsRequest {
    const criteria = this.congregationListFilterForm().value();

    return {
      ...this.filter(),
      searchTerm: criteria.searchTerm || null,
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
