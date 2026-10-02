import { Component } from '@angular/core';
import {
  NzPageHeaderComponent,
  NzPageHeaderContentDirective,
  NzPageHeaderTitleDirective,
} from 'ng-zorro-antd/page-header';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { MenuToggle } from '@ske/layouts/menu-toggle';
import { ThemeSwitcher } from '@ske/shared/theme';
import { NzBreadCrumbComponent, NzBreadCrumbItemComponent } from 'ng-zorro-antd/breadcrumb';

@Component({
  imports: [
    MenuToggle,
    NzPageHeaderComponent,
    NzPageHeaderContentDirective,
    NzPageHeaderTitleDirective,
    NzTypographyComponent,
    ThemeSwitcher,
    NzBreadCrumbComponent,
    NzBreadCrumbItemComponent,
  ],
  selector: 'ske-configuration-header',
  templateUrl: './configuration-header.html',
})
export class ConfigurationHeader {}
