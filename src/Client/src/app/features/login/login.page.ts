import { Component, inject } from '@angular/core';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { ThemeSwitcher } from '@ske/shared/theme';
import { LoginForm } from './form/login-form';
import { AuthStore } from '@ske/auth';
import { Credentials } from '@ske/models';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { ErrorAlert } from '@ske/shared/errors';

@Component({
  selector: 'ske-login-page',
  templateUrl: './login.page.html',
  imports: [
    NzCardComponent,
    ThemeSwitcher,
    LoginForm,
    NzIconDirective,
    ErrorAlert
  ],
  host: {
    class: 'flex h-screen w-screen flex-row items-center justify-center'
  }
})
export class LoginPage {
  public readonly store = inject(AuthStore);

  public login(credentials:Credentials) {
    this.store.login(credentials);
  }
}
