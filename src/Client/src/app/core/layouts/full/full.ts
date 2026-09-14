import { Component, signal } from '@angular/core';
import { NzContentComponent, NzLayoutComponent, NzSiderComponent } from 'ng-zorro-antd/layout';
import { NzMenuDirective, NzMenuItemComponent } from 'ng-zorro-antd/menu';
import { RouterLink, RouterOutlet } from '@angular/router';
import { Header } from './header/header';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NgTemplateOutlet } from '@angular/common';
import { LayoutBreakpoint } from '@ske/shared/directives';
import { NzDrawerComponent, NzDrawerContentDirective } from 'ng-zorro-antd/drawer';

@Component({
  imports: [
    NzLayoutComponent,
    NzSiderComponent,
    NzMenuDirective,
    NzMenuItemComponent,
    RouterLink,
    NzContentComponent,
    RouterOutlet,
    Header,
    NzIconDirective,
    NgTemplateOutlet,
    LayoutBreakpoint,
    NzDrawerComponent,
    NzDrawerContentDirective
  ],
  selector: 'ske-full',
  styles: ``,
  templateUrl: './full.html',
})
export class Full {
  public readonly drawerOpen = signal<boolean>(false);
}
