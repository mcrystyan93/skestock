import {Component, computed, inject, input, model, output} from '@angular/core';
import {toSignal} from '@angular/core/rxjs-interop';
import {
  NzPageHeaderComponent,
  NzPageHeaderContentDirective,
  NzPageHeaderExtraDirective,
  NzPageHeaderFooterDirective,
  NzPageHeaderSubtitleDirective,
  NzPageHeaderTitleDirective
} from 'ng-zorro-antd/page-header';
import {SchoolClassDto, SchoolClassSummary} from '@ske/models';
import {CurrencyPipe, DatePipe} from '@angular/common';
import {NzStatisticComponent} from 'ng-zorro-antd/statistic';
import {NzTypographyComponent} from 'ng-zorro-antd/typography';
import {NzButtonComponent} from 'ng-zorro-antd/button';
import {NzIconDirective} from 'ng-zorro-antd/icon';
import {NzSpaceComponent, NzSpaceItemDirective} from 'ng-zorro-antd/space';
import {GoodsReceiptCostStatistic} from './goods-receipt-cost-statistic';
import {NzBreadCrumbComponent, NzBreadCrumbItemComponent} from 'ng-zorro-antd/breadcrumb';
import {ThemeSwitcher} from '@ske/shared/theme';
import {MenuToggle} from '@ske/layouts/menu-toggle';
import {HeaderTabOption, HeaderTabs} from '@ske/shared/header-tabs';
import {NzBreakpointService, gridResponsiveMap} from 'ng-zorro-antd/core/services';
import {NzTooltipDirective} from 'ng-zorro-antd/tooltip';

@Component({
  imports: [
    NzPageHeaderComponent,
    NzPageHeaderTitleDirective,
    NzPageHeaderSubtitleDirective,
    DatePipe,
    NzPageHeaderContentDirective,
    NzStatisticComponent,
    GoodsReceiptCostStatistic,
    NzPageHeaderFooterDirective,
    CurrencyPipe,
    NzTypographyComponent,
    NzPageHeaderExtraDirective,
    NzButtonComponent,
    NzIconDirective,
    NzSpaceComponent,
    NzSpaceItemDirective,
    NzBreadCrumbComponent,
    NzBreadCrumbItemComponent,
    ThemeSwitcher,
    MenuToggle,
    HeaderTabs,
    NzTooltipDirective
  ],
  selector: 'ske-school-class-overview-header',
  styles: ``,
  templateUrl: './header.html',
})
export class Header {
  public readonly schoolClass = input.required<Partial<SchoolClassDto>>();
  public readonly summary = input.required<Partial<SchoolClassSummary>>();

  public selectedTabIndex = model<number>(0);

  public onAddGoodsReceipt = output<void>();
  public onOrderCreate = output<void>();
  public onAddStock = output<void>();
  public onClose = output<void>();

  private readonly _breakpoints = toSignal(
    inject(NzBreakpointService).subscribe(gridResponsiveMap, true),
    {initialValue: null}
  );

  public readonly isCompact = computed(() => !(this._breakpoints()?.lg ?? true));

  protected readonly tabs: readonly HeaderTabOption[] = [
    {label: 'Stoc'},
    {label: 'Comenzi'},
    {label: 'Comenzi recepționate', compactLabel: 'Recepții'},
    {label: 'Comenzi importate', compactLabel: 'Importuri'},
    {label: 'Statistici', compactLabel: 'Stat.'},
    {label: 'Departamente', compactLabel: 'Dept.'}
  ];

  protected readonly action = computed<HeaderAction | null>(() => {
    switch (this.selectedTabIndex()) {
      case 0:
        return {text: 'Adaugă articol', label: 'Adaugă articol', icon: 'icons:plus', run: () => this.onAddStock.emit()};
      case 1:
        return {
          text: 'Creează comandă',
          label: 'Creează comandă',
          icon: 'icons:plus',
          run: () => this.onOrderCreate.emit()
        };
      case 3:
        return {
          text: 'Import aviz comandă',
          label: 'Import aviz comandă',
          icon: 'icons:file-import',
          run: () => this.onAddGoodsReceipt.emit()
        };
      default:
        return null;
    }
  });
}

type HeaderAction = {
  text: string;
  label: string;
  icon: string;
  run: () => void;
};
