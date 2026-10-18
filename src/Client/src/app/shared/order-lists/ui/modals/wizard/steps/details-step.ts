import { Component, effect, input, linkedSignal, output, untracked } from '@angular/core';
import { form } from '@angular/forms/signals';
import { NzFormDirective } from 'ng-zorro-antd/form';
import {
  OrderListDetailsFields,
  OrderListDetailsModel,
  validateOrderListDetails
} from '../../detail/form/details/order-list-details-fields';

@Component({
  imports: [NzFormDirective, OrderListDetailsFields],
  selector: 'ske-order-list-details-step',
  template: `
    <form nz-form
          nzNoColon
          nzLayout="vertical">
      <h3 class="mb-3 text-base font-medium">Cum se numește comanda?</h3>
      <ske-order-list-details-fields [name]="detailsForm.name"
                                     [note]="detailsForm.note" />
    </form>
  `
})
export class OrderListDetailsStep {
  public readonly name = input('');
  public readonly note = input('');
  public readonly detailsChange = output<OrderListDetailsModel>();

  private readonly _model = linkedSignal<OrderListDetailsModel>(() => ({ name: this.name(), note: this.note() }));

  protected readonly detailsForm = form(this._model, validateOrderListDetails);

  private readonly _emitDetails = effect(() => {
    const details = this._model();

    untracked(() => {
      if (details.name !== this.name() || details.note !== this.note())
        this.detailsChange.emit(details);
    });
  });
}
