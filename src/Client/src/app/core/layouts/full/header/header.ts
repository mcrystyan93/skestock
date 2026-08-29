import { Component } from '@angular/core';
import { NzHeaderComponent } from 'ng-zorro-antd/layout';
import { ThemeSwitcher } from '@ske/shared/theme';

@Component({
  imports: [
    NzHeaderComponent,
    ThemeSwitcher
  ],
  selector: 'ske-header',
  styles: ``,
  templateUrl: './header.html',
})
export class Header {}
