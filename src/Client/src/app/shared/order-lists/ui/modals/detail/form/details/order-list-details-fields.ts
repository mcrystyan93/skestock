import { Component, input } from '@angular/core';
import { FieldTree, FormField, maxLength, required, SchemaPathTree } from '@angular/forms/signals';
import { SkeletonInputLoaderDirective } from '@ske/shared/loader';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzFormControlComponent, NzFormItemComponent, NzFormLabelComponent } from 'ng-zorro-antd/form';
import { NzInputDirective, NzInputWrapperComponent, NzTextareaCountComponent } from 'ng-zorro-antd/input';
import { MAX_ORDER_LIST_NAME_LENGTH, MAX_ORDER_LIST_NOTE_LENGTH } from '../../../../../services/order-list-wizard.utils';

export type OrderListDetailsModel = {
  name: string;
  note: string;
};

export function validateOrderListDetails(path: SchemaPathTree<OrderListDetailsModel>) {
  required(path.name, {
    message: 'Numele listei de comenzi este obligatoriu.'
  });
  maxLength(path.name, MAX_ORDER_LIST_NAME_LENGTH, {
    message: `Numele listei de comenzi nu poate depăși ${MAX_ORDER_LIST_NAME_LENGTH} de caractere.`
  });
  maxLength(path.note, MAX_ORDER_LIST_NOTE_LENGTH, {
    message: `Nota listei de comenzi nu poate depăși ${MAX_ORDER_LIST_NOTE_LENGTH} de caractere.`
  });
}

/** The name and note inputs, shared by the wizard's details step and the order list editor. */
@Component({
  imports: [
    FormField,
    NzColDirective,
    NzFormControlComponent,
    NzFormItemComponent,
    NzFormLabelComponent,
    NzInputDirective,
    NzInputWrapperComponent,
    NzRowDirective,
    NzTextareaCountComponent,
    SkeletonInputLoaderDirective
  ],
  selector: 'ske-order-list-details-fields',
  template: `
    @let nameField = name();
    @let noteField = note();
    <nz-row [nzGutter]="[16, 0]">
      <nz-col [nzLg]="12"
              [nzXs]="24">
        <nz-form-item nzLayout="vertical"
                      class="mb-2!"
                      *skeletonInputLoader="loading()">
          <nz-form-label nzRequired
                         nzSpan="24">Nume
          </nz-form-label>
          <nz-form-control [nzErrorTip]="nameErrorTemplate"
                           nzSpan="24">
            <nz-input-wrapper nzShowCount
                              [nzCount]="{ max: maxNameLength }">
              <input type="text"
                     [formField]="nameField"
                     placeholder="Numele comenzii"
                     nz-input>
            </nz-input-wrapper>
          </nz-form-control>
          <ng-template #nameErrorTemplate>
            @for (error of nameField().errors(); track error.kind) {
              <div class="ant-form-item-explain-error">
                {{ error.message }}
              </div>
            }
          </ng-template>
        </nz-form-item>
      </nz-col>
      <nz-col [nzLg]="12"
              [nzXs]="24">
        <nz-form-item *skeletonInputLoader="loading()"
                      class="mb-2!">
          <nz-form-label nzSpan="24">Nota</nz-form-label>
          <nz-form-control [nzErrorTip]="noteErrorTemplate">
            <nz-textarea-count [nzMaxCharacterCount]="maxNoteLength">
              <textarea nz-input
                        [formField]="noteField"
                        placeholder="Notite despre comanda"
                        rows="1"></textarea>
            </nz-textarea-count>
          </nz-form-control>
          <ng-template #noteErrorTemplate>
            @for (error of noteField().errors(); track error.kind) {
              <div class="ant-form-item-explain-error">
                {{ error.message }}
              </div>
            }
          </ng-template>
        </nz-form-item>
      </nz-col>
    </nz-row>
  `
})
export class OrderListDetailsFields {
  public readonly name = input.required<FieldTree<string>>();
  public readonly note = input.required<FieldTree<string>>();
  public readonly loading = input(false);

  protected readonly maxNameLength = MAX_ORDER_LIST_NAME_LENGTH;
  protected readonly maxNoteLength = MAX_ORDER_LIST_NOTE_LENGTH;
}
