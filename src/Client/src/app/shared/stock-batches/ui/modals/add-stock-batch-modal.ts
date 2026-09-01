import { Component, DestroyRef, inject, signal, viewChild } from '@angular/core';
import { NZ_MODAL_DATA, NzModalFooterDirective, NzModalRef, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { Form, StockBatchFormModel } from './form';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzDropdownDirective, NzDropdownMenuComponent } from 'ng-zorro-antd/dropdown';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzMenuDirective, NzMenuItemComponent } from 'ng-zorro-antd/menu';
import { NzSpaceCompactComponent, NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { BehaviorSubject, filter, switchMap, tap } from 'rxjs';
import { stockBatchApiEvents, StockBatchStore } from '../../services/stock-batch.store';
import { isNil } from 'lodash-es';
import { CategoryDto, CreateStockBatchRequest, toDateOnlyString } from '@ske/models';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Events } from '@ngrx/signals/events';
import { NzMessageService } from 'ng-zorro-antd/message';

@Component({
  imports: [
    NzModalTitleDirective,
    Form,
    NzModalFooterDirective,
    NzButtonComponent,
    NzDropdownDirective,
    NzDropdownMenuComponent,
    NzIconDirective,
    NzMenuDirective,
    NzMenuItemComponent,
    NzSpaceCompactComponent,
    NzSpaceComponent,
    NzSpaceItemDirective
  ],
  selector: 'ske-add-stock-batch-modal',
  styles: ``,
  templateUrl: './add-stock-batch-modal.html',
  providers: [StockBatchStore]
})
export class AddStockBatchModal {
  public readonly modalData = signal<StockBatchDetailModalData>(inject(NZ_MODAL_DATA));
  public readonly store = inject(StockBatchStore);
  private readonly _close$ = new BehaviorSubject(false);
  private readonly _formComponent = viewChild(Form);
  private readonly _destroyRef = inject(DestroyRef);
  private readonly _nzModalRef = inject(NzModalRef);
  private readonly _nzMessageService = inject(NzMessageService);
  private readonly _storeEvents = inject(Events);

  private readonly _saveSuccessRef = this._storeEvents.on(stockBatchApiEvents.saveSuccess)
    .pipe(
      takeUntilDestroyed(this._destroyRef),
      tap(() => this._nzMessageService.success('Stocul a fost adăugat cu succes.')),
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

    this.store.save(this.mapSaveRequest(formData));

    if (shouldClose)
      this._close$.next(true);
  }

  private mapSaveRequest(formData: StockBatchFormModel): CreateStockBatchRequest {
    return {
      itemId: formData.item!.id,
      locationId: formData.location!.id,
      quantity: formData.quantity,
      receivedClassId: this.modalData().schoolClassId,
      receivedDate: toDateOnlyString(formData.receivedDate)!,
      unitPrice: formData.unitPrice,
      expiryDate: toDateOnlyString(formData.expiryDate ?? null)
    };
  }
}

type StockBatchDetailModalData = {
  schoolClassId: number;
  category: Partial<CategoryDto> | null;
}
