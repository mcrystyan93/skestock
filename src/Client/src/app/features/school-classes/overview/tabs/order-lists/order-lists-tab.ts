import {Component, effect, inject, input, untracked} from '@angular/core';
import {ColumnFilter} from '@ske/models';
import {OrderListDetailModal, OrderListListStore} from '@ske/shared/order-lists';
import {isNil} from 'lodash-es';
import {FilterContainer} from './filter/filter-container';
import {NzModalService} from 'ng-zorro-antd/modal';

@Component({
  imports: [FilterContainer],
  providers: [OrderListListStore, NzModalService],
  selector: 'ske-school-class-overview-order-lists-tab',
  styles: ``,
  template: `
    <ske-order-list-filter-container class="block mb-4"
                                     (onCreate)="createOrderList()"/>
  `,
  host: {
    class: 'flex min-w-0 flex-col grow'
  }
})
export class OrderListsTab {
  public readonly classId = input.required<string | null>();

  public readonly store = inject(OrderListListStore);

  private readonly _nzModalService = inject(NzModalService);

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

  public createOrderList() {
    this._nzModalService.create({
      nzContent: OrderListDetailModal,
      nzData: {classId: this.classId()},
      nzWrapClassName: 'modal-90',
      nzCentered: true,
      nzMaskClosable: false
    });
  }


  private getClassIdFilter(classId: string): ColumnFilter {
    return {
      value: classId,
      operator: 'equals',
      fieldType: 'number',
      field: 'classId'
    };
  }
}
