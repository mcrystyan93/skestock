import { Component, effect, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NgTemplateOutlet } from '@angular/common';
import { RouterLink, RouterOutlet } from '@angular/router';
import { NzContentComponent, NzLayoutComponent, NzSiderComponent } from 'ng-zorro-antd/layout';
import { NzMenuDirective, NzMenuItemComponent } from 'ng-zorro-antd/menu';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzDrawerComponent, NzDrawerContentDirective } from 'ng-zorro-antd/drawer';
import { gridResponsiveMap, NzBreakpointService } from 'ng-zorro-antd/core/services';
import { LayoutBreakpoint } from '@ske/shared/directives';
import { ThemeSwitcher } from '@ske/shared/theme';
import { NavigationDrawerState } from './navigation-drawer.state';

@Component({
  imports: [
    NzLayoutComponent,
    NzSiderComponent,
    NzMenuDirective,
    NzMenuItemComponent,
    RouterLink,
    NzContentComponent,
    RouterOutlet,
    NzIconDirective,
    NzButtonComponent,
    NgTemplateOutlet,
    LayoutBreakpoint,
    NzDrawerComponent,
    NzDrawerContentDirective,
    ThemeSwitcher,
  ],
  selector: 'ske-full',
  templateUrl: './full.html',
})
export class Full {
  protected readonly drawer = inject(NavigationDrawerState);

  private readonly _breakpoints = toSignal(
    inject(NzBreakpointService).subscribe(gridResponsiveMap, true),
    { initialValue: null }
  );

  constructor() {
    effect(() => {
      if (this._breakpoints()?.lg) {
        this.drawer.close();
      }
    });
  }
}
