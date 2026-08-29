import { Component, inject } from '@angular/core';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { ThemeSwitcher } from '@ske/shared/theme';
import { LoginForm } from './form/login-form';
import { AuthStore } from '@ske/auth';
import { Credentials } from '@ske/models';

@Component({
  selector: 'ske-login-page',
  templateUrl: './login.page.html',
  imports: [
    NzCardComponent,
    ThemeSwitcher,
    LoginForm
  ],
  host: {
    class: 'flex h-screen w-screen flex-row'
  }
})
export class LoginPage {
  public readonly store = inject(AuthStore);

  public login(credentials:Credentials) {
    this.store.login(credentials);
  }
}
