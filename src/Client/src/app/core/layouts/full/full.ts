import { Component } from '@angular/core';
import { NzContentComponent, NzLayoutComponent, NzSiderComponent } from 'ng-zorro-antd/layout';
import { NzMenuDirective, NzMenuItemComponent } from 'ng-zorro-antd/menu';
import { RouterLink, RouterOutlet } from '@angular/router';
import { Header } from './header/header';

@Component({
  imports: [
    NzLayoutComponent,
    NzSiderComponent,
    NzMenuDirective,
    NzMenuItemComponent,
    RouterLink,
    NzContentComponent,
    RouterOutlet,
    Header
  ],
  selector: 'ske-full',
  styles: ``,
  templateUrl: './full.html',
})
export class Full {}
