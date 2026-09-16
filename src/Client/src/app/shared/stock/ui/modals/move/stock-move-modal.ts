import { Component, DestroyRef, inject, signal, viewChild } from '@angular/core';
import { Events } from '@ngrx/signals/events';
import { NZ_MODAL_DATA, NzModalFooterDirective, NzModalRef, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { DecimalPipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter, tap } from 'rxjs';
import { ErrorAlert } from '@ske/shared/errors';
import { MoveStockRequest, StockItemDto } from '@ske/models';
import { StockMoveState, stockMoveApiEvents } from '../../../services/stock-move.store';
import { StockMoveForm, StockMoveFormModel } from './stock-move-form';

@Component({
  imports: [
    NzModalTitleDirective,
    NzModalFooterDirective,
    NzButtonComponent,
    NzCardComponent,
    NzSpaceComponent,
    NzSpaceItemDirective,
    NzTypographyComponent,
    NzIconDirective,
    DecimalPipe,
    ErrorAlert,
    StockMoveForm
  ],
  selector: 'ske-stock-move-modal',
  styles: ``,
  templateUrl: './stock-move-modal.html',
  providers: [StockMoveState]
})
export class StockMoveModal {
  public readonly modalData = signal<StockMoveModalData>(inject(NZ_MODAL_DATA));
  public readonly store = inject(StockMoveState);

  private readonly _nzModalRef = inject(NzModalRef);
  private readonly _storeEvents = inject(Events);
  private readonly _formComponent = viewChild(StockMoveForm);
  private readonly _destroyRef = inject(DestroyRef);
  private readonly _nzMessageService = inject(NzMessageService);

  private readonly _moveSuccessRef = this._storeEvents.on(stockMoveApiEvents.moveSuccess)
    .pipe(
      takeUntilDestroyed(this._destroyRef),
      filter(() => this.store.completed()),
      tap(() => {
        this._nzMessageService.success('Stocul a fost mutat cu succes!');
        this.close();
      })
    )
    .subscribe();

  public close() {
    this._nzModalRef.close();
  }

  public async save() {
    const formComponent = this._formComponent();

    if (!formComponent) {
      return;
    }

    const { isValid, formData } = await formComponent.submit();

    if (!isValid || !formData?.destination?.id) {
      return;
    }

    this.store.moveStock(this.mapMoveRequest(formData));
  }

  private mapMoveRequest(formData: StockMoveFormModel): MoveStockRequest {
    const { classId, item } = this.modalData();

    return {
      classId,
      itemId: item.itemId,
      sourceLocationId: item.locationId,
      destinationLocationId: formData.destination!.id,
      quantity: formData.quantity
    };
  }
}

export type StockMoveModalData = {
  classId: string;
  item: StockItemDto;
};
