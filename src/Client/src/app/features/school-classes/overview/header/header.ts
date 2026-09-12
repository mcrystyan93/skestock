import { Component, input, model, output } from '@angular/core';
import {
  NzPageHeaderComponent, NzPageHeaderContentDirective, NzPageHeaderExtraDirective, NzPageHeaderFooterDirective,
  NzPageHeaderSubtitleDirective,
  NzPageHeaderTitleDirective
} from 'ng-zorro-antd/page-header';
import { SchoolClassDto, SchoolClassSummary } from '@ske/models';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzStatisticComponent } from 'ng-zorro-antd/statistic';
import { NzTabComponent, NzTabsComponent } from 'ng-zorro-antd/tabs';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';

@Component({
  imports: [
    NzPageHeaderComponent,
    NzPageHeaderTitleDirective,
    NzPageHeaderSubtitleDirective,
    DatePipe,
    NzPageHeaderContentDirective,
    NzRowDirective,
    NzColDirective,
    NzStatisticComponent,
    NzPageHeaderFooterDirective,
    NzTabsComponent,
    NzTabComponent,
    CurrencyPipe,
    NzTypographyComponent,
    NzPageHeaderExtraDirective,
    NzButtonComponent,
    NzIconDirective,
    NzSpaceComponent,
    NzSpaceItemDirective
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
  public onAnalytics = output<void>();
  public onClose = output<void>();
}
