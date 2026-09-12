import { Component, input } from '@angular/core';
import {
  NzPageHeaderComponent,
  NzPageHeaderSubtitleDirective,
  NzPageHeaderTitleDirective
} from 'ng-zorro-antd/page-header';

@Component({
  imports: [
    NzPageHeaderComponent,
    NzPageHeaderTitleDirective,
    NzPageHeaderSubtitleDirective
  ],
  selector: 'ske-class-analysis-page-header',
  templateUrl: './header.html'
})
export class Header {
  public readonly title = input.required<string>();
  public readonly description = input.required<string>();
}
