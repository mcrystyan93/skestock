import { Component, DestroyRef, effect, inject, input, untracked } from '@angular/core';
import { ColumnFilter, OrderListListItemDto, OrderListStatusChange } from '@ske/models';
import { OrderListDetailModal, OrderListListStore, Table } from '@ske/shared/order-lists';
import { ErrorAlert } from '@ske/shared/errors';
import { isNil } from 'lodash-es';
import { FilterContainer } from './filter/filter-container';
import { NzModalService } from 'ng-zorro-antd/modal';
import { NzMessageService } from 'ng-zorro-antd/message';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  imports: [ErrorAlert, FilterContainer, Table],
  providers: [OrderListListStore, NzModalService],
  selector: 'ske-school-class-overview-order-lists-tab',
  styles: ``,
  template: `
    <ske-order-list-filter-container class="block mb-4"
                                     (onCreate)="createOrderList()"/>

    <ske-error-display [problemDetail]="store.orderListsProblemDetail()"
                       [validationErrors]="store.orderListsValidationErrors()"
                       class="mb-2" />

    <div class="grow relative">
      <ske-order-list-table [items]="store.orderLists()"
                            [filter]="store.filter()"
                            [loading]="store.orderListsLoading()"
                            [hasNextPage]="store.hasNextPage()"
                            [isLoadingMore]="store.isLoadingMore()"
                            [statusChangingId]="store.statusChangingId()"
                            (onFilterChange)="store.load($event)"
                            (onLoadMore)="store.loadMore()"
                            (onView)="openOrderList($event)"
                            (onStatusChange)="changeStatus($event)" />
    </div>
  `,
  host: {
    class: 'flex flex-col grow absolute inset-0'
  }
})
export class OrderListsTab {
  public readonly classId = input.required<string | null>();

  public readonly store = inject(OrderListListStore);

  private readonly _nzModalService = inject(NzModalService);
  private readonly _nzMessageService = inject(NzMessageService);
  private readonly _destroyRef = inject(DestroyRef);

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
    const classId = this.classId();

    if (isNil(classId) || classId.trim().length === 0) {
      this._nzMessageService.error('Clasa nu este disponibilă pentru crearea comenzii.');
      return;
    }

    const modalRef = this._nzModalService.create({
      nzContent: OrderListDetailModal,
      nzData: {classId},
      nzWrapClassName: 'modal-90',
      nzCentered: true,
      nzMaskClosable: false
    });

    modalRef.afterClose
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe(() => this.store.reload());
  }

  public openOrderList(orderList: OrderListListItemDto) {
    const modalRef = this._nzModalService.create({
      nzContent: OrderListDetailModal,
      nzData: {
        id: orderList.id,
        classId: orderList.classId
      },
      nzWrapClassName: 'modal-90',
      nzCentered: true,
      nzMaskClosable: false
    });

    modalRef.afterClose
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe(() => this.store.reload());
  }

  public changeStatus(statusChange: OrderListStatusChange) {
    if (statusChange.action === 'submit') {
      this.store.changeStatus(statusChange);
      return;
    }

    const title = statusChange.action === 'cancel'
      ? 'Anulați comanda?'
      : 'Redeschideți comanda?';
    const content = statusChange.action === 'cancel'
      ? 'Comanda va fi păstrată în istoric și nu va mai putea fi trimisă până nu este redeschisă.'
      : 'Comanda va reveni în starea Ciornă și va putea fi modificată.';

    this._nzModalService.confirm({
      nzTitle: title,
      nzContent: content,
      nzOkText: statusChange.action === 'cancel' ? 'Anulează' : 'Redeschide',
      nzCancelText: 'Renunță',
      nzOnOk: () => this.store.changeStatus(statusChange)
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
