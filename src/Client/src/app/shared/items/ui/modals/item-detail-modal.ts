import { Component, DestroyRef, effect, inject, signal, viewChild } from '@angular/core';
import { itemApiEvents, ItemDetailState, NEW_ITEM_ROUTE_ID } from '../../services/item-detail.store';
import { CreateItemRequest, EditItemRequest, ItemDto } from '@ske/models';
import { NZ_MODAL_DATA, NzModalFooterDirective, NzModalRef, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { NzSpaceCompactComponent, NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { Form, ItemFormModel } from './form';
import { isNil } from 'lodash-es';
import { Events } from '@ngrx/signals/events';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NzMessageService } from 'ng-zorro-antd/message';
import { BehaviorSubject, filter, switchMap, tap } from 'rxjs';
import { NzDropdownDirective, NzDropdownMenuComponent } from 'ng-zorro-antd/dropdown';
import { NzMenuDirective, NzMenuItemComponent } from 'ng-zorro-antd/menu';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { ErrorAlert } from '@ske/shared/errors';

@Component({
  imports: [
    NzModalTitleDirective,
    NzModalFooterDirective,
    NzSpaceComponent,
    NzButtonComponent,
    Form,
    NzSpaceItemDirective,
    NzSpaceCompactComponent,
    NzDropdownMenuComponent,
    NzMenuDirective,
    NzMenuItemComponent,
    NzIconDirective,
    NzDropdownDirective,
    ErrorAlert
  ],
  selector: 'ske-item-detail-modal',
  styles: ``,
  templateUrl: './item-detail-modal.html',
  providers: [ItemDetailState]
})
export class ItemDetailModal {
  public readonly modalData = signal<ItemDetailModalData>(inject(NZ_MODAL_DATA));
  public readonly store = inject(ItemDetailState);

  private initialLoad = false;

  private readonly _nzModalRef = inject(NzModalRef);
  private readonly _storeEvents = inject(Events);
  private readonly _formComponent = viewChild(Form);
  private readonly _destroyRef = inject(DestroyRef);
  private readonly _nzMessageService = inject(NzMessageService);
  private readonly _close$ = new BehaviorSubject(false);

  private readonly _initialLoadEffectRef = effect(() => {
    if (this.initialLoad)
      return;

    const { item, prefill } = this.modalData();

    this.store.loadItem({ id: item?.id ?? NEW_ITEM_ROUTE_ID, prefill: item ? null : prefill });
    this.initialLoad = true;
  });

  private readonly _saveSuccessRef = this._storeEvents.on(itemApiEvents.saveSuccess)
    .pipe(
      takeUntilDestroyed(this._destroyRef),
      tap(() => this._nzMessageService.success('Articolul a fost salvat cu succes!')),
      switchMap(() => this._close$),
      filter((shouldClose) => shouldClose),
      tap(() => this.close(this.store.item()))
    )
    .subscribe();


  public close(savedItem: Partial<ItemDto> | null = null) {
    this._nzModalRef.close(savedItem);
  }

  public async save(shouldClose: boolean = true) {
    const formComponent = this._formComponent();

    if (isNil(formComponent))
      return;

    const { isValid, formData } = await formComponent.submit();

    if (!isValid || isNil(formData))
      return;

    this.store.saveItem(this.mapSaveRequest(formData));

    if(shouldClose)
      this._close$.next(true);
  }

  private mapSaveRequest(formData: ItemFormModel): CreateItemRequest | EditItemRequest {
    return {
      sku: formData.sku || null,
      name: formData.name,
      description: formData.description || null,
      unit: formData.unit,
      minThreshold: formData.minThreshold,
      isPerishable: formData.isPerishable,
      shelfLifeDays: formData.isPerishable ? (formData.shelfLifeDays ?? null) : null,
      categoryId: formData.category?.id as string
    };
  }
}

type ItemDetailModalData = {
  item: ItemDto | null;
  prefill?: Partial<ItemDto> | null;
}
