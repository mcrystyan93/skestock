import { Component, computed, inject, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  NzPageHeaderComponent,
  NzPageHeaderContentDirective,
  NzPageHeaderExtraDirective,
  NzPageHeaderTitleDirective
} from 'ng-zorro-antd/page-header';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { gridResponsiveMap, NzBreakpointService } from 'ng-zorro-antd/core/services';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzTooltipDirective } from 'ng-zorro-antd/tooltip';
import { NzBreadCrumbComponent, NzBreadCrumbItemComponent } from 'ng-zorro-antd/breadcrumb';
import { ThemeSwitcher } from '@ske/shared/theme';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { MenuToggle } from '@ske/layouts';

@Component({
  imports: [
    NzPageHeaderComponent,
    NzPageHeaderExtraDirective,
    NzPageHeaderTitleDirective,
    NzButtonComponent,
    NzIconDirective,
    NzTooltipDirective,
    NzBreadCrumbComponent,
    NzBreadCrumbItemComponent,
    ThemeSwitcher,
    NzPageHeaderContentDirective,
    NzTypographyComponent,
    MenuToggle
  ],
  selector: 'ske-school-class-header',
  templateUrl: './header.html',
})
export class Header {
  public readonly onAdd = output<void>();

  private readonly _breakpoints = toSignal(
    inject(NzBreakpointService).subscribe(gridResponsiveMap, true),
    { initialValue: null }
  );

  public readonly isCompact = computed(() => !(this._breakpoints()?.lg ?? true));
}
