import { Component, output } from '@angular/core';
import { NzHeaderComponent } from 'ng-zorro-antd/layout';
import { ThemeSwitcher } from '@ske/shared/theme';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { LayoutBreakpoint } from '@ske/shared/directives';

@Component({
  imports: [
    NzHeaderComponent,
    ThemeSwitcher,
    NzButtonComponent,
    NzIconDirective,
    LayoutBreakpoint
  ],
  selector: 'ske-header',
  styles: ``,
  templateUrl: './header.html',
})
export class Header {
  public readonly drawerToggle = output<void>();
}
