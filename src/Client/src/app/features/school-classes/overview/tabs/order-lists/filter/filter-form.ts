import { Component, computed, effect, input, linkedSignal, output, signal, untracked } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { debounce, form, FormField, submit } from '@angular/forms/signals';
import {
  buildEqualsFilterForValue,
  ColumnFilter,
  GetAllOrderListsRequest,
  getFilterValue,
  OrderListStatus
} from '@ske/models';
import { FormsModule } from '@angular/forms';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzInputDirective, NzInputPrefixDirective, NzInputWrapperComponent } from 'ng-zorro-antd/input';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { isEqual, isNil } from 'lodash-es';
import { NzFormDirective } from 'ng-zorro-antd/form';
import { NzSegmentedComponent, NzSegmentedItemComponent } from 'ng-zorro-antd/segmented';
import { LayoutBreakpoint } from '@ske/shared/directives';
import { NzBadgeComponent } from 'ng-zorro-antd/badge';
import { NzDrawerComponent, NzDrawerContentDirective } from 'ng-zorro-antd/drawer';
import { NzOptionComponent, NzSelectComponent } from 'ng-zorro-antd/select';

@Component({
  imports: [
    FormsModule,
    NzRowDirective,
    NzColDirective,
    FormField,
    NzInputWrapperComponent,
    NzIconDirective,
    NzInputPrefixDirective,
    NzInputDirective,
    NzButtonComponent,
    NzFormDirective,
    NzSegmentedComponent,
    NzSegmentedItemComponent,
    NgTemplateOutlet,
    LayoutBreakpoint,
    NzBadgeComponent,
    NzDrawerComponent,
    NzDrawerContentDirective,
    NzOptionComponent,
    NzSelectComponent
  ],
  selector: 'ske-order-list-filter-form',
  styles: ``,
  templateUrl: './filter-form.html'
})
export class FilterForm {
  public readonly loading = input.required<boolean>();
  public readonly filter = input.required<GetAllOrderListsRequest>();

  public readonly onFilterChange = output<GetAllOrderListsRequest>();
  public readonly filtersOpen = signal(false);

  private _initialFormChangeHandled = false;

  private readonly _formModel = linkedSignal({
    source: () => this.filter(),
    computation: (filter): OrderListFilterModel => ({
      searchTerm: filter.searchTerm ?? '',
      status: getFilterValue<OrderListStatus>(filter.filters, 'status') ?? 'All'
    })
  });

  public readonly statusSegmentOptions: { label: string, value: OrderListStatus }[] = [
    { label: 'Toate', value: 'All' },
    { label: 'Ciorne', value: 'Draft' },
    { label: 'Finalizate', value: 'Submitted' },
    { label: 'Anulate', value: 'Cancelled' }
  ];

  public readonly orderListFilterForm = form(this._formModel, (schemaPath) => {
    debounce(schemaPath.searchTerm, 300);
  });

  public readonly activeFilterCount = computed(() =>
    this.orderListFilterForm().value().status === 'All' ? 0 : 1
  );

  public readonly filtersButtonLabel = computed(() => {
    const count = this.activeFilterCount();

    return count === 0 ? 'Filtre' : 'Filtre, 1 filtru activ';
  });

  private readonly _formEffectChange = effect(() => {
    this.orderListFilterForm().value();

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

  private buildFilterCriteria(): GetAllOrderListsRequest {
    const criteria = this.orderListFilterForm().value();
    const statusFilter = criteria.status && criteria.status !== 'All' ? criteria.status : null;
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

  public async onSubmit() {
    let data: OrderListFilterModel | null = null;
    const isValid = submit(this.orderListFilterForm, async (_) => {
      data = this.orderListFilterForm().value();
    });

    if (!isValid || isNil(data))
      return;

    this.onFilterChange.emit(this.buildFilterCriteria());
  }

  public clear() {
    this.orderListFilterForm().reset({
      searchTerm: '',
      status: 'All'
    });

    this.onSubmit();
  }
}

type OrderListFilterModel = {
  searchTerm: string;
  status: OrderListStatus;
};
