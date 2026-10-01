import { Component, DestroyRef, inject } from '@angular/core';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NavigationDrawerState } from '../navigation-drawer.state';

@Component({
  imports: [NzButtonComponent, NzIconDirective],
  selector: 'ske-menu-toggle',
  host: { class: 'inline-flex lg:hidden' },
  template: `
    <button nz-button
            nzType="text"
            type="button"
            aria-label="Deschide meniul"
            aria-controls="ske-navigation-drawer"
            [attr.aria-expanded]="drawer.open()"
            (click)="drawer.toggle()">
      <nz-icon nzType="icons:bars"
               class="text-lg!"></nz-icon>
    </button>
  `
})
export class MenuToggle {
  protected readonly drawer = inject(NavigationDrawerState);

  constructor() {
    this.drawer.register();
    inject(DestroyRef).onDestroy(() => this.drawer.unregister());
  }
}
