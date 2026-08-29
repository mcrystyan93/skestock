import { Component, input, output, signal } from '@angular/core';
import { Credentials } from '@ske/models';
import { email, form, FormField, required, submit } from '@angular/forms/signals';
import { NzFormControlComponent, NzFormDirective, NzFormItemComponent } from 'ng-zorro-antd/form';
import { FormsModule } from '@angular/forms';
import { isNil } from 'lodash-es';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { NzInputDirective, NzInputPasswordDirective, NzInputWrapperComponent } from 'ng-zorro-antd/input';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzButtonComponent } from 'ng-zorro-antd/button';

@Component({
  imports: [
    NzFormDirective,
    FormsModule,
    NzSpaceComponent,
    NzFormItemComponent,
    NzSpaceItemDirective,
    NzFormControlComponent,
    NzInputWrapperComponent,
    NzIconDirective,
    FormField,
    NzInputPasswordDirective,
    NzButtonComponent,
    NzInputDirective
  ],
  selector: 'ske-login-form',
  templateUrl: './login-form.html',
  host: {
    class: 'w-[400px] min-w-[280px] max-w-[75vw] block',
  },
})
export class LoginForm {
  public readonly loading = input.required<boolean>();

  public readonly onSubmit = output<Credentials>();

  private readonly _loginModel = signal<Credentials>({
    email: '',
    password: ''
  });

  public readonly loginForm = form(this._loginModel, (schema) => {
    required(schema.email, {
      message: 'Email este obligatoriu'
    });
    email(schema.email, {
      message: 'Email invalid'
    });
    required(schema.password, {
      message: 'Parola este obligatorie'
    });
  });

  public async submit() {
    let formData: Credentials | null = null;

    const isValid = await submit(this.loginForm, async (data) => {
      formData = this.loginForm().value();
    });

    if (!isValid)
      return;

    if(isNil(formData))
      return;

    this.onSubmit.emit(formData);
  }
}

export type LoginFormSubmit = {
  isValid: boolean;
  formData: Credentials | null;
};
