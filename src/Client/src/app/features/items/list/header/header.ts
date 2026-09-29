import { Component, computed, inject, model, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  NzPageHeaderComponent,
  NzPageHeaderExtraDirective,
  NzPageHeaderFooterDirective,
  NzPageHeaderTitleDirective
} from 'ng-zorro-antd/page-header';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { gridResponsiveMap, NzBreakpointService } from 'ng-zorro-antd/core/services';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzTooltipDirective } from 'ng-zorro-antd/tooltip';
import { NzBreadCrumbComponent, NzBreadCrumbItemComponent } from 'ng-zorro-antd/breadcrumb';
import { ThemeSwitcher } from '@ske/shared/theme';
import { MenuToggle } from '@ske/layouts';
import { HeaderTabOption, HeaderTabs } from '@ske/shared/header-tabs';

@Component({
  imports: [
    NzPageHeaderComponent,
    NzPageHeaderExtraDirective,
    NzPageHeaderFooterDirective,
    NzPageHeaderTitleDirective,
    NzButtonComponent,
    NzIconDirective,
    NzTooltipDirective,
    NzBreadCrumbComponent,
    NzBreadCrumbItemComponent,
    ThemeSwitcher,
    MenuToggle,
    HeaderTabs
  ],
  selector: 'ske-item-header',
  templateUrl: './header.html',
})
export class Header {
  public readonly selectedTabIndex = model(0);
  public readonly onAdd = output<void>();
  public readonly onAddSupplyList = output<void>();
  public readonly onImport = output<void>();

  private readonly _breakpoints = toSignal(
    inject(NzBreakpointService).subscribe(gridResponsiveMap, true),
    { initialValue: null }
  );

  public readonly isCompact = computed(() => !(this._breakpoints()?.lg ?? true));

  protected readonly tabs: readonly HeaderTabOption[] = [
    { label: 'Articole' },
    { label: 'Liste' },
    { label: 'Importuri articole', compactLabel: 'Importuri' }
  ];

  protected readonly actions: readonly HeaderAction[] = [
    { text: 'Adaugă', label: 'Adaugă articol', icon: 'icons:plus', run: () => this.onAdd.emit() },
    { text: 'Adaugă listă', label: 'Adaugă listă', icon: 'icons:plus', run: () => this.onAddSupplyList.emit() },
    { text: 'Importă', label: 'Importă articole', icon: 'icons:upload', run: () => this.onImport.emit() }
  ];
}

type HeaderAction = {
  text: string;
  label: string;
  icon: string;
  run: () => void;
};
