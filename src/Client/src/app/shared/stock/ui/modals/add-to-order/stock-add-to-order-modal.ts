import { Component, computed, DestroyRef, inject, linkedSignal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, FormField, max, min, required, submit, validate } from '@angular/forms/signals';
import { Events } from '@ngrx/signals/events';
import { NZ_MODAL_DATA, NzModalFooterDirective, NzModalRef, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzFormControlComponent, NzFormDirective, NzFormItemComponent, NzFormLabelComponent } from 'ng-zorro-antd/form';
import { NzInputDirective } from 'ng-zorro-antd/input';
import { NzInputNumberComponent } from 'ng-zorro-antd/input-number';
import { NzOptionComponent, NzSelectComponent } from 'ng-zorro-antd/select';
import { NzSkeletonComponent } from 'ng-zorro-antd/skeleton';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { NzMessageService } from 'ng-zorro-antd/message';
import { OrderListDto, StockItemDto } from '@ske/models';
import { ErrorAlert } from '@ske/shared/errors';
import { StockAddToOrderState, stockAddToOrderApiEvents } from '../../../services/stock-add-to-order.store';

const NEW_ORDER = 'new';
const NAME_MAX_LENGTH = 200;

@Component({
  imports: [
    ErrorAlert,
    FormField,
    NzModalTitleDirective,
    NzModalFooterDirective,
    NzButtonComponent,
    NzFormControlComponent,
    NzFormDirective,
    NzFormItemComponent,
    NzFormLabelComponent,
    NzInputDirective,
    NzInputNumberComponent,
    NzOptionComponent,
    NzSelectComponent,
    NzSkeletonComponent,
    NzTypographyComponent
  ],
  selector: 'ske-stock-add-to-order-modal',
  templateUrl: './stock-add-to-order-modal.html',
  providers: [StockAddToOrderState]
})
export class StockAddToOrderModal {
  public readonly modalData = inject<StockAddToOrderModalData>(NZ_MODAL_DATA);
  public readonly store = inject(StockAddToOrderState);
  protected readonly newOrderValue = NEW_ORDER;

  private readonly _nzModalRef = inject(NzModalRef<StockAddToOrderModal, OrderListDto>);
  private readonly _message = inject(NzMessageService);
  private readonly _events = inject(Events);
  private readonly _destroyRef = inject(DestroyRef);

  // Preselects the first draft once the list arrives; falls back to creating a new order.
  private readonly _model = linkedSignal<AddToOrderFormModel>(() => ({
    target: this.store.drafts()[0]?.id ?? NEW_ORDER,
    newName: '',
    quantity: 1
  }));

  protected readonly addForm = form(this._model, (path) => {
    required(path.quantity, { message: 'Cantitatea este obligatorie.' });
    min(path.quantity, 0.01, { message: 'Cantitatea trebuie să fie mai mare decât 0.' });
    max(path.quantity, 1_000_000, { message: 'Cantitatea este prea mare.' });
    validate(path.newName, (ctx) => {
      if (ctx.valueOf(path.target) !== NEW_ORDER) {
        return undefined;
      }

      const name = ctx.value().trim();

      if (!name) {
        return { kind: 'required', message: 'Numele comenzii este obligatoriu.' };
      }

      return name.length > NAME_MAX_LENGTH
        ? { kind: 'maxLength', message: `Numele nu poate depăși ${NAME_MAX_LENGTH} de caractere.` }
        : undefined;
    });
  });

  protected readonly isNewOrder = computed(() => this._model().target === NEW_ORDER);

  private readonly _addSuccessRef = this._events.on(stockAddToOrderApiEvents.addSuccess)
    .pipe(takeUntilDestroyed(this._destroyRef))
    .subscribe(({ payload: orderList }) => {
      this._message.success(
        `„${this.modalData.item.itemName}” a fost adăugat în comanda „${orderList.name ?? 'fără nume'}”.`
      );
      this._nzModalRef.close(orderList);
    });

  constructor() {
    this.store.loadDrafts(this.modalData.classId);
  }

  protected close(): void {
    this._nzModalRef.close();
  }

  protected async save(): Promise<void> {
    await submit(this.addForm, async () => {
      const { classId, item } = this.modalData;
      const { target, newName, quantity } = this._model();
      const isNew = target === NEW_ORDER;

      this.store.addItem({
        classId,
        itemId: item.itemId,
        quantity,
        orderListId: isNew ? null : target,
        newOrderListName: isNew ? newName.trim() : null
      });
    });
  }
}

type AddToOrderFormModel = {
  target: string;
  newName: string;
  quantity: number;
};

export type StockAddToOrderModalData = {
  classId: string;
  item: StockItemDto;
};
