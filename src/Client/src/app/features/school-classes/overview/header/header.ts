import { Component, input, model } from '@angular/core';
import {
  NzPageHeaderComponent, NzPageHeaderContentDirective, NzPageHeaderFooterDirective,
  NzPageHeaderSubtitleDirective,
  NzPageHeaderTitleDirective
} from 'ng-zorro-antd/page-header';
import { SchoolClassDto, SchoolClassSummary } from '@ske/models';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzStatisticComponent } from 'ng-zorro-antd/statistic';
import { NzTabComponent, NzTabsComponent } from 'ng-zorro-antd/tabs';

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
    CurrencyPipe
  ],
  selector: 'ske-header',
  styles: ``,
  templateUrl: './header.html',
})
export class Header {
  public readonly schoolClass = input.required<Partial<SchoolClassDto>>();
  public readonly summary = input.required<Partial<SchoolClassSummary>>();

  public selectedTabIndex = model<number>(0);
}
