import { Component, effect, inject, input, untracked } from '@angular/core';
import { ColumnFilter } from '@ske/models';
import { OrderListListStore } from '@ske/shared/order-lists';
import { isNil } from 'lodash-es';
import { FilterContainer } from './filter/filter-container';

@Component({
  imports: [FilterContainer],
  providers: [OrderListListStore],
  selector: 'ske-school-class-overview-order-lists-tab',
  styles: ``,
  template: `
    <ske-order-list-filter-container class="block mb-4" />
  `,
  host: {
    class: 'flex min-w-0 flex-col grow'
  }
})
export class OrderListsTab {
  public readonly classId = input.required<string | null>();
  public readonly store = inject(OrderListListStore);

  private readonly _classIdEffectRef = effect(() => {
    const classId = this.classId();

    if (isNil(classId))
      return;

    untracked(() => {
      this.store.load({
        ...this.store.filter(),
        filters: [this.getClassIdFilter(classId)]
      });
    });
  });

  private getClassIdFilter(classId: string): ColumnFilter {
    return {
      value: classId,
      operator: 'equals',
      fieldType: 'number',
      field: 'classId'
    };
  }
}
