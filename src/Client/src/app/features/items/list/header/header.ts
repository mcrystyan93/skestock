import { Component, model, output } from '@angular/core';
import {
  NzPageHeaderComponent,
  NzPageHeaderExtraDirective,
  NzPageHeaderFooterDirective
} from 'ng-zorro-antd/page-header';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzTabComponent, NzTabsComponent } from 'ng-zorro-antd/tabs';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';

@Component({
  imports: [
    NzPageHeaderComponent,
    NzPageHeaderExtraDirective,
    NzPageHeaderFooterDirective,
    NzButtonComponent,
    NzIconDirective,
    NzTabsComponent,
    NzTabComponent,
    NzSpaceItemDirective,
    NzSpaceComponent
  ],
  selector: 'ske-item-header',
  styles: ``,
  templateUrl: './header.html',
})
export class Header {
  public readonly selectedTabIndex = model(0);
  public readonly onAdd = output<void>();
  public readonly onImport = output<void>();
}
