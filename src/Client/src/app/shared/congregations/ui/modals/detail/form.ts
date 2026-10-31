import { Component, input, linkedSignal } from '@angular/core';
import { CongregationDto } from '@ske/models';
import { email, form, FormField, maxLength, required, submit } from '@angular/forms/signals';
import { NzFormControlComponent, NzFormDirective, NzFormItemComponent, NzFormLabelComponent } from 'ng-zorro-antd/form';
import { SkeletonInputLoaderDirective } from '@ske/shared/loader';
import { NzInputDirective, NzInputWrapperComponent, NzTextareaCountComponent } from 'ng-zorro-antd/input';
import { NzSwitchComponent } from 'ng-zorro-antd/switch';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';

@Component({
  imports: [
    NzFormDirective,
    SkeletonInputLoaderDirective,
    NzFormItemComponent,
    NzFormLabelComponent,
    NzFormControlComponent,
    NzInputWrapperComponent,
    NzTextareaCountComponent,
    FormField,
    NzInputDirective,
    NzSwitchComponent,
    NzRowDirective,
    NzColDirective
  ],
  selector: 'ske-congregation-form',
  styles: ``,
  templateUrl: './form.html'
})
export class CongregationForm {
  public readonly loading = input.required<boolean>();
  public readonly congregation = input.required<Partial<CongregationDto>>();
  /** Messages returned by the server for a field (keyed by the camelCase form field name), shown under it. */
  public readonly serverErrors = input<Record<string, string[]>>({});

  private readonly _formModel = linkedSignal({
    source: () => this.congregation(),
    computation: (congregation) => (<CongregationFormModel>{
      id: congregation.id ?? null,
      name: congregation.name ?? '',
      email: congregation.email ?? '',
      coordinator: congregation.coordinator ?? '',
      coordinatorPhone: congregation.coordinatorPhone ?? '',
      dessertContactPerson: congregation.dessertContactPerson ?? '',
      dessertContactPhone: congregation.dessertContactPhone ?? '',
      canReceiveStudents: congregation.canReceiveStudents ?? false,
      isSmallCongregation: congregation.isSmallCongregation ?? false,
      message: congregation.message ?? ''
    })
  });

  // Keep the max lengths in sync with CreateCongregationCommandValidator.
  public readonly congregationForm = form(this._formModel, (schemaPath) => {
    required(schemaPath.name, { message: 'Numele congregației este obligatoriu.' });
    maxLength(schemaPath.name, 100, { message: 'Numele congregației nu poate depăși 100 de caractere.' });
    required(schemaPath.email, { message: 'Emailul este obligatoriu.' });
    email(schemaPath.email, { message: 'Emailul nu este valid.' });
    maxLength(schemaPath.email, 200, { message: 'Emailul nu poate depăși 200 de caractere.' });
    maxLength(schemaPath.coordinator, 100, { message: 'Numele coordonatorului nu poate depăși 100 de caractere.' });
    maxLength(schemaPath.coordinatorPhone, 30, { message: 'Telefonul nu poate depăși 30 de caractere.' });
    maxLength(schemaPath.dessertContactPerson, 100, { message: 'Numele persoanei de contact nu poate depăși 100 de caractere.' });
    maxLength(schemaPath.dessertContactPhone, 30, { message: 'Telefonul nu poate depăși 30 de caractere.' });
    maxLength(schemaPath.message, 1000, { message: 'Mesajul nu poate depăși 1000 de caractere.' });
  });

  public async submit(): Promise<CongregationFormSubmit> {
    let formData: CongregationFormModel | null = null;
    const isValid = await submit(this.congregationForm, async () => {
      formData = this.congregationForm().value();
    });

    return { isValid, formData };
  }
}

export type CongregationFormModel = {
  id: string | null;
  name: string;
  email: string;
  coordinator: string;
  coordinatorPhone: string;
  dessertContactPerson: string;
  dessertContactPhone: string;
  canReceiveStudents: boolean;
  isSmallCongregation: boolean;
  message: string;
};
export type CongregationFormSubmit = {
  isValid: boolean;
  formData: CongregationFormModel | null;
}
