import { Component, effect, input, linkedSignal, output, untracked } from '@angular/core';
import { debounce, form, FormField, submit } from '@angular/forms/signals';
import { GetAllOrderListsRequest } from '@ske/models';
import { FormsModule } from '@angular/forms';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzInputDirective, NzInputPrefixDirective, NzInputWrapperComponent } from 'ng-zorro-antd/input';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzDividerComponent } from 'ng-zorro-antd/divider';
import { isEqual, isNil } from 'lodash-es';

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
    NzDividerComponent
  ],
  selector: 'ske-order-list-filter-form',
  styles: ``,
  templateUrl: './filter-form.html'
})
export class FilterForm {
  public readonly loading = input.required<boolean>();
  public readonly filter = input.required<GetAllOrderListsRequest>();

  public readonly onFilterChange = output<GetAllOrderListsRequest>();
  public readonly onCreate = output<void>();

  private _initialFormChangeHandled = false;

  private readonly _formModel = linkedSignal({
    source: () => this.filter(),
    computation: (filter): OrderListFilterModel => ({
      searchTerm: filter.searchTerm ?? ''
    })
  });

  public readonly orderListFilterForm = form(this._formModel, (schemaPath) => {
    debounce(schemaPath.searchTerm, 300);
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

    return {
      ...this.filter(),
      searchTerm: criteria.searchTerm
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
      searchTerm: ''
    });

    this.onSubmit();
  }
}

type OrderListFilterModel = {
  searchTerm: string;
};
