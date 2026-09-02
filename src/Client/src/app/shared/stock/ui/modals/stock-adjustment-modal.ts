import { Component, DestroyRef, inject, signal, viewChild } from '@angular/core';
import { StockAdjustmentState, stockApiEvents } from '../../services/stock-adjustment.store';
import { AdjustStockRequest, StockItemDto } from '@ske/models';
import { NZ_MODAL_DATA, NzModalFooterDirective, NzModalRef, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { NzSpaceCompactComponent, NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { Form, StockAdjustmentFormModel } from './form';
import { isNil } from 'lodash-es';
import { Events } from '@ngrx/signals/events';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NzMessageService } from 'ng-zorro-antd/message';
import { BehaviorSubject, filter, switchMap, tap } from 'rxjs';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { DecimalPipe } from '@angular/common';
import { NzDropdownDirective, NzDropdownMenuComponent } from 'ng-zorro-antd/dropdown';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzMenuDirective, NzMenuItemComponent } from 'ng-zorro-antd/menu';
import { NzAvatarComponent } from 'ng-zorro-antd/avatar';

@Component({
  imports: [
    NzModalTitleDirective,
    NzModalFooterDirective,
    NzSpaceComponent,
    NzButtonComponent,
    Form,
    NzSpaceItemDirective,
    NzTypographyComponent,
    NzCardComponent,
    DecimalPipe,
    NzSpaceCompactComponent,
    NzDropdownDirective,
    NzDropdownMenuComponent,
    NzIconDirective,
    NzMenuDirective,
    NzMenuItemComponent,
    NzAvatarComponent
  ],
  selector: 'ske-stock-adjustment-modal',
  styles: ``,
  templateUrl: './stock-adjustment-modal.html',
  providers: [StockAdjustmentState]
})
export class StockAdjustmentModal {
  public readonly modalData = signal<StockAdjustmentModalData>(inject(NZ_MODAL_DATA));
  public readonly store = inject(StockAdjustmentState);

  private readonly _nzModalRef = inject(NzModalRef);
  private readonly _storeEvents = inject(Events);
  private readonly _formComponent = viewChild(Form);
  private readonly _destroyRef = inject(DestroyRef);
  private readonly _nzMessageService = inject(NzMessageService);
  private readonly _close$ = new BehaviorSubject(false);

  private readonly _adjustSuccessRef = this._storeEvents.on(stockApiEvents.adjustSuccess)
    .pipe(
      takeUntilDestroyed(this._destroyRef),
      tap(() => {
        this._nzMessageService.success('Stocul a fost ajustat cu succes!');
      }),
      switchMap(() => this._close$),
      filter((shouldClose) => shouldClose),
      tap(() => this.close())
    )
    .subscribe();

  public close() {
    this._nzModalRef.close();
  }

  public async save(shouldClose: boolean = true) {
    const formComponent = this._formComponent();

    if (isNil(formComponent))
      return;

    const { isValid, formData } = await formComponent.submit();

    if (!isValid || isNil(formData))
      return;

    this.store.adjustStock(this.mapAdjustRequest(formData));

    if(shouldClose)
      this._close$.next(true);
  }

  private mapAdjustRequest(formData: StockAdjustmentFormModel): AdjustStockRequest {
    const { classId, item } = this.modalData();

    return {
      classId,
      itemId: item.itemId,
      locationId: item.locationId,
      actualQuantity: formData.actualQuantity,
      reason: formData.reason
    };
  }
}

export type StockAdjustmentModalData = {
  classId: string;
  item: StockItemDto;
}
