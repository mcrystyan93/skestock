import { Component, output } from '@angular/core';
import {
  NzPageHeaderComponent,
  NzPageHeaderContentDirective,
  NzPageHeaderExtraDirective
} from 'ng-zorro-antd/page-header';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzBreadCrumbComponent, NzBreadCrumbItemComponent } from 'ng-zorro-antd/breadcrumb';
import { ThemeSwitcher } from '@ske/shared/theme';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';

@Component({
  imports: [
    NzPageHeaderComponent,
    NzPageHeaderExtraDirective,
    NzButtonComponent,
    NzIconDirective,
    NzBreadCrumbComponent,
    NzBreadCrumbItemComponent,
    ThemeSwitcher,
    NzPageHeaderContentDirective,
    NzTypographyComponent
  ],
  selector: 'ske-school-class-header',
  styles: ``,
  templateUrl: './header.html',
})
export class Header {
  public readonly onAdd = output<void>();
}
