import { Component, effect, input, linkedSignal, output, untracked } from '@angular/core';
import {
  buildEqualsFilterForValue,
  ClassStatus,
  ColumnFilter,
  GetAllSchoolClassesRequest,
  getFilterValue
} from '@ske/models';
import { debounce, form, FormField, submit } from '@angular/forms/signals';
import { isEqual, isNil } from 'lodash-es';
import { FormsModule } from '@angular/forms';
import { NzFormDirective } from 'ng-zorro-antd/form';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzInputDirective, NzInputWrapperComponent } from 'ng-zorro-antd/input';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzSegmentedComponent, NzSegmentedItemComponent } from 'ng-zorro-antd/segmented';

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
    NzSegmentedItemComponent
  ],
  selector: 'ske-school-class-filter-form',
  styles: ``,
  templateUrl: './filter-form.html'
})
export class FilterForm {
  public readonly loading = input.required<boolean>();
  public readonly filter = input.required<GetAllSchoolClassesRequest>();

  public readonly onFilterChange = output<GetAllSchoolClassesRequest>();
  private _initialFilterEmitted = false;
  private _initialFormChangeHandled = false;

  private readonly _formModel = linkedSignal({
    source: () => this.filter(),
    computation: (filter) => (<SchoolClassListFilterModel>{
      searchTerm: filter.searchTerm ?? '',
      status: getFilterValue<ClassStatus>(filter.filters, 'status') ?? ClassStatus.All
    })
  });

  public readonly statusSegmentOptions: { label: string, value: ClassStatus }[] = [
    { label: 'Toate', value: ClassStatus.All },
    { label: 'Activa', value: ClassStatus.Active },
    { label: 'Viitoare', value: ClassStatus.Upcoming },
    { label: 'Finalizata', value: ClassStatus.Closed },
    { label: 'Intrerupta', value: ClassStatus.Paused }
  ];

  public readonly schoolClassListFilterForm = form(this._formModel, (schemaPath) => {
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

  private readonly _formEffectChange = effect(() => {
    this.schoolClassListFilterForm().value();

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
    let data: SchoolClassListFilterModel | null = null;
    let isValid = submit(this.schoolClassListFilterForm, async (_) => {
      data = this.schoolClassListFilterForm().value();
    });

    if (!isValid || isNil(data))
      return;

    this.onFilterChange.emit(this.buildFilterCriteria());
  }

  public clear() {
    this.schoolClassListFilterForm().reset({
      searchTerm: '',
      status: ClassStatus.All
    });

    this.onSubmit();
  }

  private buildFilterCriteria(): GetAllSchoolClassesRequest {
    const criteria = this.schoolClassListFilterForm().value();
    const statusFilter = criteria.status && criteria.status !== ClassStatus.All ? criteria.status : null;
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

type SchoolClassListFilterModel = {
  searchTerm: string;
  status: ClassStatus;
}
