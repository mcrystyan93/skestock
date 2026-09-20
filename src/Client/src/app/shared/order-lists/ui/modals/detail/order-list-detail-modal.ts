import { Component, effect, inject, signal } from '@angular/core';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzDropdownDirective, NzDropdownMenuComponent } from 'ng-zorro-antd/dropdown';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzMenuDirective, NzMenuItemComponent } from 'ng-zorro-antd/menu';
import { NzSpaceCompactComponent, NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { OrderListDetailState } from '../../../services/order-list-detail.store';
import { ItemDto } from '@ske/models';
import { NZ_MODAL_DATA, NzModalFooterDirective, NzModalRef } from 'ng-zorro-antd/modal';
import { isNil } from 'lodash-es';
import { ErrorAlert } from '@ske/shared/errors';
import { OrderListDetailForm } from './form/order-list-detail-form';

@Component({
  imports: [
    NzButtonComponent,
    NzDropdownDirective,
    NzDropdownMenuComponent,
    NzIconDirective,
    NzMenuDirective,
    NzMenuItemComponent,
    NzSpaceCompactComponent,
    NzSpaceComponent,
    NzModalFooterDirective,
    NzSpaceItemDirective,
    ErrorAlert,
    OrderListDetailForm
  ],
  selector: 'ske-order-list-detail-modal',
  styles: ``,
  templateUrl: './order-list-detail-modal.html',
  providers: [OrderListDetailState]
})
export class OrderListDetailModal {
  public readonly modalData = signal<OrderListDetailModalData>(inject(NZ_MODAL_DATA));

  public readonly store = inject(OrderListDetailState);

  private readonly _nzModalRef = inject(NzModalRef);
  private initialLoad = false;

  private readonly _initialLoadEffectRef = effect(() => {
    if (this.initialLoad)
      return;

    const {id} = this.modalData();

    if (isNil(id))
      return;

    this.store.loadOrderList({id});
    this.initialLoad = true;
  });

  public close(savedItem: Partial<ItemDto> | null = null) {
    this._nzModalRef.close(savedItem);
  }

  public async save(shouldClose: boolean = true) {
  }
}

type OrderListDetailModalData = {
  id: string | null;
  classId: string;
}
