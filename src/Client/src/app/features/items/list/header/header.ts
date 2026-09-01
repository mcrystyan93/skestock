import { Component, output } from '@angular/core';
import { NzPageHeaderComponent, NzPageHeaderExtraDirective } from 'ng-zorro-antd/page-header';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';

@Component({
  imports: [
    NzPageHeaderComponent,
    NzPageHeaderExtraDirective,
    NzButtonComponent,
    NzIconDirective
  ],
  selector: 'ske-item-header',
  styles: ``,
  templateUrl: './header.html',
})
export class Header {
  public readonly onAdd = output<void>();
}
