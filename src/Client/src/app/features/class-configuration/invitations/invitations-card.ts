import { Component, input, linkedSignal, output } from '@angular/core';
import { FormField, form, max, min, required, submit, validate } from '@angular/forms/signals';
import { NzCardComponent } from 'ng-zorro-antd/card';
import {
  NzFormControlComponent,
  NzFormDirective,
  NzFormItemComponent,
  NzFormLabelComponent,
} from 'ng-zorro-antd/form';
import { NzInputNumberComponent } from 'ng-zorro-antd/input-number';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { LoaderDirective } from '@ske/shared/loader';
import { MAX_INVITATION_COUNT } from '../services/configuration.constants';
import { SaveInvitationCountRequest } from '@ske/models';
import { NzButtonComponent } from 'ng-zorro-antd/button';

@Component({
  imports: [
    FormField,
    NzButtonComponent,
    LoaderDirective,
    NzCardComponent,
    NzFormControlComponent,
    NzFormDirective,
    NzFormItemComponent,
    NzFormLabelComponent,
    NzInputNumberComponent,
    NzTypographyComponent,
  ],
  selector: 'ske-configuration-invitations-card',
  templateUrl: './invitations-card.html'
})
export class InvitationsCard {
  public readonly invitationCount = input.required<number>();
  public readonly loading = input(false);
  public readonly onSave = output<void>();
  protected readonly maxInvitationCount = MAX_INVITATION_COUNT;

  private readonly _formModel = linkedSignal<SaveInvitationCountRequest>(() => ({
    invitationCount: this.invitationCount(),
  }));

  public readonly invitationsForm = form(this._formModel, (path) => {
    required(path.invitationCount, { message: 'Numărul de invitații este obligatoriu.' });
    validate(path.invitationCount, ({ value }) =>
      Number.isInteger(value())
        ? undefined
        : { kind: 'integer', message: 'Introduceți un număr întreg.' },
    );
    min(path.invitationCount, 0, { message: 'Numărul de invitații nu poate fi negativ.' });
    max(path.invitationCount, MAX_INVITATION_COUNT, {
      message: 'Numărul depășește limita acceptată.',
    });
  });

  public async submit(): Promise<InvitationsFormSubmit> {
    let formData: SaveInvitationCountRequest | null = null;
    const isValid = await submit(this.invitationsForm, async () => {
      formData = this.invitationsForm().value();
    });
    return { isValid, formData };
  }
}

export type InvitationsFormSubmit = {
  isValid: boolean;
  formData: SaveInvitationCountRequest | null;
};
